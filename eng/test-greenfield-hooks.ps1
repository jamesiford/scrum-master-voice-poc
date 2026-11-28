# Focused pure hook regressions; no Azure or Graph mutation.
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\..\infra\hooks\lib.ps1"

$templateJson = az bicep build --file "$PSScriptRoot\..\infra\main.bicep" --stdout
if ($LASTEXITCODE -ne 0) { throw 'Cannot compile the real deployment output contract.' }
$template = $templateJson | ConvertFrom-Json
$parameters = Get-Content "$PSScriptRoot\..\infra\main.parameters.json" -Raw | ConvertFrom-Json
$configured = @{}
foreach ($parameter in $parameters.parameters.PSObject.Properties) {
  foreach ($variable in [regex]::Matches($parameter.Value.value, '\$\{([A-Z_]+)(?:=[^}]*)?\}')) {
    $configured[$variable.Groups[1].Value] = "configured-$($parameter.Name)"
  }
}
$configured.MEDIA_HOST_ENABLED = 'true'
$beforeDown = $configured.Clone()
foreach ($output in $template.outputs.PSObject.Properties.Name) { $configured.Remove($output) }
foreach ($inputName in $beforeDown.Keys) {
  if ($configured[$inputName] -ne $beforeDown[$inputName]) {
    throw "Down_ResourceOutputs_PreservesDeploymentInputs failed: $inputName"
  }
}

$existing = @(
  @{ resourceAppId = 'graph'; resourceAccess = @(@{ id = 'files'; type = 'Scope' }, @{ id = 'user'; type = 'Scope' }) }
  @{ resourceAppId = 'workiq'; resourceAccess = @(@{ id = 'ask'; type = 'Scope' }) })
$required = @(
  @{ resourceAppId = 'graph'; resourceAccess = @(@{ id = 'user'; type = 'Scope' }) }
  @{ resourceAppId = 'ado'; resourceAccess = @(@{ id = 'impersonation'; type = 'Scope' }) })
$merged = @(Merge-RequiredResourceAccess $existing $required)
if ($merged.Count -ne 3 -or @(($merged | Where-Object resourceAppId -eq graph).resourceAccess).Count -ne 2 -or
  ($merged | Where-Object resourceAppId -eq workiq).resourceAccess[0].id -ne 'ask') {
  throw 'Merge_OptionalDeclarations_PreservesAndDeduplicates failed'
}
$again = @(Merge-RequiredResourceAccess $merged $required) | ConvertTo-Json -Depth 10 -Compress
if ($again -ne ($merged | ConvertTo-Json -Depth 10 -Compress)) { throw 'Merge_RepeatedRun_IsIdempotent failed' }
$empty = @(Merge-RequiredResourceAccess $null $required)
if ($empty.Count -ne 2) { throw 'Merge_EmptyApplication_AddsBaseline failed' }

foreach ($case in @(
  @{ name = 'zavag9'; location = 'westus3'; group = 'rg-zava-scrum-zavag9'; allowed = $true }
  @{ name = 'zavaw3'; location = 'westus3'; group = 'rg-zava-scrum-zavaw3'; allowed = $true }
  @{ name = 'zavag8'; location = 'westus3'; group = 'rg-zava-scrum-zavag8'; allowed = $false }
  @{ name = 'mcm'; location = 'westus3'; group = 'rg-mcm-scrum-demo-wus3'; allowed = $false }
  @{ name = 'zavag9'; location = 'eastus2'; group = 'rg-zava-scrum-zavag9'; allowed = $false }
  @{ name = 'zavagf'; location = 'eastus2'; group = 'rg-zava-scrum-zavaw3'; allowed = $false })) {
  $env:AZURE_ENV_NAME = $case.name
  $env:AZURE_LOCATION = $case.location
  $env:AZURE_RESOURCE_GROUP = $case.group
  $env:AZURE_SUBSCRIPTION_ID = '4b0dd31f-5379-4acc-8dcd-60f2929d2513'
  $accepted = $true
  try { Assert-MediaDeploymentEnvironment } catch { $accepted = $false }
  if ($accepted -ne $case.allowed) { throw "Environment_ExactScope_Enforced failed: $($case | ConvertTo-Json -Compress)" }
}
$env:AZURE_ENV_NAME = 'zavag9'
$env:AZURE_LOCATION = 'westus3'
$env:AZURE_RESOURCE_GROUP = 'rg-zava-scrum-zavag9'
$env:AZURE_SUBSCRIPTION_ID = 'another-subscription'
$refused = $false
try { Assert-MediaDeploymentEnvironment } catch { $refused = $true }
if (-not $refused) { throw 'Environment_WrongSubscription_Refused failed' }
$preflight = [System.Management.Automation.Language.Parser]::ParseFile(
  "$PSScriptRoot\..\infra\hooks\preflight.ps1", [ref]$null, [ref]$null)
