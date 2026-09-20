<#
.SYNOPSIS
    Talk to the bug triage MCP server directly, without an editor.

.DESCRIPTION
    Normally your editor launches this server and you just talk to Claude in plain English.
    This script exists so you can SEE the server working on its own - useful for checking
    configuration, or for a quick answer without starting a session.

.EXAMPLE
    .\try-it.ps1
    Checks the server starts, lists its tools, and reports which sources are configured.

.EXAMPLE
    .\try-it.ps1 -StackTrace (Get-Content crash.txt -Raw)
    Maps a stack trace onto files in the checkout.

.EXAMPLE
    .\try-it.ps1 -Report "feedback page returns 500, operation id 4f9c1a2b3d4e5f60718293a4b5c6d7e8"
    Pulls the actionable identifiers out of a free-form bug report.

.EXAMPLE
    .\try-it.ps1 -Exceptions
    Lists the top production exceptions from Application Insights (needs credentials).
#>
[CmdletBinding()]
param(
    [string]$StackTrace,
    [string]$Report,
    [switch]$Exceptions,
    [int]$LookbackHours = 24,
    [string]$ServerPath
)

$ErrorActionPreference = 'Stop'

if (-not $ServerPath) {
    $ServerPath = Join-Path $PSScriptRoot 'bin\Release\publish\Risebird.Mcp.BugTriage.exe'
}

if (-not (Test-Path $ServerPath)) {
    Write-Host "Server not found at:" -ForegroundColor Red
    Write-Host "  $ServerPath"
    Write-Host ""
    Write-Host "Publish it first, from the repository root:" -ForegroundColor Yellow
    Write-Host "  dotnet publish Risebird.Mcp.BugTriage -c Release -o Risebird.Mcp.BugTriage\bin\Release\publish"
    exit 1
}

# --- start the server and speak MCP over stdin/stdout -------------------------------------------

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $ServerPath
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)

function Send-Message($payload) {
    $proc.StandardInput.WriteLine(($payload | ConvertTo-Json -Depth 20 -Compress))
    $proc.StandardInput.Flush()
}

function Read-Message {
    $line = $proc.StandardOutput.ReadLine()
    if ($null -eq $line) { throw "The server closed the connection unexpectedly." }
    return $line | ConvertFrom-Json
}

function Invoke-Tool($id, $name, $toolArgs) {
    Send-Message @{ jsonrpc = '2.0'; id = $id; method = 'tools/call'; params = @{ name = $name; arguments = $toolArgs } }
    $response = Read-Message
    if ($response.error) { return "ERROR: $($response.error.message)" }
    return $response.result.content[0].text
}

try {
    Send-Message @{
        jsonrpc = '2.0'; id = 1; method = 'initialize'
        params  = @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'try-it'; version = '1.0' } }
    }
    $init = Read-Message
    Send-Message @{ jsonrpc = '2.0'; method = 'notifications/initialized' }

    Write-Host "Connected to $($init.result.serverInfo.name) v$($init.result.serverInfo.version)" -ForegroundColor Green
    Write-Host ""

    if ($StackTrace) {
        Write-Host "=== resolve_stack_trace ===" -ForegroundColor Cyan
        Invoke-Tool 2 'resolve_stack_trace' @{ stackTrace = $StackTrace; contextLines = 4; commitsPerFile = 3 }
        return
    }

    if ($Report) {
        Write-Host "=== analyze_bug_report ===" -ForegroundColor Cyan
        Invoke-Tool 2 'analyze_bug_report' @{ reportText = $Report }
        return
    }

    if ($Exceptions) {
        Write-Host "=== query_exceptions (last $LookbackHours h) ===" -ForegroundColor Cyan
        Invoke-Tool 2 'query_exceptions' @{ lookbackHours = $LookbackHours }
        return
    }

    # --- default: self-check ---------------------------------------------------------------------

    Send-Message @{ jsonrpc = '2.0'; id = 2; method = 'tools/list' }
    $tools = (Read-Message).result.tools
    Write-Host "$($tools.Count) tools available:" -ForegroundColor Cyan
    $tools | Sort-Object name | ForEach-Object { Write-Host "  - $($_.name)" }
    Write-Host ""

    Write-Host "Configuration check:" -ForegroundColor Cyan

    # Repository access: resolvable only when RootPath points at a real checkout.
    $probe = Invoke-Tool 3 'resolve_stack_trace' @{
        stackTrace     = '   at Probe.Type.Method() in X:\build\Risebird.Business\UserService.cs:line 1'
        contextLines   = 0
        commitsPerFile = 0
    }
    if ($probe -like '*RootPath is not set*') {
        Write-Host "  repository    NOT CONFIGURED - set Triage:Repository:RootPath" -ForegroundColor Yellow
    }
    elseif (($probe | ConvertFrom-Json).framesResolved -ge 1) {
        Write-Host "  repository    OK - stack traces resolve to real files" -ForegroundColor Green
    }
    else {
        Write-Host "  repository    reachable, but the probe file was not found (this is fine)" -ForegroundColor Gray
    }

    $appInsights = Invoke-Tool 4 'query_exceptions' @{ lookbackHours = 1; maxResults = 1 }
    if ($appInsights -like '*not configured*') {
        Write-Host "  App Insights  NOT CONFIGURED - set Triage:AppInsights:ResourceId" -ForegroundColor Yellow
    }
    elseif ($appInsights -like 'ERROR*' -or $appInsights -like '*authentication*' -or $appInsights -like '*sign-in*') {
        Write-Host "  App Insights  configured, but the call failed:" -ForegroundColor Yellow
        Write-Host "                $($appInsights -replace '\s+', ' ')" -ForegroundColor Gray
    }
    else {
        Write-Host "  App Insights  OK - queries are returning" -ForegroundColor Green
    }

    $workItem = Invoke-Tool 5 'search_work_items' @{ maxResults = 1 }
    if ($workItem -like '*not configured*') {
        Write-Host "  Azure DevOps  NOT CONFIGURED - set Triage:AzureDevOps:*" -ForegroundColor Yellow
    }
    else {
        Write-Host "  Azure DevOps  responded" -ForegroundColor Green
    }

    $gitHub = Invoke-Tool 6 'search_github_issues' @{ query = 'is:issue'; maxResults = 1 }
    if ($gitHub -like '*not configured*' -or $gitHub -like '*No repository given*') {
        Write-Host "  GitHub        NOT CONFIGURED - set Triage:GitHub:Token" -ForegroundColor Yellow
    }
    else {
        Write-Host "  GitHub        responded" -ForegroundColor Green
    }

    $fixes = Invoke-Tool 7 'list_fixes' @{} | ConvertFrom-Json
    $fixState = if ($fixes.writesEnabled) { 'ENABLED - apply_fix can write to source' } else { 'disabled (default)' }
    $fixColour = if ($fixes.writesEnabled) { 'Yellow' } else { 'Gray' }
    Write-Host "  fix tools     $fixState" -ForegroundColor $fixColour

    Write-Host ""
    Write-Host "This script is only for checking the server by hand." -ForegroundColor Gray
    Write-Host "Day to day, just ask Claude in plain English - it calls these tools for you." -ForegroundColor Gray
}
finally {
    if ($proc -and -not $proc.HasExited) {
        $proc.StandardInput.Close()
        if (-not $proc.WaitForExit(5000)) { $proc.Kill() }
    }
}
