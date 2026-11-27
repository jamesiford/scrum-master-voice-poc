# Build the committed source, never a developer's dependency caches, secrets or conductor notes.
param([Parameter(Mandatory)][ValidateSet('api', 'bridge')][string]$Service)
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3' -or $env:AZURE_SUBSCRIPTION_ID -ne '4b0dd31f-5379-4acc-8dcd-60f2929d2513') {
  throw 'This conductor deployment is authorised only for zavaw3/westus3 in the designated subscription.'
}
$repo = (Resolve-Path "$PSScriptRoot\..\..").Path
$dirty = git -C $repo diff HEAD --name-only -- src agents demo global.json Directory.Build.props Directory.Packages.props Dockerfile.api Dockerfile.bridge
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Commit runtime/build inputs before deploying their source snapshot.' }
$head = git -C $repo rev-parse --short HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the committed deployment source.' }
$environmentArguments = @()
if ($Service -eq 'api' -and ($env:DEMO_RESET_TENANT_ID -or $env:DEMO_RESET_TEAM_IDS)) {
  if ($env:DEMO_RESET_TENANT_ID -ne $env:AZURE_TENANT_ID -or -not $env:DEMO_RESET_TEAM_IDS) {
    throw 'Demo reset configuration must name the selected tenant and an explicit team allowlist.'
  }
  $ids = @($env:DEMO_RESET_TEAM_IDS.Split(',') | ForEach-Object { $_.Trim() })
  if (($ids | Where-Object { -not $_ }).Count -gt 0 -or ($ids | Sort-Object -Unique).Count -ne $ids.Count) {
    throw 'Demo reset team ids must be nonempty and unique.'
  }
  $vars = @("Demo__ResetTenantId=$($env:DEMO_RESET_TENANT_ID)")
  $names = @()
  for ($i = 0; $i -lt $ids.Count; $i++) {
    $names += "Demo__ResetTeamIds__$i"
    $vars += "$($names[-1])=$($ids[$i])"
  }
  $existingNames = az containerapp show --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP -n $env:API_APP_NAME `
    --query 'properties.template.containers[0].env[].name' -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw 'Cannot read existing API environment names before the scoped update.' }
  $obsolete = @($existingNames | Where-Object { $_ -like 'Demo__ResetTeamIds__*' -and $_ -notin $names })
  $environmentArguments = @('--set-env-vars') + $vars
  if ($obsolete.Count -gt 0) { $environmentArguments += @('--remove-env-vars') + $obsolete }
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('zava-service-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  $zip = Join-Path $work 'source.zip'
  git -C $repo archive HEAD --format=zip "--output=$zip"
  if ($LASTEXITCODE -ne 0) { throw 'Cannot package the committed source.' }
  $context = Join-Path $work 'context'
  Expand-Archive -Path $zip -DestinationPath $context
  $registry = ($env:AZURE_CONTAINER_REGISTRY_ENDPOINT -split '\.')[0]
  $tag = "git-$head"
  $image = "zava-scrum-agent/$Service-$($env:AZURE_ENV_NAME):$tag"
  $dockerfile = if ($Service -eq 'api') { 'Dockerfile.api' } else { 'Dockerfile.bridge' }
  az acr build --subscription $env:AZURE_SUBSCRIPTION_ID --registry $registry --image $image --file $dockerfile $context --no-logs -o none
  if ($LASTEXITCODE -ne 0) { throw "$Service remote image build failed; inspect the ACR run log." }
  $app = if ($Service -eq 'api') { $env:API_APP_NAME } else { $env:BRIDGE_APP_NAME }
  az containerapp update --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP -n $app `
    --image "$($env:AZURE_CONTAINER_REGISTRY_ENDPOINT)/$image" @environmentArguments -o none
  if ($LASTEXITCODE -ne 0) { throw "$Service revision deployment failed." }
  Write-Check PASS "$Service deployed" "$($env:AZURE_CONTAINER_REGISTRY_ENDPOINT)/$image; existing environment/secrets retained"
}
finally { Remove-Item -LiteralPath $work -Recurse -Force }
