# Creates or updates the portal app registration (Easy Auth sign-in, and the API scope the gate harness uses). Idempotent: it finds the
# app by its tag, so a second run changes nothing and never makes a duplicate. Runs in `preprovision`, before any Azure resource exists.
# Output (azd environment): PORTAL_CLIENT_ID, PORTAL_APP_OBJECT_ID, PORTAL_SP_OBJECT_ID.
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$envName = $env:AZURE_ENV_NAME
$displayName = "Zava Scrum Portal ($envName)"
$tag = "zava-scrum-agent:$envName"
# Stable ids so a re-run updates the same scope instead of adding another.
$scopeId = '7d6f1b6e-3b0a-4c61-9d0c-5a4b1f0c2e11'
$azureCliAppId = '04b07795-8ddb-461a-bbee-02f9e1bf7b46'
$graphAppId = '00000003-0000-0000-c000-000000000000'
$graphUserRead = 'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
# Azure DevOps (ADR 0020): the portal app asks for its delegated user_impersonation scope, so an approval is written as the approver.
$adoAppId = '499b84ac-1321-427f-aa17-267ca6975798'
# The Teams desktop/mobile and web clients, pre-authorised so Teams single sign-on for card actions needs no consent prompt.
# Checked against Learn (bots/how-to/authentication/bot-sso-register-aad): Teams desktop and mobile 1fec8e78-bce4-4aaf-ab1b-5451cc387264, Teams web 5e3ce6c0-2b1f-4285-8d4b-75ee78787346; standalone bot URI api://botid-<app id>.
$teamsClients = @('1fec8e78-bce4-4aaf-ab1b-5451cc387264', '5e3ce6c0-2b1f-4285-8d4b-75ee78787346')

$roles = [System.Collections.Generic.List[object]]::new()
foreach ($r in @(Get-Content "$PSScriptRoot/../entra/portal-app-roles.json" -Raw | ConvertFrom-Json)) {
  $roles.Add(@{ id = $r.id; value = $r.value; displayName = $r.displayName; description = $r.description; allowedMemberTypes = @('User'); isEnabled = $true })
}

$found = (Invoke-Graph GET "/applications?`$filter=tags/any(t:t eq '$tag')&`$select=id,appId,displayName").value
if ($found.Count -gt 1) { throw "More than one application carries the tag '$tag'. Delete the extras in Entra and run again." }

if ($found.Count -eq 0) {
  $app = Invoke-Graph POST '/applications' @{ displayName = $displayName; signInAudience = 'AzureADMyOrg'; tags = @($tag) }
  Write-Check PASS 'portal app' "created $displayName ($($app.appId))"
}
else {
  $app = $found[0]
  Write-Check PASS 'portal app' "found $($app.displayName) ($($app.appId))"
}

# Azure DevOps lives in the tenant as a first-party service principal that may not exist yet in a new tenant; the scope id is looked up by name.
$adoSp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$adoAppId'&`$select=id,oauth2PermissionScopes").value | Select-Object -First 1
if (-not $adoSp) {
  Invoke-Graph POST '/servicePrincipals' @{ appId = $adoAppId } | Out-Null
  $adoSp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$adoAppId'&`$select=id,oauth2PermissionScopes").value | Select-Object -First 1
}
$adoScope = $adoSp.oauth2PermissionScopes | Where-Object { $_.value -eq 'user_impersonation' } | Select-Object -First 1
if (-not $adoScope) { throw 'Azure DevOps exposes no user_impersonation scope in this tenant.' }

# Scope first, then the pre-authorised client (Graph rejects a pre-authorisation for a scope that does not exist yet).
$existingAccess = (Invoke-Graph GET "/applications/$($app.id)?`$select=requiredResourceAccess").requiredResourceAccess
$requiredAccess = @(
  @{ resourceAppId = $graphAppId; resourceAccess = @(@{ id = $graphUserRead; type = 'Scope' }) }
  @{ resourceAppId = $adoAppId; resourceAccess = @(@{ id = $adoScope.id; type = 'Scope' }) })
Invoke-Graph PATCH "/applications/$($app.id)" @{
  displayName    = $displayName
  # The second URI is the form Teams single sign-on for bots expects (api://botid-<app id>); the portal keeps api://<app id>.
  identifierUris = @("api://$($app.appId)", "api://botid-$($app.appId)")
  web            = @{ implicitGrantSettings = @{ enableIdTokenIssuance = $true } }
  appRoles       = @($roles)
  requiredResourceAccess = @(Merge-RequiredResourceAccess $existingAccess $requiredAccess)
  api            = @{
    requestedAccessTokenVersion = 2
    oauth2PermissionScopes      = @(@{
        id = $scopeId; value = 'access_as_user'; type = 'User'; isEnabled = $true
        adminConsentDisplayName = 'Use the Zava Scrum Agent portal API'
        adminConsentDescription = 'Lets the caller use the portal API as the signed-in user.'
        userConsentDisplayName  = 'Use the Zava Scrum Agent portal API'
        userConsentDescription  = 'Lets this app use the portal API as you.'
      })
  }
} | Out-Null
Invoke-Graph PATCH "/applications/$($app.id)" @{
  api = @{ preAuthorizedApplications = @(@($azureCliAppId) + $teamsClients | ForEach-Object { @{ appId = $_; delegatedPermissionIds = @($scopeId) } }) }
} | Out-Null
Write-Check PASS 'portal app API' "scope api://$($app.appId)/access_as_user, Azure CLI and Teams clients pre-authorised"

