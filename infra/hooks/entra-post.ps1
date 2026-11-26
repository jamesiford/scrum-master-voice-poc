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
$uris = @("$($env:API_URI)/.auth/login/aad/callback", "$($env:BRIDGE_URI)/.auth/login/aad/callback", 'https://token.botframework.com/.auth/web/redirect')
# The SPA (served by the API) signs in with MSAL.js, which needs the single-page-application platform, not the web one.
Invoke-Graph PATCH "/applications/$appObjectId" @{ web = @{ redirectUris = $uris; implicitGrantSettings = @{ enableIdTokenIssuance = $true } }; spa = @{ redirectUris = @($env:API_URI, "$($env:API_URI)/") } } | Out-Null
Write-Check PASS 'portal redirect URIs' ($uris -join ', ')

# ---- Microsoft Graph application permissions for the API's identity ----------------------------------------------------------
# User.Read.All lets the API resolve a roster member's user principal name to an Entra object id (ADR 0017). GroupMember.Read.All lets it
# sync a team's memberships from an Entra group (ADR 0018). Both read only. Role ids are looked up by name on the Graph service principal.
$graphAppId = '00000003-0000-0000-c000-000000000000'
$graphSp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$graphAppId'&`$select=id,appRoles").value[0]
$mi = $env:MANAGED_IDENTITY_PRINCIPAL_ID
$granted = (Invoke-Graph GET "/servicePrincipals/$mi/appRoleAssignments").value
foreach ($roleName in @('User.Read.All', 'GroupMember.Read.All')) {
  $role = $graphSp.appRoles | Where-Object { $_.value -eq $roleName -and $_.allowedMemberTypes -contains 'Application' }
  if (-not $role) { throw "Graph has no application role $roleName." }
  $has = $granted | Where-Object { $_.appRoleId -eq $role.id -and $_.resourceId -eq $graphSp.id }
  if (-not $has) {
    Invoke-Graph POST "/servicePrincipals/$mi/appRoleAssignments" @{ principalId = $mi; resourceId = $graphSp.id; appRoleId = $role.id } | Out-Null
    Write-Check PASS "managed identity Graph permission $roleName" 'granted'
  }
  else { Write-Check PASS "managed identity Graph permission $roleName" 'already granted' }
}
# ---- Delegated Azure DevOps writes (ADR 0020): no client secret anywhere ---------------------------------------------------------
# An approval is written as the approver. The API exchanges the person's token on their behalf (OBO) and proves itself to Entra with the
# managed identity through a federated credential on the portal app, so there is no client secret. Teams card actions get the person's
# token through single sign-on: a Bot Service OAuth connection ("AAD v2 with Federated Credentials") plus a second federated credential
# for the Bot Service token store.
# Checked against Learn (microsoft-365/agents-sdk/azure-bot-user-authorization-federated-credentials): issuer https://login.microsoftonline.com/<tenant>/v2.0,
# subject /eid1/c/pub/t/<base64url tenant>/a/<base64url first-party app>/<unique id> (the Bot Service Token Store app is 9ExAW52n_ky4ZiS_jhpJIQ),
# audience api://AzureADTokenExchange, provider "AAD v2 with Federated Credentials", single tenant, redirect https://token.botframework.com/.auth/web/redirect.
function ConvertTo-Base64Url([byte[]]$Bytes) { [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }
function Set-FederatedCredential([string]$Name, [string]$Subject, [string]$Description) {
  $existing = (Invoke-Graph GET "/applications/$appObjectId/federatedIdentityCredentials").value | Where-Object { $_.name -eq $Name }
  $body = @{ name = $Name; issuer = "https://login.microsoftonline.com/$tenant/v2.0"; subject = $Subject; audiences = @('api://AzureADTokenExchange'); description = $Description }
  if (-not $existing) { Invoke-Graph POST "/applications/$appObjectId/federatedIdentityCredentials" $body | Out-Null; Write-Check PASS "federated credential $Name" 'created' }
  elseif ($existing.subject -ne $Subject) { Invoke-Graph PATCH "/applications/$appObjectId/federatedIdentityCredentials/$($existing.id)" @{ subject = $Subject } | Out-Null; Write-Check PASS "federated credential $Name" 'subject updated' }
  else { Write-Check PASS "federated credential $Name" 'already configured' }
}
Set-FederatedCredential 'zava-api-managed-identity' $mi 'The API managed identity proves the portal app for the on-behalf-of exchange to Azure DevOps.'

if (-not $env:BOT_SSO_UNIQUE_ID) { Set-AzdValue BOT_SSO_UNIQUE_ID ([guid]::NewGuid().ToString()); Import-AzdEnvironment }
# The Bot Service token store first-party application id, and the tenant, encoded as the federated subject documents describe.
$tokenStoreApp = '9ExAW52n_ky4ZiS_jhpJIQ'
$tenantPart = ConvertTo-Base64Url ([Guid]$tenant).ToByteArray()
Set-FederatedCredential 'zava-bot-token-store' "/eid1/c/pub/t/$tenantPart/a/$tokenStoreApp/$($env:BOT_SSO_UNIQUE_ID)" 'The Azure Bot token store acts for the portal app in the single sign-on OAuth connection.'

$botUrl = "https://management.azure.com/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$rg/providers/Microsoft.BotService/botServices/$($env:BOT_NAME)/connections/sso?api-version=2022-09-15"
$connection = @{
  location   = 'global'
  properties = @{
    serviceProviderId          = 'c00b44ab-5e16-c44c-af26-2fd5bc55eb18'
    serviceProviderDisplayName = 'Aadv2WithFic'
    clientId                   = $clientId
    scopes                     = "api://botid-$clientId/access_as_user"
    parameters                 = @(
      @{ key = 'ClientId'; value = $clientId }
      @{ key = 'UniqueIdentifier'; value = $env:BOT_SSO_UNIQUE_ID }
      @{ key = 'TokenExchangeUrl'; value = "api://botid-$clientId" }
      @{ key = 'TenantId'; value = $tenant }
    )
  }
}
$tmp = New-TemporaryFile
($connection | ConvertTo-Json -Depth 10) | Set-Content $tmp -Encoding utf8
az rest --method PUT --url "`"$botUrl`"" --body "@$tmp" -o none
if ($LASTEXITCODE -ne 0) { Remove-Item $tmp; throw 'The Azure Bot OAuth connection for Teams single sign-on could not be created.' }
Remove-Item $tmp
Write-Check PASS 'bot OAuth connection sso' "Entra ID v2 with federated credentials, app $clientId"

# The agent signs card actions in with that connection. Auto sign-in stays off: only the approve actions ask for a token.
$ssoEnv = @('AgentApplication__UserAuthorization__AutoSignIn=false', 'AgentApplication__UserAuthorization__Handlers__sso__Settings__AzureBotOAuthConnectionName=sso')
az containerapp update -n $env:API_APP_NAME -g $rg --set-env-vars @ssoEnv -o none
Write-Check PASS 'API delegated writes' 'Teams single sign-on handler applied (the on-behalf-of client id is Portal__ClientId)'
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
  @{ Name = $env:API_APP_NAME; Excluded = @('/api/messages', '/healthz', '/ws/bridge/*', '/api/media/context', '/invocations_ws') }
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
