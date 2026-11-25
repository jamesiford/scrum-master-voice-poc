# Canary and rollback for the hosted agent (ADR 0021). Versions are immutable; traffic is chosen by the endpoint's version_selector.
#   pwsh infra/hooks/canary.ps1 -Show                      print the current split
#   pwsh infra/hooks/canary.ps1 -Canary 2 -Percent 10      send 10 % of new sessions to version 2, the rest stay on the stable version
#   pwsh infra/hooks/canary.ps1 -Promote 2                 send 100 % to version 2
#   pwsh infra/hooks/canary.ps1 -Rollback                  one step back: 100 % to the previous stable version
# Rules are per agent. A session keeps the version it started on, so a canary never changes a standup that is already running.
# VERIFY: https://learn.microsoft.com/azure/foundry/agents/concepts/hosted-agents (agent_endpoint.version_selector, FixedRatio rules)
[CmdletBinding(DefaultParameterSetName = 'Show')]
param(
  [Parameter(ParameterSetName = 'Show')][switch]$Show,
  [Parameter(ParameterSetName = 'Canary', Mandatory)][string]$Canary,
  [Parameter(ParameterSetName = 'Canary')][ValidateRange(1, 99)][int]$Percent = 10,
  [Parameter(ParameterSetName = 'Promote', Mandatory)][string]$Promote,
  [Parameter(ParameterSetName = 'Rollback', Mandatory)][switch]$Rollback
)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$endpoint = $env:FOUNDRY_PROJECT_ENDPOINT
$agent = if ($env:AGENT_HOST_NAME) { $env:AGENT_HOST_NAME } else { 'zava-voice-relay' }
$token = az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
$h = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
$url = "$endpoint/agents/$($agent)?api-version=v1"

function Get-Rules { (Invoke-RestMethod $url -Headers $h).agent_endpoint.version_selector.version_selection_rules }
function Set-Rules($rules) {
  $body = @{ agent_endpoint = @{ version_selector = @{ version_selection_rules = @($rules) } } } | ConvertTo-Json -Depth 10
  Invoke-RestMethod -Method Patch $url -Headers $h -Body $body | Out-Null
}
function Show-Rules($rules) { foreach ($r in $rules) { Write-Check PASS "traffic $($r.traffic_percentage) %" "version $($r.agent_version)" } }

$current = Get-Rules
switch ($PSCmdlet.ParameterSetName) {
  'Show' { Show-Rules $current }
  'Canary' {
    $stable = ($current | Sort-Object traffic_percentage -Descending | Select-Object -First 1).agent_version
    # The previous split is kept on the azd environment so -Rollback is one step and needs no memory of versions.
    Set-AzdValue AGENT_HOST_PREVIOUS_STABLE $stable
    Set-Rules @(@{ type = 'FixedRatio'; agent_version = $stable; traffic_percentage = 100 - $Percent }, @{ type = 'FixedRatio'; agent_version = $Canary; traffic_percentage = $Percent })
    Show-Rules (Get-Rules)
  }
  'Promote' {
    $old = ($current | Sort-Object traffic_percentage -Descending | Select-Object -First 1).agent_version
    if ($old -ne $Promote) { Set-AzdValue AGENT_HOST_PREVIOUS_STABLE $old }
    Set-Rules @(@{ type = 'FixedRatio'; agent_version = $Promote; traffic_percentage = 100 })
    Show-Rules (Get-Rules)
  }
  'Rollback' {
    $previous = $env:AGENT_HOST_PREVIOUS_STABLE
    if (-not $previous) { throw 'There is no previous stable version recorded; promote or canary a version first.' }
    Set-Rules @(@{ type = 'FixedRatio'; agent_version = $previous; traffic_percentage = 100 })
    Show-Rules (Get-Rules)
  }
}
