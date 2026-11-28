# Shared helpers for the azd hooks. Dot-source: . "$PSScriptRoot/lib.ps1"
$ErrorActionPreference = 'Stop'
# The Azure CLI exits non-zero when Graph returns characters the console code page cannot print, so force UTF-8.
$env:PYTHONIOENCODING = 'utf-8'
$env:PYTHONUTF8 = '1'
$script:Failures = 0

# Hooks run with the azd environment already in the process environment. When a script is run by hand, load it from `azd env get-values`.
function Import-AzdEnvironment {
  if ($env:AZURE_ENV_NAME) { return }
  foreach ($line in (azd env get-values 2>$null)) {
    if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)="?(.*?)"?$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2]) }
  }
}

# Read the existing ingestion setting without logging it or persisting it in the azd environment.
function Get-InsightsConnectionString {
  if (-not $env:APPINSIGHTS_NAME) {
    $names = az resource list --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP `
      --resource-type Microsoft.Insights/components --query '[].name' -o json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or @($names).Count -ne 1) { throw 'Expected exactly one Insights component in the intended environment.' }
    $env:APPINSIGHTS_NAME = @($names)[0]
  }
  $connection = az resource show --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP `
    --resource-type Microsoft.Insights/components -n $env:APPINSIGHTS_NAME --query properties.ConnectionString -o tsv
  if ($LASTEXITCODE -ne 0 -or -not $connection) { throw 'Cannot read the existing Insights ingestion setting.' }
  return $connection
}

# Writes a value into the azd environment so later hooks and `azd deploy` see it. Never used for secrets.
function Set-AzdValue([string]$Name, [string]$Value) {
  azd env set $Name $Value | Out-Null
  [Environment]::SetEnvironmentVariable($Name, $Value)
}

function Write-Check([string]$Status, [string]$Name, [string]$Detail = '') {
  $colour = switch ($Status) { 'PASS' { 'Green' } 'WARN' { 'Yellow' } 'GATE' { 'Yellow' } 'SKIP' { 'DarkGray' } default { 'Red' } }
  Write-Host ('{0,-5} {1}' -f $Status, $Name) -ForegroundColor $colour -NoNewline
  if ($Detail) { Write-Host "  $Detail" } else { Write-Host '' }
  if ($Status -eq 'FAIL') { $script:Failures++ }
}

# Preserve separately approved optional permissions when reconciling the baseline application declarations.
function Merge-RequiredResourceAccess($Existing, $Required) {
  $resources = [ordered]@{}
  foreach ($resource in @($Existing) + @($Required)) {
    if (-not $resource) { continue }
    if (-not $resources.Contains($resource.resourceAppId)) { $resources[$resource.resourceAppId] = [ordered]@{} }
    foreach ($permission in $resource.resourceAccess) {
      $resources[$resource.resourceAppId]["$($permission.type):$($permission.id)"] = @{
        id = $permission.id; type = $permission.type
      }
    }
  }
  foreach ($id in $resources.Keys) {
    @{ resourceAppId = $id; resourceAccess = @($resources[$id].Values) }
  }
}

# Keep the existing deployment protected while permitting the fresh westus3 G8 proof.
function Assert-MediaDeploymentEnvironment {
  $allowed = ($env:AZURE_ENV_NAME -eq 'zavaw3' -and $env:AZURE_LOCATION -eq 'westus3') -or
    ($env:AZURE_ENV_NAME -eq 'zavag8' -and $env:AZURE_LOCATION -eq 'westus3')
  if (-not $allowed -or $env:AZURE_SUBSCRIPTION_ID -ne '4b0dd31f-5379-4acc-8dcd-60f2929d2513' -or
    $env:AZURE_RESOURCE_GROUP -ne "rg-zava-scrum-$($env:AZURE_ENV_NAME)") {
    throw 'Media deployment requires zavaw3/westus3 or zavag8/westus3 and its exact resource group in the designated subscription.'
  }
}

# Microsoft Graph through the signed-in Azure CLI session (the CLI's own token carries Application.ReadWrite.All and
# AppRoleAssignment.ReadWrite.All for an administrator).
function Invoke-Graph([string]$Method, [string]$Path, $Body = $null) {
  $uri = if ($Path.StartsWith('http')) { $Path } else { "https://graph.microsoft.com/v1.0$Path" }
  $azArgs = @('rest', '--method', $Method, '--url', "`"$uri`"", '--resource', 'https://graph.microsoft.com', '--headers', 'Content-Type=application/json')
  $tmp = $null
  if ($null -ne $Body) {
    $tmp = New-TemporaryFile
    ($Body | ConvertTo-Json -Depth 20 -Compress) | Set-Content $tmp -Encoding utf8
    $azArgs += @('--body', "@$tmp")
  }
  try {
    $out = & az @azArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($out | Out-String) }
    if ($out) { return ($out | Out-String | ConvertFrom-Json) }
  }
  finally { if ($tmp) { Remove-Item $tmp -ErrorAction SilentlyContinue } }
}

# --resource names Graph explicitly: without it the CLI can reuse a cached token minted under outdated policies (TokenCreatedWithOutdatedPolicies).
function Get-GraphToken { az account get-access-token --resource https://graph.microsoft.com --query accessToken -o tsv }

function Get-TokenClaims([string]$Jwt) {
  $p = $Jwt.Split('.')[1].Replace('-', '+').Replace('_', '/')
  $p = $p.PadRight([int]([Math]::Ceiling($p.Length / 4) * 4), '=')
  [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($p)) | ConvertFrom-Json
}

$script:AdoResource = '499b84ac-1321-427f-aa17-267ca6975798'
function Get-AdoHeaders {
  $t = az account get-access-token --resource $script:AdoResource --query accessToken -o tsv
  @{ Authorization = "Bearer $t"; 'Content-Type' = 'application/json' }
}

function Get-AdoOrgName { ($env:ADO_ORG_URL).TrimEnd('/').Split('/')[-1] }
