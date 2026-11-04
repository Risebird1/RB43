namespace Risebird.AzureTimerFunctions.CodeProjects
{
    /// <summary>
    /// PowerShell run on the candidate machine through managed Run Command
    /// (Microsoft.Compute/virtualMachines/runCommands). Both run as LocalSystem. Parameters arrive
    /// as named script arguments; secrets (the zip SAS, the candidate password) go in
    /// protectedParameters so they are encrypted in transit and never echoed in the VM model.
    ///
    /// Scripts are ASCII only: the extension misreads smart quotes and other Unicode.
    /// Windows PowerShell 5.1 syntax, so no PowerShell 7 is required in the stack image.
    /// </summary>
    public static class CodeProjectVmScripts
    {
        public const string BaselineFile = @"C:\ProgramData\Risebird\baseline.txt";

        /// <summary>
        /// Candidate standard user (never an administrator), IMDS/WireServer blocked for that user,
        /// project restored into the workspace, baseline git commit recorded where the candidate
        /// cannot change it.
        /// </summary>
        public const string Setup = @"
param(
    [string]$WorkspacePath = 'C:\workspace',
    [string]$CandidateUser = 'candidate',
    [string]$CandidatePassword,
    [string]$ProjectZipUrl
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 1. Candidate account: a standard user who may sign in over RDP (Bastion). Groups by SID so
#    localized Windows images work too.
$secure = ConvertTo-SecureString $CandidatePassword -AsPlainText -Force
if (Get-LocalUser -Name $CandidateUser -ErrorAction SilentlyContinue) {
    Set-LocalUser -Name $CandidateUser -Password $secure
} else {
    New-LocalUser -Name $CandidateUser -Password $secure -PasswordNeverExpires -AccountNeverExpires -UserMayNotChangePassword | Out-Null
}
Add-LocalGroupMember -SID 'S-1-5-32-545' -Member $CandidateUser -ErrorAction SilentlyContinue   # Users
Add-LocalGroupMember -SID 'S-1-5-32-555' -Member $CandidateUser -ErrorAction SilentlyContinue   # Remote Desktop Users
Remove-LocalGroupMember -SID 'S-1-5-32-544' -Member $CandidateUser -ErrorAction SilentlyContinue # never Administrators
$sid = (Get-LocalUser -Name $CandidateUser).SID.Value

# 2. Instance Metadata Service and WireServer are unreachable for the candidate. An NSG cannot
#    filter these link-local endpoints; Azure's guidance is an in-guest firewall rule scoped to
#    the processes that must not reach them. The VM agent (LocalSystem) is unaffected.
Get-NetFirewallRule -DisplayName 'RB-Block-Platform-Endpoints-Candidate' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName 'RB-Block-Platform-Endpoints-Candidate' -Direction Outbound -Action Block `
    -RemoteAddress @('169.254.169.254', '168.63.129.16') -LocalUser ('D:(A;;CC;;;' + $sid + ')') | Out-Null

# 3. Project into the workspace.
$zip = Join-Path $env:TEMP 'rb-project.zip'
Invoke-WebRequest -Uri $ProjectZipUrl -OutFile $zip -UseBasicParsing
if (Test-Path $WorkspacePath) { Remove-Item $WorkspacePath -Recurse -Force }
New-Item -ItemType Directory -Path $WorkspacePath | Out-Null
Expand-Archive -Path $zip -DestinationPath $WorkspacePath -Force
Remove-Item $zip -Force

# 4. Baseline commit. git runs here as LocalSystem and later as the candidate, so the workspace
#    is marked safe system-wide; the candidate commits under a neutral identity.
$git = (Get-Command git -ErrorAction Stop).Source
$ws = $WorkspacePath -replace '\\', '/'
& $git config --system --add safe.directory $ws
Push-Location $WorkspacePath
& $git -c init.defaultBranch=main init --quiet
& $git config user.name 'Candidate'
& $git config user.email 'candidate@risebird.invalid'
& $git config core.autocrlf false
& $git add -A
& $git commit --quiet -m 'baseline'
if ($LASTEXITCODE -ne 0) { throw 'git baseline commit failed' }
$baseline = (& $git rev-parse HEAD).Trim()
& $git tag rb-baseline
Pop-Location

# The candidate owns the workspace but cannot touch the recorded baseline: grading diffs against
# this file, so moving the tag or rewriting history cannot hide or fake work.
$dir = Split-Path -Parent '" + BaselineFile + @"'
New-Item -ItemType Directory -Path $dir -Force | Out-Null
Set-Content -Path '" + BaselineFile + @"' -Value $baseline -Encoding ASCII
icacls $dir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
icacls $WorkspacePath /grant ($CandidateUser + ':(OI)(CI)M') /T /Q | Out-Null

# 5. Remote Desktop policy: one session per user (a second sign-in replaces the first instead of
#    running alongside it), and no clipboard or drive redirection. This backs up Bastion's own
#    copy/paste setting, so code written elsewhere cannot be pasted in even if that setting drifts.
$ts = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services'
New-Item -Path $ts -Force | Out-Null
Set-ItemProperty -Path $ts -Name 'fSingleSessionPerUser' -Value 1 -Type DWord
Set-ItemProperty -Path $ts -Name 'fDisableClip' -Value 1 -Type DWord
Set-ItemProperty -Path $ts -Name 'fDisableCdm' -Value 1 -Type DWord
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name 'fSingleSessionPerUser' -Value 1 -Type DWord

# 6. The candidate lands in the project: VS Code opens on the workspace at sign-in (and from a
#    desktop shortcut), and every PowerShell window starts there. A terminal outside the workspace
#    is how a candidate ends up committing nowhere ('not a git repository').
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut('C:\Users\Public\Desktop\workspace.lnk')
$lnk.TargetPath = $WorkspacePath
$lnk.Save()
$code = @(""$env:ProgramFiles\Microsoft VS Code\Code.exe"", ""${env:ProgramFiles(x86)}\Microsoft VS Code\Code.exe"") | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($code) {
    foreach ($path in @('C:\Users\Public\Desktop\Open project in VS Code.lnk',
                        'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\Open project in VS Code.lnk')) {
        $s = $shell.CreateShortcut($path)
        $s.TargetPath = $code
        $s.Arguments = '""' + $WorkspacePath + '""'
        $s.WorkingDirectory = $WorkspacePath
        $s.Save()
    }
}
$profileDir = Join-Path $env:windir 'System32\WindowsPowerShell\v1.0'
Set-Content -Path (Join-Path $profileDir 'profile.ps1') -Encoding ASCII -Value (""if (`$PWD.Path -eq `$HOME) { Set-Location '"" + $WorkspacePath + ""' }"")

# Build output never belongs in the candidate's diff (and is not committed when work is saved at submit).
Add-Content -Path (Join-Path $WorkspacePath '.git\info\exclude') -Encoding ASCII -Value @('bin/', 'obj/', 'node_modules/', '__pycache__/', '*.pyc', '.pytest_cache/', 'target/', 'TestResults/', 'test-results/', '.vs/', '.vscode/', '.idea/', 'dist/')

Write-Output ('RB-SETUP-OK ' + $baseline)
";

        /// <summary>
        /// Disables the candidate account and logs off every session it has. Used at the slot deadline
        /// (server-side, whether or not the page submitted) and before collecting, so nothing can change
        /// after the work is read. Never fails the caller: a missing account or session is fine.
        /// </summary>
        private const string LockBlock = @"
$ErrorActionPreference = 'Continue'
Disable-LocalUser -Name $CandidateUser -ErrorAction SilentlyContinue
# quser lines: USERNAME SESSIONNAME ID STATE ... (SESSIONNAME is blank for disconnected sessions,
# so the session id is taken as the first purely numeric column).
foreach ($line in ((quser 2>$null) | Select-Object -Skip 1)) {
    $parts = ($line.Trim() -replace '^>', '') -split '\s+'
    if ($parts.Count -gt 0 -and $parts[0] -ieq $CandidateUser) {
        $id = $parts | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1
        if ($id) { logoff $id }
    }
}
";

        public const string Lock = @"
param([string]$CandidateUser = 'candidate')
" + LockBlock + @"
Write-Output 'RB-LOCKED'
";

        /// <summary>
        /// Emits one line "RBJSON:{...}" with the baseline and head commits, the commit log with
        /// shortstats, the unified diff and a zip of the tree at HEAD (base64). The candidate is signed
        /// out first, so the files are final; anything they did not commit is then committed for them,
        /// so work is never lost to a forgotten commit or a terminal opened outside the workspace.
        /// </summary>
        public const string Collect = @"
param([string]$WorkspacePath = 'C:\workspace', [string]$CandidateUser = 'candidate')
" + LockBlock + @"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$git = (Get-Command git -ErrorAction Stop).Source
Set-Location $WorkspacePath
$baseline = (Get-Content '" + BaselineFile + @"' -Raw).Trim()

# Save uncommitted work. Build output stays out (also for machines set up before this list existed).
# git warnings go to stderr, which 'Stop' would turn into a failure, so this part runs relaxed.
$ErrorActionPreference = 'Continue'
$exclude = Join-Path $WorkspacePath '.git\info\exclude'
$known = if (Test-Path $exclude) { Get-Content $exclude } else { @() }
$missing = @('bin/', 'obj/', 'node_modules/', '__pycache__/', '*.pyc', '.pytest_cache/', 'target/', 'TestResults/', 'test-results/', '.vs/', '.vscode/', '.idea/', 'dist/') | Where-Object { $known -notcontains $_ }
if ($missing) { Add-Content -Path $exclude -Encoding ASCII -Value $missing }
& $git add -A *> $null
if (& $git status --porcelain) {
    & $git -c user.name=Candidate -c user.email=candidate@risebird.invalid commit --quiet --no-verify -m 'Uncommitted work saved automatically at submit' *> $null
}
$ErrorActionPreference = 'Stop'
$head = (& $git rev-parse HEAD).Trim()

$commits = @()
foreach ($line in (& $git log --reverse --format='%H%x1f%s%x1f%cI' ($baseline + '..' + $head))) {
    if (-not $line) { continue }
    $p = $line -split [char]31
    $stat = (& $git show --shortstat --format= $p[0]) -join ' '
    $files = 0; $ins = 0; $del = 0
    if ($stat -match '(\d+) files? changed') { $files = [int]$Matches[1] }
    if ($stat -match '(\d+) insertions?') { $ins = [int]$Matches[1] }
    if ($stat -match '(\d+) deletions?') { $del = [int]$Matches[1] }
    $commits += @{ sha = $p[0]; message = $p[1]; atUtc = ([DateTimeOffset]::Parse($p[2])).UtcDateTime.ToString('o'); filesChanged = $files; insertions = $ins; deletions = $del }
}

$diff = (& $git diff --no-color --no-ext-diff $baseline $head) -join ""`n""
if ($diff.Length -gt 200000) { $diff = $diff.Substring(0, 200000) + ""`n... (diff truncated)"" }

$zip = Join-Path $env:TEMP ('rb-collect-' + [guid]::NewGuid().ToString('N') + '.zip')
& $git archive --format=zip -o $zip $head
$zipBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($zip))
Remove-Item $zip -Force

$result = @{ baselineSha = $baseline; headSha = $head; commits = $commits; diff = $diff; zipBase64 = $zipBase64 }
Write-Output ('RBJSON:' + ($result | ConvertTo-Json -Depth 5 -Compress))
";
    }
}
