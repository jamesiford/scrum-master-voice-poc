<#
  M11 prompt 4 G1 driver (ADR 0021). Runs the gate harness against the deployed API with the voice route set to Direct or Hosted (the relay through the
  Foundry hosted agent), with the decision model on (Mode=Model) or off (Mode=Rules), three concurrent Simulated Room standups per run (one per team, the G9 shape). The routes are
  interleaved in rounds so each hosted run has a direct run in the same time window; the comparison is paired per round, not two medians.
    pwsh docs/gates/m11-p4a/g1.ps1 -Rounds 6
  Needs the azd environment (azd env select <name>) and az login.
#>
param([int]$Rounds = 6, [string]$OutRoot = 'docs/gates/m11-p4a/g1', [string]$Teams = 'team-payments,team-b,team-c')
. "$PSScriptRoot/../../../infra/hooks/lib.ps1"
Import-AzdEnvironment

$rg = $env:AZURE_RESOURCE_GROUP
$apiApp = $env:API_APP_NAME
$api = $env:API_URI.TrimEnd('/')
$relay = "wss://$($env:FOUNDRY_NAME).services.ai.azure.com/api/projects/$($env:FOUNDRY_PROJECT_NAME)/agents/$($env:AGENT_HOST_NAME)/endpoint/protocols/invocations_ws"

function Wait-Healthy([string]$Url) {
  $deadline = (Get-Date).AddMinutes(5)
  while ((Get-Date) -lt $deadline) { try { if ((Invoke-WebRequest $Url -TimeoutSec 15 -UseBasicParsing).StatusCode -eq 200) { return } } catch { Start-Sleep -Seconds 5 } }
  throw "$Url did not become healthy"
}

function Set-Route([string]$Route, [string]$Mode) {
  if ($Route -eq 'hosted') { az containerapp update -g $rg -n $apiApp --set-env-vars "VoiceLive__RelayEndpoint=$relay" "UtteranceAnalysis__Mode=$Mode" -o none }
  else {
    az containerapp update -g $rg -n $apiApp --remove-env-vars VoiceLive__RelayEndpoint -o none
    az containerapp update -g $rg -n $apiApp --set-env-vars "UtteranceAnalysis__Mode=$Mode" -o none
  }
  Wait-Healthy "$api/healthz"
  Start-Sleep -Seconds 20
}

for ($round = 1; $round -le $Rounds; $round++) {
  # Alternate which route goes first in each pair so neither always runs first in the window.
  foreach ($mode in 'Model', 'Rules') {
    $routes = if ($round % 2 -eq 1) { 'direct', 'hosted' } else { 'hosted', 'direct' }
    foreach ($route in $routes) {
      $name = "round$round-$route-$($mode.ToLower())"
      # Resume: a cell with results is kept. A failed or interrupted cell is run again, in the same round.
      if ((Test-Path (Join-Path $OutRoot $name)) -and (Get-ChildItem (Join-Path $OutRoot $name) -Filter *.json -ErrorAction SilentlyContinue)) { Write-Host "=== $name (kept)"; continue }
      Write-Host "=== $name"
      Set-Route $route $mode
      # Cards, follow-ups and bot activities are still whole-collection Cosmos documents and pass 2 MB after about 90 standups (ADR 0021), so each run starts from a reset.
      $reset = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
      Invoke-RestMethod -Method Post "$api/api/demo/reset" -Headers @{ Authorization = "Bearer $reset" } -TimeoutSec 120 | Out-Null
      $token = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
      dotnet run --project tools/GateRunner -- --label $name --runs 1 --api $api --team team-payments --teams $Teams --out (Join-Path $OutRoot $name) --token $token --timeout-minutes 25 --build "$(git rev-parse --short HEAD)"
      if ($LASTEXITCODE -ne 0) { Write-Warning "GateRunner exited with $LASTEXITCODE for $name" }
    }
  }
}

