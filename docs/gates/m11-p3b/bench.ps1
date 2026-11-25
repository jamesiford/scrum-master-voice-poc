<#
  M11 prompt 3B measurement driver (ADR 0019). Sets the realtime model, the decision-model mode, the silence setting and the cast on the
  deployed API and bridge, then runs the gate harness with three concurrent Simulated Room standups (one per team) per batch and saves the results.
  Candidate comparisons run two batches (6 standups, about 54 timed questions); silence runs one batch (3 standups).
  The extra teams are registered by the harness as copies of team-payments.
  Every configuration uses the same method, so the comparison is like for like.
    pwsh docs/gates/m11-p3b/bench.ps1 -Configs 'rt21-decision','rt21-rules','mini-decision','mini-rules'
  Needs the azd environment (azd env select <name>) and az login.
#>
param(
  [string[]]$Configs,
  [string]$OutRoot = 'docs/gates/m11-p3b'
)
# -File passes a comma list as one string, so split it.
$Configs = $Configs | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim("'", ' ') } | Where-Object { $_ }
. "$PSScriptRoot/../../../infra/hooks/lib.ps1"
Import-AzdEnvironment

# name -> model, analyser mode, silence ms, cast file
$all = @{
  'rt21-decision' = @{ Model = 'gpt-realtime-2.1';      Mode = 'Decision'; Silence = 800; Cast = 'demo/cast/payments-standup.yaml' }
  'rt21-rules'    = @{ Model = 'gpt-realtime-2.1';      Mode = 'Rules';    Silence = 800; Cast = 'demo/cast/payments-standup.yaml' }
  'mini-decision' = @{ Model = 'gpt-realtime-2.1-mini'; Mode = 'Decision'; Silence = 800; Cast = 'demo/cast/payments-standup.yaml' }
  'mini-rules'    = @{ Model = 'gpt-realtime-2.1-mini'; Mode = 'Rules';    Silence = 800; Cast = 'demo/cast/payments-standup.yaml' }
}
foreach ($ms in 800, 650, 500) {
  foreach ($m in @(@('rt21', 'gpt-realtime-2.1'), @('mini', 'gpt-realtime-2.1-mini'))) {
    $all["sil-$($m[0])-$ms"] = @{ Model = $m[1]; Mode = 'Decision'; Silence = $ms; Cast = 'demo/cast/payments-standup-pauses.yaml' }
  }
}

$rg = $env:AZURE_RESOURCE_GROUP
$apiApp = $env:API_APP_NAME
$bridgeApp = $env:BRIDGE_APP_NAME
$api = $env:API_URI.TrimEnd('/')

function Wait-Healthy([string]$Url) {
  $deadline = (Get-Date).AddMinutes(5)
  while ((Get-Date) -lt $deadline) { try { if ((Invoke-WebRequest $Url -TimeoutSec 15 -UseBasicParsing).StatusCode -eq 200) { return } } catch { Start-Sleep -Seconds 5 } }
  throw "$Url did not become healthy"
}

foreach ($name in $Configs) {
  $c = $all[$name]
  if (-not $c) { throw "Unknown configuration $name" }
  Write-Host "=== $name : $($c.Model), analyser $($c.Mode), silence $($c.Silence) ms, $($c.Cast)"
  az containerapp update -g $rg -n $apiApp --set-env-vars "VoiceLive__Model=$($c.Model)" "UtteranceAnalysis__Mode=$($c.Mode)" "Voice__SilenceMs=$($c.Silence)" -o none
  az containerapp update -g $rg -n $bridgeApp --set-env-vars "Demo__CastFile=$($c.Cast)" -o none
  Wait-Healthy "$api/healthz"
  Wait-Healthy "$($env:BRIDGE_URI.TrimEnd('/'))/healthz"
  Start-Sleep -Seconds 20
  $token = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
  $out = Join-Path $OutRoot $name
  dotnet run --project tools/GateRunner -- --label $name --runs $(if ($name -like 'sil-*') { 1 } else { 2 }) --api $api --team team-payments --teams team-payments,team-b,team-c --out $out --token $token --timeout-minutes 25 --build "$(git rev-parse --short HEAD)"
  if ($LASTEXITCODE -ne 0) { Write-Warning "GateRunner exited with $LASTEXITCODE for $name" }
}
