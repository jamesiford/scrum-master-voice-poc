# Build and deploy only the storage API; preserve hosted/native/bridge state and all unrelated configuration.
param([switch]$QuiesceLegacy)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_RESOURCE_GROUP -ne 'rg-zava-scrum-zavaw3' -or $env:AZURE_LOCATION -ne 'westus3') {
  throw 'Storage API rollout is restricted to zavaw3/westus3.'
}
$group = $env:AZURE_RESOURCE_GROUP
$api = az containerapp list -g $group --query "[?starts_with(name, 'ca-api-')].name" -o tsv
if ($LASTEXITCODE -ne 0 -or -not $api -or $api.Contains("`n")) { throw 'Expected exactly one API in zavaw3.' }
$before = az containerapp show -g $group -n $api -o json | ConvertFrom-Json -Depth 100
if ($LASTEXITCODE -ne 0) { throw 'Cannot snapshot existing API configuration.' }
if ($before.properties.configuration.activeRevisionsMode -ne 'Single') { throw 'Quiesce every legacy writer explicitly before a multiple-revision rollout.' }
$image = $before.properties.template.containers[0].image
$registry = $image.Split('/')[0].Split('.')[0]
$repository = ($image.Split('/', 2)[1] -split '[:@]')[0]
$sha = git -C "$PSScriptRoot\..\.." rev-parse --short HEAD
$tag = "$repository`:storage-$sha"
az acr build --registry $registry --image $tag --file "$PSScriptRoot\..\..\Dockerfile.api" "$PSScriptRoot\..\.." --no-logs --output none
if ($LASTEXITCODE -ne 0) { throw 'API image build failed; the existing revision is untouched.' }
$digest = az acr repository show -n $registry --image $tag --query digest -o tsv
if ($LASTEXITCODE -ne 0 -or -not $digest) { throw 'Cannot resolve the validated API image digest.' }
if ($QuiesceLegacy) {
  $token = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
  if ($LASTEXITCODE -ne 0 -or -not $token) { throw 'Cannot verify idle sessions before legacy-writer quiescence.' }
  $auth = @{ Authorization = "Bearer $token" }
  $teams = Invoke-RestMethod "$($env:API_URI.TrimEnd('/'))/api/teams" -Headers $auth
  foreach ($team in $teams) {
    $sessions = Invoke-RestMethod "$($env:API_URI.TrimEnd('/'))/api/sessions?teamId=$([uri]::EscapeDataString($team.id))" -Headers $auth
    if (@($sessions | Where-Object { $_.status -eq 'Live' -or ($_.startedAt -and -not $_.endedAt) }).Count -gt 0) {
      throw "Team $($team.id) has a running session; no legacy writer was stopped."
    }
  }
  az containerapp revision deactivate -g $group -n $api --revision $before.properties.latestReadyRevisionName --output none
  if ($LASTEXITCODE -ne 0) { throw 'Could not quiesce the exact old API revision; no migration image is deployed.' }
}
az containerapp update -g $group -n $api --image "$registry.azurecr.io/$repository@$digest" `
  --set-env-vars Cosmos__TeamRecordsContainer=TeamRecords --query properties.latestRevisionName -o tsv
if ($LASTEXITCODE -ne 0) { throw 'Storage API update failed; retain export and legacy data for recovery.' }
$deadline = (Get-Date).AddMinutes(4)
do {
  try {
    if ((Invoke-WebRequest "$($env:API_URI.TrimEnd('/'))/healthz" -TimeoutSec 15).StatusCode -eq 200) { break }
  } catch {
    if ((Get-Date) -ge $deadline) { throw }
  }
  Start-Sleep -Seconds 5
} while ((Get-Date) -lt $deadline)
$after = az containerapp show -g $group -n $api -o json | ConvertFrom-Json -Depth 100
if ($LASTEXITCODE -ne 0 -or $after.properties.latestRevisionName -ne $after.properties.latestReadyRevisionName) {
  throw 'New API revision is not ready; migration is not credited.'
}
foreach ($state in @($before, $after)) {
  $state.properties.template.containers[0].image = ''
  $state.properties.template.containers[0].env = @($state.properties.template.containers[0].env |
    Where-Object name -ne 'Cosmos__TeamRecordsContainer' | Sort-Object name)
}
foreach ($part in @('identity', 'configuration', 'template')) {
  $old = if ($part -eq 'identity') { $before.identity } else { $before.properties.$part }
  $new = if ($part -eq 'identity') { $after.identity } else { $after.properties.$part }
  if (($old | ConvertTo-Json -Depth 100 -Compress) -ne ($new | ConvertTo-Json -Depth 100 -Compress)) {
    throw "Unrelated API $part changed; inspect before claiming configuration parity."
  }
}
Write-Host "Storage API digest $digest ready; identity, secrets metadata, scale, probes and unrelated configuration preserved."
