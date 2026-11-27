# Runs before `azd provision`. Fails fast, with an explanation, on anything that would otherwise fail ten minutes into a deployment.
# Each line is PASS, WARN or FAIL. Exit code 1 when any check fails. Run by hand with: pwsh infra/hooks/preflight.ps1
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment
$regions = Get-Content "$PSScriptRoot/regions.json" -Raw | ConvertFrom-Json

Write-Host "Preflight for environment '$env:AZURE_ENV_NAME' in '$env:AZURE_LOCATION'"

# ---- tools -------------------------------------------------------------------------------------------------------------------
foreach ($tool in 'az', 'azd', 'dotnet') {
  if (Get-Command $tool -ErrorAction SilentlyContinue) { Write-Check PASS "tool: $tool" } else { Write-Check FAIL "tool: $tool" "Install $tool; the hooks and the Azure DevOps seeder need it." }
}

# ---- sign-in -----------------------------------------------------------------------------------------------------------------
$account = az account show -o json 2>$null | ConvertFrom-Json
if (-not $account) { Write-Check FAIL 'az login' 'Run: az login --tenant <tenant-id>'; exit 1 }
$sub = if ($env:AZURE_SUBSCRIPTION_ID) { $env:AZURE_SUBSCRIPTION_ID } else { $account.id }
if ($env:AZURE_TENANT_ID -and $env:AZURE_TENANT_ID -ne $account.tenantId) {
  Write-Check FAIL 'tenant' "Signed in to $($account.tenantId) but AZURE_TENANT_ID is $env:AZURE_TENANT_ID. Run: az login --tenant $env:AZURE_TENANT_ID"
}
else { Write-Check PASS 'sign-in' "$($account.user.name), tenant $($account.tenantId), subscription $sub" }

# ---- required values ---------------------------------------------------------------------------------------------------------
foreach ($name in 'ADO_ORG_URL', 'ADMIN_UPN', 'AZURE_LOCATION') {
  if (-not [Environment]::GetEnvironmentVariable($name)) { Write-Check FAIL "value: $name" "Run: azd env set $name <value>" } else { Write-Check PASS "value: $name" }
}
if ($script:Failures -gt 0) { Write-Host "`n$script:Failures check(s) failed. Fix them and run again."; exit 1 }

# ---- Azure roles of the deployer ---------------------------------------------------------------------------------------------
$me = try { (Invoke-Graph GET '/me?$select=id').id } catch { $null }
$roles = if ($me) { az role assignment list --assignee $me --include-inherited --include-groups --scope "/subscriptions/$sub" --query '[].roleDefinitionName' -o tsv 2>$null } else { @() }
$hasOwner = $roles -contains 'Owner'
$hasWrite = $hasOwner -or ($roles -contains 'Contributor' -and (($roles -contains 'User Access Administrator') -or ($roles -contains 'Role Based Access Control Administrator')))
if ($hasWrite) { Write-Check PASS 'Azure roles' 'can create resources and role assignments' }
else { Write-Check FAIL 'Azure roles' "Need Owner, or Contributor plus User Access Administrator, on subscription $sub (the template assigns roles). Found: $($roles -join ', ')" }

# ---- resource providers ------------------------------------------------------------------------------------------------------
foreach ($rp in $regions.resourceProviders) {
  $state = az provider show -n $rp --query registrationState -o tsv 2>$null
  if ($state -eq 'Registered') { continue }
  az provider register -n $rp --wait 2>&1 | Out-Null
  Write-Check PASS "provider: $rp" "registered (was $state)"
}

# ---- Microsoft Entra permissions ---------------------------------------------------------------------------------------------
try {
  $claims = Get-TokenClaims (Get-GraphToken)
  $scopes = "$($claims.scp)".Split(' ')
  $missing = @('Application.ReadWrite.All', 'AppRoleAssignment.ReadWrite.All') | Where-Object { $_ -notin $scopes }
  if ($missing) { Write-Check FAIL 'Entra permissions' "The Azure CLI token lacks $($missing -join ', '). Sign in as a Cloud Application Administrator or Global Administrator (activate the role first)." }
  else {
    Invoke-Graph GET '/applications?$top=1&$select=id' | Out-Null
    Write-Check PASS 'Entra permissions' 'can create app registrations and role assignments'
  }
}
catch { Write-Check FAIL 'Entra permissions' $_.Exception.Message.Split("`n")[0] }

