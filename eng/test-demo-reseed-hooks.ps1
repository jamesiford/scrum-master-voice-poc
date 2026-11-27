# Execute the real configuration block with a read-only CLI fixture; no cloud mutation.
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'
foreach ($file in @('demo-reseed.ps1', 'demo-reseed-scope.ps1', 'deploy-service.ps1')) {
  $errors = $null
  $null = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $root "infra\hooks\$file"), [ref]$null, [ref]$errors)
  if ($errors.Count -gt 0) { throw "Hook syntax failed: $file" }
}
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
  (Join-Path $root 'infra\hooks\deploy-service.ps1'), [ref]$null, [ref]$null)
$configuration = $ast.Find({
  param($node)
  $node -is [System.Management.Automation.Language.IfStatementAst] -and
    $node.Clauses[0].Item1.Extent.Text.StartsWith('$Service -eq ''api'' -and ($env:DEMO_RESET_TENANT_ID')
}, $true)
if (-not $configuration) { throw 'Cannot locate the real demo configuration block.' }
$block = [scriptblock]::Create($configuration.Extent.Text)
function az {
  if ("$($args[0]) $($args[1])" -ne 'containerapp show') {
    throw 'The configuration fixture permits only environment-name reads.'
  }
  $script:calls++
  $global:LASTEXITCODE = if ($script:readFails) { 1 } else { 0 }
  ConvertTo-Json -InputObject @('Portal__ClientId', 'Demo__ResetTeamIds__0',
    'Demo__ResetTeamIds__1', 'Demo__ResetTeamIds__2', 'Bridge__ApiKey')
}
$env:AZURE_TENANT_ID = 'reviewed-tenant'
foreach ($case in @(
  @{ name = 'api'; service = 'api'; tenant = 'reviewed-tenant'; ids = 'first,second'; fails = $false; readFails = $false }
  @{ name = 'bridge'; service = 'bridge'; tenant = 'reviewed-tenant'; ids = 'first,second'; fails = $false; readFails = $false }
  @{ name = 'foreign'; service = 'api'; tenant = 'foreign'; ids = 'first'; fails = $true; readFails = $false }
  @{ name = 'duplicate'; service = 'api'; tenant = 'reviewed-tenant'; ids = 'first,first'; fails = $true; readFails = $false }
  @{ name = 'empty'; service = 'api'; tenant = 'reviewed-tenant'; ids = 'first,,second'; fails = $true; readFails = $false }
  @{ name = 'partial'; service = 'api'; tenant = 'reviewed-tenant'; ids = ''; fails = $true; readFails = $false }
  @{ name = 'read-failure'; service = 'api'; tenant = 'reviewed-tenant'; ids = 'first'; fails = $true; readFails = $true }
)) {
  $Service = $case.service
  $env:DEMO_RESET_TENANT_ID = $case.tenant
  $env:DEMO_RESET_TEAM_IDS = $case.ids
  $environmentArguments = @()
  $calls = 0
  $readFails = $case.readFails
  $failed = $false
  try { . $block } catch { $failed = $true }
  if ($failed -ne $case.fails) { throw "Configuration_$($case.name)_ExpectedOutcome failed." }
  if ($case.name -eq 'api') {
    $expected = @('--set-env-vars', 'Demo__ResetTenantId=reviewed-tenant',
      'Demo__ResetTeamIds__0=first', 'Demo__ResetTeamIds__1=second',
      '--remove-env-vars', 'Demo__ResetTeamIds__2')
    if (($environmentArguments -join '|') -ne ($expected -join '|') -or $calls -ne 1) {
      throw 'Configuration_Api_PreservesOtherKeysAndRemovesObsoleteIndices failed.'
    }
  } elseif ($case.name -eq 'bridge' -and ($environmentArguments.Count -ne 0 -or $calls -ne 0)) {
    throw 'Configuration_Bridge_DoesNotChangeEnvironment failed.'
  } elseif ($case.fails -and -not $case.readFails -and $calls -ne 0) {
    throw 'Configuration_InvalidInput_RefusesBeforeCloudRead failed.'
  }
}
Write-Host 'PASS 7 demo configuration cases and 3 hook syntax checks; no cloud mutation.'
