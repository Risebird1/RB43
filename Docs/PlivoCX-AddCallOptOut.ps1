<#
.SYNOPSIS
  Adds call opt-out handling to an exported Plivo CX candidate flow (Schedule / Reschedule / Confirm attendance),
  so the flow can be imported back into cx.plivo.com.

.DESCRIPTION
  What it adds to the flow's AI conversation node (ai_agent_v2):
    - An "Opted Out" route (intent) that apologises and ends the call, plus its end node and edge.
      A Plivo CX flow can't hang up by itself; it ends only when the agent selects a route.
    - Two extracted variables, read by PlivoCXInterviewCall/HangupCallback:
        unsubscribe     true | false - the candidate asked not to be called again about this role
        opt_out_reason  short quote of what they said
    - An "OPT-OUT" section in the node instructions, and opt-out phrases in the STT guidance.
  Everything else in the export is left as it is. Running it twice does nothing the second time.

  PlivoCXCallOutcomeProcessor applies unsubscribe = true: the candidate goes cold in that skill channel (no
  booking, upcoming interview there cancelled, "Opted out: AI call" badge, every pre-dial check skips them).

.EXAMPLE
  .\PlivoCX-AddCallOptOut.ps1 -InputPath "Outbound Interview Scheduler-config.json" -OutputPath "PlivoCX-ScheduleAgent-flow.json"
#>
param(
    [Parameter(Mandatory = $true)] [string] $InputPath,
    [Parameter(Mandatory = $true)] [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
# [IO.File] resolves relative paths against the process directory, not the PowerShell location.
$InputPath  = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($InputPath)
$OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

$flow = [IO.File]::ReadAllText((Resolve-Path $InputPath), [Text.Encoding]::UTF8) | ConvertFrom-Json
$ai = @($flow.nodes | Where-Object { $_.type -like 'ai_agent*' })
if ($ai.Count -ne 1) { throw "Expected exactly one AI conversation node, found $($ai.Count)." }
$ai = $ai[0]
$cfg = $ai.data.config
$aiName = $cfg.name

if (@($cfg.extract_variables | Where-Object { $_.variable_name -eq 'unsubscribe' }).Count -gt 0) {
    Write-Host "Already has the opt-out ($InputPath); writing it unchanged."
    [IO.File]::WriteAllText($OutputPath, ($flow | ConvertTo-Json -Depth 100), (New-Object Text.UTF8Encoding($false)))
    return
}

$intentId = [guid]::NewGuid().ToString()
$endId    = [guid]::NewGuid().ToString()
$edgeId   = [guid]::NewGuid().ToString()

# 1. Instructions: when and how to opt out (route, not a spoken goodbye).
$cfg.instructions = $cfg.instructions.TrimEnd() + "`n`n" + @"
OPT-OUT (DO NOT CALL AGAIN)
If the candidate says anything like "don't call me again", "not interested", "remove my number", "stop calling", "unsubscribe" or "I'm not looking for a job":
- Do not try to persuade them and do not continue the call flow.
- IMMEDIATELY select the 'Opted Out' route. It apologises and ends the call, so do not say goodbye yourself.
- "Call me later", "not now", "I'm busy" or "I'm in a meeting" is NOT an opt-out: handle it as described above.
"@

# 2. The "Opted Out" route.
$intent = [pscustomobject]@{
    id                  = $intentId
    intent_instructions = "Select IMMEDIATELY when the candidate asks not to be called again, says they are not interested in this role, asks to remove their number, says stop calling or unsubscribe, or says they are not looking for a job. Never for 'call me later', 'not now', busy or in a meeting."
    intent_name         = 'Opted Out'
    intent_speech       = [pscustomobject]@{
        intent_speech_text = "Understood. We won't call you again about this role. Sorry for the trouble. Goodbye."
        speech_enabled     = $true
    }
}
$cfg.intents = @($cfg.intents) + $intent

# An opt-out must take the new route, not a generic "not confirmed" one (Confirm attendance flow).
foreach ($i in @($cfg.intents)) {
    if ($i.intent_name -eq 'Attendance Not Confirmed') {
        $i.intent_instructions = $i.intent_instructions -replace ', is not interested, asks not to be called', ''
        $i.intent_instructions += " (A candidate who asks not to be called again or is not interested goes to 'Opted Out'.)"
    }
}
$cfg.instructions = $cfg.instructions -replace "need another time, not interested, do not call\.", "need another time. (Not interested / do not call -> select 'Opted Out'.)"

# 3. Extracted variables.
$cfg.extract_variables = @($cfg.extract_variables) + @(
    [pscustomobject]@{
        id                           = [guid]::NewGuid().ToString()
        configure_data_format        = $true
        variable_type                = 'enum'
        variable_enum_values         = @('true', 'false')
        variable_format_instructions = 'Exactly one of the allowed values, lowercase, without quotes.'
        variable_instructions        = "true only if the candidate asked not to be called again about this role, said they are not interested, asked to remove their number, said stop calling or unsubscribe, or said they are not looking for a job. false in every other case, including 'call me later', 'not now', busy, wrong person, unclear, or the call dropped."
        variable_name                = 'unsubscribe'
    },
    [pscustomobject]@{
        id                           = [guid]::NewGuid().ToString()
        configure_data_format        = $true
        variable_type                = 'string'
        variable_enum_values         = @()
        variable_format_instructions = 'A short quote, at most 20 words. Empty when unsubscribe is false.'
        variable_instructions        = 'If the candidate opted out: a short quote of what they said. Otherwise leave empty.'
        variable_name                = 'opt_out_reason'
    }
)

# 4. End node + edge for the new route; every route out of the AI node carries the new variables.
$newVars = @("{{$aiName.unsubscribe}}", "{{$aiName.opt_out_reason}}")
foreach ($e in @($flow.edges | Where-Object { $_.source -eq $ai.id })) {
    $e.data.nodeVars = @($e.data.nodeVars) + $newVars
}
$template = @($flow.edges | Where-Object { $_.source -eq $ai.id })[0]
$routeVars = if ($template) { @($template.data.nodeVars) } else { $newVars }

$flow.nodes = @($flow.nodes) + [pscustomobject]@{
    id   = $endId
    type = 'end_conversation'
    data = [pscustomobject]@{
        meta   = [pscustomobject]@{ name = 'End conversation' }
        config = [pscustomobject]@{ name = 'Hangup Opted Out'; description = ''; end_message = $null }
    }
}
$flow.edges = @($flow.edges) + [pscustomobject]@{
    id           = $edgeId
    source       = $ai.id
    target       = $endId
    sourceHandle = $intentId
    data         = [pscustomobject]@{ nodeVars = $routeVars }
}

# 5. Speech-to-text hints.
if ($flow.PSObject.Properties.Name -contains 'sttGuidance' -and $flow.sttGuidance) {
    $flow.sttGuidance = $flow.sttGuidance.TrimEnd() + " Listen for opt-out phrases like 'don't call me again', 'not interested', 'remove my number', 'stop calling', 'unsubscribe', 'mujhe call mat karo'."
}

[IO.File]::WriteAllText($OutputPath, ($flow | ConvertTo-Json -Depth 100), (New-Object Text.UTF8Encoding($false)))
Write-Host "Wrote $OutputPath - AI node '$aiName': +route 'Opted Out', +unsubscribe, +opt_out_reason."
