# Rollout for the hosted agent (ADR 0021). Agent versions are immutable.
#   pwsh infra/hooks/canary.ps1 -Show                                print the stable pin and the canary settings
#   pwsh infra/hooks/canary.ps1 -Pin 3                               send 100 % of the stable agent to version 3 (the previous pin is remembered)
#   pwsh infra/hooks/agent-host.ps1 -Canary                          build the candidate into the canary agent
#   pwsh infra/hooks/canary.ps1 -Canary -Percent 10 [-Teams a,b]     send 10 % of new sessions (of those teams, or all) to the canary agent
#   pwsh infra/hooks/canary.ps1 -Promote                             move the canary image into the stable agent as a new version, pin it, stop the canary
#   pwsh infra/hooks/canary.ps1 -Rollback                            one step back: stop the canary if one is running, otherwise pin the previous stable version
# Why two agents: the platform refuses a percentage split on one agent endpoint ("multi-arm fixed-ratio traffic splitting requires an experiment
# selector", which only the Agents Optimization service may set), although the production-deployment page shows one. So the stable agent is pinned
# to one version, and the API sends a share of sessions to a second agent. A session keeps its agent: the choice is a hash of the session id.
[CmdletBinding(DefaultParameterSetName = 'Show')]
param(
  [Parameter(ParameterSetName = 'Show')][switch]$Show,
  [Parameter(ParameterSetName = 'Pin', Mandatory)][string]$Pin,
  [Parameter(ParameterSetName = 'Canary', Mandatory)][switch]$Canary,
  [Parameter(ParameterSetName = 'Canary', Mandatory)][ValidateRange(0, 100)][int]$Percent,
  [Parameter(ParameterSetName = 'Canary')][string[]]$Teams = @(),
  [Parameter(ParameterSetName = 'Promote', Mandatory)][switch]$Promote,
  [Parameter(ParameterSetName = 'Rollback', Mandatory)][switch]$Rollback
)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$endpoint = $env:FOUNDRY_PROJECT_ENDPOINT
$stable = if ($env:AGENT_HOST_NAME) { $env:AGENT_HOST_NAME } else { 'zava-voice-relay' }
$canaryAgent = "$stable-canary"
$rg = $env:AZURE_RESOURCE_GROUP
$api = $env:API_APP_NAME
$token = az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
$h = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }

function Get-Agent([string]$Name) { Invoke-RestMethod "$endpoint/agents/$($Name)?api-version=v1" -Headers $h }
function Get-Pin { $r = (Get-Agent $stable).agent_endpoint.version_selector.version_selection_rules; if ($r.Count -eq 1 -and $r[0].agent_version -ne '@latest') { $r[0].agent_version } else { (Get-Agent $stable).versions.latest.version } }
function Set-Pin([string]$Version) {
  $body = @{ agent_endpoint = @{ version_selector = @{ version_selection_rules = @(@{ type = 'FixedRatio'; agent_version = $Version; traffic_percentage = 100 }) } } } | ConvertTo-Json -Depth 10
  Invoke-RestMethod -Method Patch "$endpoint/agents/$($stable)?api-version=v1" -Headers $h -Body $body | Out-Null
}
function Wait-Api { $deadline = (Get-Date).AddMinutes(5); while ((Get-Date) -lt $deadline) { try { if ((Invoke-WebRequest "$($env:API_URI.TrimEnd('/'))/healthz" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200) { return } } catch { Start-Sleep -Seconds 5 } } }
function Wss([string]$Name) { "wss://$($env:FOUNDRY_NAME).services.ai.azure.com/api/projects/$($env:FOUNDRY_PROJECT_NAME)/agents/$Name/endpoint/protocols/invocations_ws" }
function Set-CanaryRoute([int]$Pct, [string[]]$TeamList) {
  $vars = @("VoiceLive__CanaryEndpoint=$(Wss $canaryAgent)", "VoiceLive__CanaryPercent=$Pct")
  $i = 0; foreach ($t in $TeamList) { $vars += "VoiceLive__CanaryTeams__$i=$t"; $i++ }
  az containerapp update -g $rg -n $api --set-env-vars @vars -o none
  Wait-Api
}

switch ($PSCmdlet.ParameterSetName) {
  'Show' {
    Write-Check PASS 'stable agent' "$stable pinned to version $(Get-Pin)"
    try { Write-Check PASS 'canary agent' "$canaryAgent latest version $((Get-Agent $canaryAgent).versions.latest.version)" } catch { Write-Check SKIP 'canary agent' 'not created (agent-host.ps1 -Canary)' }
    $env_ = az containerapp show -g $rg -n $api --query "properties.template.containers[0].env[?starts_with(name, 'VoiceLive__Canary')].[name,value]" -o tsv
    Write-Check PASS 'API canary settings' $(if ($env_) { ($env_ -join '; ') } else { 'none (no canary)' })
  }
  'Pin' {
    Set-AzdValue AGENT_HOST_PREVIOUS_STABLE (Get-Pin)
    Set-Pin $Pin
    Write-Check PASS 'stable agent' "pinned to version $Pin (previous $($env:AGENT_HOST_PREVIOUS_STABLE))"
  }
  'Canary' {
    Get-Agent $canaryAgent | Out-Null
    Set-CanaryRoute $Percent $Teams
    Write-Check PASS 'canary' "$Percent % of new sessions$(if ($Teams) { ' in ' + ($Teams -join ', ') }) go to $canaryAgent version $((Get-Agent $canaryAgent).versions.latest.version)"
  }
  'Promote' {
    $image = (Get-Agent $canaryAgent).versions.latest.definition.container_configuration.image
    $repo = Resolve-Path (Join-Path $PSScriptRoot '../..')
    $model = if ($env:VOICELIVE_MODEL) { $env:VOICELIVE_MODEL } else { 'gpt-realtime-2.1' }
    $out = dotnet run --project (Join-Path $repo 'tools/AgentDeploy') -- --endpoint $endpoint --name $stable --image $image --tenant $env:AZURE_TENANT_ID `
      --env "VOICELIVE_ENDPOINT=wss://$($env:FOUNDRY_NAME).services.ai.azure.com/voice-live/realtime" --env "VOICELIVE_MODEL=$model" --env 'VOICELIVE_API_VERSION=2026-04-10'
    if ($LASTEXITCODE -ne 0) { throw 'Creating the stable version from the canary image failed.' }
    $version = ($out | Where-Object { $_ -like 'VERSION=*' } | Select-Object -Last 1).Split('=')[1]
    Set-AzdValue AGENT_HOST_PREVIOUS_STABLE (Get-Pin)
    Set-Pin $version
    Set-CanaryRoute 0 @()
    Write-Check PASS 'promoted' "stable agent now pinned to version $version ($image); the canary is stopped"
  }
  'Rollback' {
    $pct = az containerapp show -g $rg -n $api --query "properties.template.containers[0].env[?name=='VoiceLive__CanaryPercent'].value | [0]" -o tsv
    if ($pct -and [int]$pct -gt 0) { Set-CanaryRoute 0 @(); Write-Check PASS 'rollback' 'the canary is stopped; every session uses the stable agent' }
    else {
      if (-not $env:AGENT_HOST_PREVIOUS_STABLE) { throw 'There is no previous stable version recorded; pin one first.' }
      Set-Pin $env:AGENT_HOST_PREVIOUS_STABLE
      Write-Check PASS 'rollback' "stable agent pinned back to version $($env:AGENT_HOST_PREVIOUS_STABLE)"
    }
  }
}
