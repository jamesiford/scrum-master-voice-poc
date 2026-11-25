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
foreach ($m in $regions.requiredModels) {
  $deploySku = $m.sku.Replace('{realtimeSku}', $sku)
  $found = $catalogue | Where-Object { $_.model.name -eq $m.name -and ($_.model.skus.name -contains $deploySku) } | Select-Object -First 1
  if (-not $found) { Write-Check FAIL "model: $($m.name)" "Not offered as $deploySku in $loc for this subscription (check with: az cognitiveservices model list -l $loc)"; continue }
  $want = [int]$(if ([Environment]::GetEnvironmentVariable($m.capacityParam)) { [Environment]::GetEnvironmentVariable($m.capacityParam) } else { $m.capacityDefault })
  $row = $usage | Where-Object { $_.name.value -eq $m.usage.Replace('{sku}', $deploySku) }
  if (-not $row) { Write-Check WARN "quota: $($m.name)" "No quota row for $($m.usage.Replace('{sku}', $deploySku)); the deployment will show the real answer" }
  elseif (($row.limit - $row.currentValue) -lt $want) {
    $hint = if ($deploySku -eq 'GlobalStandard' -and $m.sku -eq '{realtimeSku}') { ' Try: azd env set REALTIME_SKU DataZoneStandard' } else { " Lower $($m.capacityParam) or free quota." }
    Write-Check FAIL "quota: $($m.name) ($deploySku)" ("Needs $want, only {0} of {1} free in $loc.$hint" -f ($row.limit - $row.currentValue), $row.limit)
  }
  else { Write-Check PASS "quota: $($m.name) ($deploySku)" ("{0} of {1} free, need $want" -f ($row.limit - $row.currentValue), $row.limit) }
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
if ($env:MEDIA_VM_SIZE) {
  $family = (az vm list-skus -l $loc --size $env:MEDIA_VM_SIZE --query '[0].family' -o tsv 2>$null)
  $u = az vm list-usage -l $loc --query "[?name.value=='$family']" -o json 2>$null | ConvertFrom-Json
  if ($u -and ($u.limit - $u.currentValue) -ge 4) { Write-Check PASS "quota: Windows VM $env:MEDIA_VM_SIZE" "$($u.limit - $u.currentValue) vCPU free" }
  else { Write-Check FAIL "quota: Windows VM $env:MEDIA_VM_SIZE" "Not enough $family vCPU quota in $loc" }
}
else { Write-Check SKIP 'quota: Windows VM' 'MEDIA_VM_SIZE is not set; the media bot VM arrives with prompt 6' }

Write-Host ''
if ($script:Failures -gt 0) { Write-Host "$script:Failures check(s) failed. Nothing was provisioned." -ForegroundColor Red; exit 1 }
Write-Host 'Preflight passed.' -ForegroundColor Green