# ---- Azure DevOps ------------------------------------------------------------------------------------------------------------
try {
  $h = Get-AdoHeaders
  $org = Get-AdoOrgName
  $cd = Invoke-RestMethod "https://dev.azure.com/$org/_apis/connectionData" -Headers $h
  $descriptor = $cd.authenticatedUser.subjectDescriptor
  $groups = (Invoke-RestMethod "https://vssps.dev.azure.com/$org/_apis/graph/groups?api-version=7.1-preview.1" -Headers $h).value
  $pca = ($groups | Where-Object { $_.displayName -eq 'Project Collection Administrators' -and $_.principalName -eq "[$org]\Project Collection Administrators" } | Select-Object -First 1).descriptor
  $up = (Invoke-RestMethod "https://vssps.dev.azure.com/$org/_apis/graph/memberships/$($descriptor)?direction=up&api-version=7.1-preview.1" -Headers $h).value.containerDescriptor
  if ($pca -and $pca -in $up) { Write-Check PASS 'Azure DevOps' "$($cd.authenticatedUser.providerDisplayName) is a Project Collection Administrator of $org" }
  else { Write-Check FAIL 'Azure DevOps' "Sign-in user must be a Project Collection Administrator of $org to create the project and add the managed identity. Ask one to run the hook, or add you under Organization settings > Permissions." }
}
catch { Write-Check FAIL 'Azure DevOps' "Cannot reach $env:ADO_ORG_URL with the signed-in user: $($_.Exception.Message.Split("`n")[0])" }

# ---- region, models and quota ------------------------------------------------------------------------------------------------
$loc = $env:AZURE_LOCATION
if ($loc -in $regions.hostedAgents.measured) { Write-Check PASS 'hosted agents region' "$loc (measured, ADR 0015)" }
elseif ($loc -in $regions.hostedAgents.documented) { Write-Check WARN 'hosted agents region' "$loc is documented on Learn but was not exercised here" }
else { Write-Check FAIL 'hosted agents region' "$loc is not a documented hosted agent region" }

$sku = if ($env:REALTIME_SKU) { $env:REALTIME_SKU } else { 'DataZoneStandard' }
$catalogue = az cognitiveservices model list -l $loc -o json 2>$null | ConvertFrom-Json
$usage = az cognitiveservices usage list -l $loc -o json 2>$null | ConvertFrom-Json
# On a re-run this environment already holds part of the quota. Its own deployments are added back, so azd up stays repeatable.
$ownDeployments = @()
foreach ($acct in (az resource list --tag "azd-env-name=$($env:AZURE_ENV_NAME)" -o json 2>$null | ConvertFrom-Json | Where-Object { $_.type -eq 'Microsoft.CognitiveServices/accounts' })) {
  $ownDeployments += az cognitiveservices account deployment list -g $acct.resourceGroup -n $acct.name -o json 2>$null | ConvertFrom-Json
}
foreach ($m in $regions.requiredModels) {
  if ($env:PROVISION_REALTIME_MODELS -eq 'false' -and $m.sku -eq '{realtimeSku}') {
    # https://learn.microsoft.com/azure/ai-services/speech-service/voice-live#supported-models-and-regions
    Write-Check PASS "managed Voice Live: $($m.name)" 'Service-managed model; no standalone Azure OpenAI deployment or its quota is required.'
    continue
  }
  $deploySku = $m.sku.Replace('{realtimeSku}', $sku)
  $found = $catalogue | Where-Object { $_.model.name -eq $m.name -and ($_.model.skus.name -contains $deploySku) } | Select-Object -First 1
  if (-not $found) { Write-Check FAIL "model: $($m.name)" "Not offered as $deploySku in $loc for this subscription (check with: az cognitiveservices model list -l $loc)"; continue }
  $want = [int]$(if ([Environment]::GetEnvironmentVariable($m.capacityParam)) { [Environment]::GetEnvironmentVariable($m.capacityParam) } else { $m.capacityDefault })
  $row = $usage | Where-Object { $_.name.value -eq $m.usage.Replace('{sku}', $deploySku) }
  if (-not $row) { Write-Check WARN "quota: $($m.name)" "No quota row for $($m.usage.Replace('{sku}', $deploySku)); the deployment will show the real answer"; continue }
  $own = 0
  foreach ($d in ($ownDeployments | Where-Object { $_.properties.model.name -eq $m.name -and $_.sku.name -eq $deploySku })) { $own += [int]$d.sku.capacity }
  $free = $row.limit - ($row.currentValue - $own)
  if ($free -lt $want) {
    $hint = if ($deploySku -eq 'GlobalStandard' -and $m.sku -eq '{realtimeSku}') { ' Try: azd env set REALTIME_SKU DataZoneStandard' } else { " Lower $($m.capacityParam) or free quota." }
    Write-Check FAIL "quota: $($m.name) ($deploySku)" "Needs $want, only $free of $($row.limit) free in $loc.$hint"
  }
  else { Write-Check PASS "quota: $($m.name) ($deploySku)" "$free of $($row.limit) free, need $want$(if ($own) { " ($own already held by this environment)" })" }
}

# ---- tenant policy that already bit us ---------------------------------------------------------------------------------------
$deny = az policy assignment list --scope "/subscriptions/$sub" --query "[?enforcementMode=='Default'].policyDefinitionId" -o tsv 2>$null
$cosmosDeny = $false
foreach ($id in ($deny | Select-Object -Unique)) {
  if (-not $id -or $id -like '*policySetDefinitions*') { continue }
  $def = az policy definition show --name ($id.Split('/')[-1]) -o json 2>$null | ConvertFrom-Json
  if ($def -and ($def.policyRule | ConvertTo-Json -Depth 20) -match 'DocumentDB/databaseAccounts' -and ($def.policyRule | ConvertTo-Json -Depth 20) -match 'publicNetworkAccess' -and ($def.policyRule.then.effect -match 'deny|Deny' -or $def.parameters.effect.defaultValue -match 'Deny')) { $cosmosDeny = $true }
}
if ($cosmosDeny) { Write-Check WARN 'policy: Cosmos DB public access' 'A deny policy covers Cosmos DB network access. The template tags the resource group SecurityControl=Ignore, which the tenant policy honours (ADR 0006).' }
else { Write-Check PASS 'policy: Cosmos DB public access' 'no blocking policy found at subscription scope (the resource group is still tagged SecurityControl=Ignore)' }

# ---- Windows VM (prompt 6) ---------------------------------------------------------------------------------------------------
if ($env:MEDIA_HOST_ENABLED -eq 'true') {
  $size = if ($env:MEDIA_VM_SIZE) { $env:MEDIA_VM_SIZE } else { 'Standard_D4s_v5' }
  $catalogue = az vm list-skus -l $loc --resource-type virtualMachines --size $size --all -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw "Cannot read the VM SKU catalogue in $loc." }
  $vmSku = $catalogue | Where-Object name -eq $size | Select-Object -First 1
  if (-not $vmSku) { Write-Check FAIL "SKU: Windows VM $size" "No exact catalogue entry in $loc" }
  elseif (@($vmSku.restrictions).Count -gt 0) {
    Write-Check FAIL "SKU: Windows VM $size" "Restricted in ${loc}: $($vmSku.restrictions | ConvertTo-Json -Depth 10 -Compress)"
  }
  else {
    $cores = [int]($vmSku.capabilities | Where-Object name -eq 'vCPUs').value
    $available = ($vmSku.capabilities | Where-Object name -eq 'vCPUsAvailable').value
    $architecture = ($vmSku.capabilities | Where-Object name -eq 'CpuArchitectureType').value
    $generations = "$(($vmSku.capabilities | Where-Object name -eq 'HyperVGenerations').value)".Split(',')
    $generation = if ($env:MEDIA_VM_IMAGE_SKU -eq '2022-datacenter-g2') { 'V2' } else { 'V1' }
    if ($cores -lt 4 -or ($available -and [int]$available -lt 4)) { Write-Check FAIL "SKU: Windows VM $size" "Native media requires at least four available vCPUs; found $cores/$available" }
    elseif ($architecture -eq 'Arm64' -or $generation -notin $generations) {
      Write-Check FAIL "SKU: Windows VM $size" "The win-x64 $generation image is incompatible with $architecture/$($generations -join ',')"
    }
    else {
      $usage = az vm list-usage -l $loc -o json | ConvertFrom-Json
      if ($LASTEXITCODE -ne 0) { throw "Cannot read VM quota in $loc." }
      $own = 0
      $group = "rg-zava-scrum-$($env:AZURE_ENV_NAME)"
      $exists = az group exists -n $group -o tsv
      if ($LASTEXITCODE -ne 0) { throw 'Cannot check this environment''s resource group.' }
      $vms = @()
      if ($exists -eq 'true') {
        $vms = az vm list -g $group -o json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read this environment''s existing VM capacity.' }
      }
      foreach ($vm in @($vms | Where-Object { $_.location -eq $loc -and $_.hardwareProfile.vmSize -eq $size })) { $own += $cores }
      foreach ($family in @($vmSku.family, 'cores')) {
        $row = $usage | Where-Object { $_.name.value -eq $family } | Select-Object -First 1
        if (-not $row) { Write-Check FAIL "quota: Windows VM $size ($family)" 'No quota row returned; capacity is not verified' }
        elseif (($row.limit - $row.currentValue + $own) -lt $cores) {
          Write-Check FAIL "quota: Windows VM $size ($family)" "Needs $cores vCPUs, $($row.limit - $row.currentValue + $own) free in $loc"
        }
        else { Write-Check PASS "quota: Windows VM $size ($family)" "$($row.limit - $row.currentValue + $own) free, need $cores ($own already held by this environment)" }
      }
    }
  }
}
else { Write-Check SKIP 'quota: Windows VM' 'MEDIA_HOST_ENABLED is not true' }

Write-Host ''
if ($script:Failures -gt 0) { Write-Host "$script:Failures check(s) failed. Nothing was provisioned." -ForegroundColor Red; exit 1 }
Write-Host 'Preflight passed.' -ForegroundColor Green
