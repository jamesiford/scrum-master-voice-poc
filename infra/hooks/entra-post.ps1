# Runs in `postprovision`, once the apps exist: portal redirect URIs and Easy Auth on both apps. Idempotent.
# Container Apps Easy Auth takes a client secret only (Learn: container-apps/authentication-entra); there is no federated credential
# option, so the secret is generated here, stored only as a Container Apps secret, and never passed through azd or Bicep parameters.
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$rg = $env:AZURE_RESOURCE_GROUP
$tenant = $env:AZURE_TENANT_ID
$clientId = $env:PORTAL_CLIENT_ID
$appObjectId = $env:PORTAL_APP_OBJECT_ID
if (-not $clientId) { throw 'PORTAL_CLIENT_ID is not set. The preprovision hook entra-portal-app.ps1 creates it.' }

# ---- redirect URIs ----------------------------------------------------------------------------------------------------------
$uris = @("$($env:API_URI)/.auth/login/aad/callback", "$($env:BRIDGE_URI)/.auth/login/aad/callback")
# The SPA (served by the API) signs in with MSAL.js, which needs the single-page-application platform, not the web one.
Invoke-Graph PATCH "/applications/$appObjectId" @{ web = @{ redirectUris = $uris; implicitGrantSettings = @{ enableIdTokenIssuance = $true } }; spa = @{ redirectUris = @($env:API_URI, "$($env:API_URI)/") } } | Out-Null
Write-Check PASS 'portal redirect URIs' ($uris -join ', ')

# ---- Microsoft Graph application permission for the API's identity ------------------------------------------------------------
# User.Read.All lets the API resolve a roster member's user principal name to an Entra object id (ADR 0017). It reads users only.
$graphAppId = '00000003-0000-0000-c000-000000000000'
$userReadAll = 'df021288-bdef-4463-88db-98f22de89214'
$graphSp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$graphAppId'&`$select=id").value[0]
$mi = $env:MANAGED_IDENTITY_PRINCIPAL_ID
$has = (Invoke-Graph GET "/servicePrincipals/$mi/appRoleAssignments").value | Where-Object { $_.appRoleId -eq $userReadAll -and $_.resourceId -eq $graphSp.id }
if (-not $has) {
  Invoke-Graph POST "/servicePrincipals/$mi/appRoleAssignments" @{ principalId = $mi; resourceId = $graphSp.id; appRoleId = $userReadAll } | Out-Null
  Write-Check PASS 'managed identity Graph permission' 'User.Read.All granted (roster identity resolution)'
}
else { Write-Check PASS 'managed identity Graph permission' 'User.Read.All already granted' }

# ---- Portal sign-in values for the API (idempotent) ---------------------------------------------------------------------------
# Bicep takes these as optional parameters, but azd may resolve them before preprovision has created the app, so they are applied here too.
$cur = az containerapp show -n $env:API_APP_NAME -g $rg --query "properties.template.containers[0].env[?name=='Portal__ClientId'].value | [0]" -o tsv
if ($cur -ne $clientId) {
  az containerapp update -n $env:API_APP_NAME -g $rg --set-env-vars "Portal__ClientId=$clientId" "Identity__AdminObjectIds=$($env:ADMIN_OBJECT_ID)" -o none
  Write-Check PASS 'API portal validation' "Portal__ClientId and Identity__AdminObjectIds applied to $($env:API_APP_NAME)"
}
else { Write-Check PASS 'API portal validation' 'already configured' }
# ---- Easy Auth --------------------------------------------------------------------------------------------------------------
$apps = @(
  @{ Name = $env:API_APP_NAME; Excluded = @('/api/messages', '/healthz', '/ws/bridge/*') }
  @{ Name = $env:BRIDGE_APP_NAME; Excluded = @('/teams/*', '/healthz') }
)

$azureCliClient = '04b07795-8ddb-461a-bbee-02f9e1bf7b46'
function Test-EasyAuth($name) {
  $url = "https://management.azure.com/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$rg/providers/Microsoft.App/containerApps/$name/authConfigs/current?api-version=2024-03-01"
  $cfg = az rest --method GET --url "`"$url`"" 2>$null | ConvertFrom-Json
  $hasSecret = (az containerapp secret list -n $name -g $rg --query "[?name=='portal-secret'].name" -o tsv 2>$null)
  [bool]($cfg -and $cfg.properties.identityProviders.azureActiveDirectory.registration.clientId -eq $clientId -and $cfg.properties.identityProviders.azureActiveDirectory.validation.defaultAuthorizationPolicy.allowedApplications -contains $azureCliClient -and $hasSecret)
}

$secretValue = $null
foreach ($app in $apps) {
  if (Test-EasyAuth $app.Name) { Write-Check PASS "Easy Auth: $($app.Name)" 'already configured'; continue }

  if (-not $secretValue) {
    # One credential for both apps. Older ones created by this script are removed so they do not pile up.
    $old = (Invoke-Graph GET "/applications/$appObjectId`?`$select=passwordCredentials").passwordCredentials | Where-Object { $_.displayName -like 'easyauth-*' }
    $new = Invoke-Graph POST "/applications/$appObjectId/addPassword" @{ passwordCredential = @{ displayName = "easyauth-$($env:AZURE_ENV_NAME)"; endDateTime = (Get-Date).AddYears(1).ToUniversalTime().ToString('o') } }
    $secretValue = $new.secretText
    foreach ($o in $old) { Invoke-Graph POST "/applications/$appObjectId/removePassword" @{ keyId = $o.keyId } | Out-Null }
  }

  az containerapp secret set -n $app.Name -g $rg --secrets "portal-secret=$secretValue" -o none
  $body = @{
    properties = @{
      platform         = @{ enabled = $true }
      globalValidation = @{ unauthenticatedClientAction = 'RedirectToLoginPage'; redirectToProvider = 'azureactivedirectory'; excludedPaths = $app.Excluded }
      identityProviders = @{
        azureActiveDirectory = @{
          enabled      = $true
          registration = @{ openIdIssuer = "https://login.microsoftonline.com/$tenant/v2.0"; clientId = $clientId; clientSecretSettingName = 'portal-secret' }
          # The Azure CLI client is pre-authorised on the portal app scope (entra-portal-app.ps1) so verify.ps1 and GateRunner can call in.
          validation   = @{ allowedAudiences = @("api://$clientId", $clientId); defaultAuthorizationPolicy = @{ allowedApplications = @($clientId, $azureCliClient) } }
        }
      }
    }
  }
  $tmp = New-TemporaryFile
  ($body | ConvertTo-Json -Depth 20) | Set-Content $tmp -Encoding utf8
  $url = "https://management.azure.com/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$rg/providers/Microsoft.App/containerApps/$($app.Name)/authConfigs/current?api-version=2024-03-01"
  az rest --method PUT --url "`"$url`"" --body "@$tmp" -o none
  Remove-Item $tmp
  Write-Check PASS "Easy Auth: $($app.Name)" 'configured, bearer tokens for api://<portal app> accepted'
}
$secretValue = $null