$modelLoop = $preflight.Find({
  param($node)
  $node -is [System.Management.Automation.Language.ForEachStatementAst] -and
    $node.Condition.Extent.Text -eq '$regions.requiredModels'
}, $true)
if (-not $modelLoop) { throw 'Cannot locate the real model quota preflight loop.' }
$modelBlock = [scriptblock]::Create($modelLoop.Extent.Text)
$regions = @{ requiredModels = @(
  @{ name = 'voice'; sku = '{realtimeSku}' }
  @{ name = 'chat'; sku = 'GlobalStandard' }
  @{ name = 'decision'; sku = 'GlobalStandard' }
) }
$catalogue = @()
$sku = 'GlobalStandard'
$loc = 'westus3'
foreach ($case in @(
  @{ value = 'false'; failures = 2 }
  @{ value = 'true'; failures = 3 }
  @{ value = ''; failures = 3 }
)) {
  $env:PROVISION_REALTIME_MODELS = $case.value
  $script:Failures = 0
  & $modelBlock | Out-Null
  if ($script:Failures -ne $case.failures) { throw "ManagedVoice_OnlyOptionalRealtimeQuotaSkipped failed: $($case.value)" }
}
$env:PROVISION_REALTIME_MODELS = $null
$capacity = $preflight.Find({
  param($node)
  $node -is [System.Management.Automation.Language.IfStatementAst] -and
    $node.Clauses[0].Item1.Extent.Text -eq '$env:MEDIA_HOST_ENABLED -eq ''true'''
}, $true)
if (-not $capacity) { throw 'Cannot locate the real native-capacity preflight block.' }
$capacityBlock = [scriptblock]::Create($capacity.Extent.Text)
function az {
  $global:LASTEXITCODE = 0
  $command = "$($args[0]) $($args[1])"
  switch ($command) {
    'vm list-skus' { ConvertTo-Json -InputObject @($script:sku) -Depth 10 -Compress }
    'vm list-usage' { ConvertTo-Json -InputObject $script:usage -Depth 10 -Compress }
    'group exists' { if ($script:existingVm) { 'true' } else { 'false' } }
    'vm list' { ConvertTo-Json -InputObject @(@{ location = 'westus3'; hardwareProfile = @{ vmSize = 'test-size' } }) -Compress }
    default { throw "Unexpected cloud command in capacity fixture: $command" }
  }
}
$env:MEDIA_HOST_ENABLED = 'true'
$env:MEDIA_VM_SIZE = 'test-size'
$env:MEDIA_VM_IMAGE_SKU = '2022-datacenter-g2'
$loc = 'westus3'
foreach ($case in @(
  @{ name = 'restricted'; restricted = $true; cores = 4; available = 4; gen = 'V2'; free = 100; existing = $false; pass = $false }
  @{ name = 'small'; restricted = $false; cores = 2; available = 2; gen = 'V2'; free = 100; existing = $false; pass = $false }
  @{ name = 'constrained'; restricted = $false; cores = 4; available = 2; gen = 'V2'; free = 100; existing = $false; pass = $false }
  @{ name = 'generation'; restricted = $false; cores = 4; available = 4; gen = 'V1'; free = 100; existing = $false; pass = $false }
  @{ name = 'empty'; restricted = $false; cores = 4; available = 4; gen = 'V2'; free = 100; existing = $false; pass = $true }
  @{ name = 'own-capacity'; restricted = $false; cores = 4; available = 4; gen = 'V2'; free = 0; existing = $true; pass = $true }
  @{ name = 'regional-quota'; restricted = $false; cores = 4; available = 4; gen = 'V2'; free = 0; existing = $false; pass = $false })) {
  $script:sku = @{
    name = 'test-size'; family = 'test-family'
    restrictions = @()
    capabilities = @(
      @{ name = 'vCPUs'; value = "$($case.cores)" }
      @{ name = 'vCPUsAvailable'; value = "$($case.available)" }
      @{ name = 'CpuArchitectureType'; value = 'x64' }
      @{ name = 'HyperVGenerations'; value = $case.gen })
  }
  if ($case.restricted) { $script:sku.restrictions = @(@{ reasonCode = 'NotAvailableForSubscription' }) }
  $script:usage = @(
    @{ name = @{ value = 'test-family' }; currentValue = $(if ($case.name -eq 'regional-quota') { 0 } else { 100 - $case.free }); limit = 100 }
    @{ name = @{ value = 'cores' }; currentValue = 100 - $case.free; limit = 100 })
  $script:existingVm = $case.existing
  $script:Failures = 0
  & $capacityBlock | Out-Null
  if (($script:Failures -eq 0) -ne $case.pass) { throw "Capacity_$($case.name)_ExpectedVerdict failed" }
}
Write-Host 'PASS 21 focused greenfield hook checks'