$sp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$($app.appId)'&`$select=id").value | Select-Object -First 1
if (-not $sp) { $sp = Invoke-Graph POST '/servicePrincipals' @{ appId = $app.appId; tags = @($tag) } }
Write-Check PASS 'portal service principal' $sp.id

# Admin consent for User.Read (the sign-in permission), so nobody sees a consent prompt: this is the grant a tenant administrator would
# otherwise click in the portal.
$graphSp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$graphAppId'&`$select=id").value[0]
$grants = (Invoke-Graph GET "/oauth2PermissionGrants?`$filter=clientId eq '$($sp.id)' and resourceId eq '$($graphSp.id)'").value
if (-not $grants) {
  Invoke-Graph POST '/oauth2PermissionGrants' @{ clientId = $sp.id; consentType = 'AllPrincipals'; resourceId = $graphSp.id; scope = 'User.Read openid profile' } | Out-Null
  Write-Check PASS 'portal admin consent' 'User.Read granted for the organisation'
}
else { Write-Check PASS 'portal admin consent' 'already granted' }

# Azure DevOps delegated consent: the grant a tenant administrator would click. It lets the portal app act on a signed-in person's behalf in
# Azure DevOps and nothing else; the person still needs access to the project (docs/DEPLOY.md).
$adoGrant = (Invoke-Graph GET "/oauth2PermissionGrants?`$filter=clientId eq '$($sp.id)' and resourceId eq '$($adoSp.id)'").value
if (-not $adoGrant) {
  Invoke-Graph POST '/oauth2PermissionGrants' @{ clientId = $sp.id; consentType = 'AllPrincipals'; resourceId = $adoSp.id; scope = 'user_impersonation' } | Out-Null
  Write-Check PASS 'portal Azure DevOps consent' 'user_impersonation granted for the organisation'
}
else { Write-Check PASS 'portal Azure DevOps consent' 'already granted' }

# The scope's own consent: the SPA signs users in to the portal app and asks for api://<app>/access_as_user. Granting it for the
# organisation means nobody sees a consent prompt (the grant a tenant administrator would otherwise click).
$own = (Invoke-Graph GET "/oauth2PermissionGrants?`$filter=clientId eq '$($sp.id)' and resourceId eq '$($sp.id)'").value
if (-not $own) {
  Invoke-Graph POST '/oauth2PermissionGrants' @{ clientId = $sp.id; consentType = 'AllPrincipals'; resourceId = $sp.id; scope = 'access_as_user' } | Out-Null
  Write-Check PASS 'portal API consent' 'access_as_user granted for the organisation'
}
else { Write-Check PASS 'portal API consent' 'already granted' }

# The administrator gets every app role (Admin, Approver, Reader) so one person can run the demo end to end. Others are assigned
# in Entra (Enterprise applications > Zava Scrum Portal > Users and groups); docs/DEPLOY.md lists it as the way to add people.
$adminUser = Invoke-Graph GET "/users/$([uri]::EscapeDataString($env:ADMIN_UPN))?`$select=id,displayName"
$assigned = (Invoke-Graph GET "/servicePrincipals/$($sp.id)/appRoleAssignedTo").value | Where-Object { $_.principalId -eq $adminUser.id }
foreach ($r in @(Get-Content "$PSScriptRoot/../entra/portal-app-roles.json" -Raw | ConvertFrom-Json)) {
  if ($assigned | Where-Object { $_.appRoleId -eq $r.id }) { continue }
  Invoke-Graph POST "/servicePrincipals/$($sp.id)/appRoleAssignedTo" @{ principalId = $adminUser.id; resourceId = $sp.id; appRoleId = $r.id } | Out-Null
}
Write-Check PASS 'portal app roles' "$($adminUser.displayName) holds Zava.Scrum.Admin, Approver and Reader"
Set-AzdValue ADMIN_OBJECT_ID $adminUser.id

Set-AzdValue PORTAL_CLIENT_ID $app.appId
Set-AzdValue PORTAL_APP_OBJECT_ID $app.id
Set-AzdValue PORTAL_SP_OBJECT_ID $sp.id
