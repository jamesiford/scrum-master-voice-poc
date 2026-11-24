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

# Scope first, then the pre-authorised client (Graph rejects a pre-authorisation for a scope that does not exist yet).
Invoke-Graph PATCH "/applications/$($app.id)" @{
  displayName    = $displayName
  identifierUris = @("api://$($app.appId)")
  web            = @{ implicitGrantSettings = @{ enableIdTokenIssuance = $true } }
  appRoles       = @($roles)
  requiredResourceAccess = @(@{ resourceAppId = $graphAppId; resourceAccess = @(@{ id = $graphUserRead; type = 'Scope' }) })
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
  api = @{ preAuthorizedApplications = @(@{ appId = $azureCliAppId; delegatedPermissionIds = @($scopeId) }) }
} | Out-Null
Write-Check PASS 'portal app API' "scope api://$($app.appId)/access_as_user, Azure CLI pre-authorised"

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

Set-AzdValue PORTAL_CLIENT_ID $app.appId
Set-AzdValue PORTAL_APP_OBJECT_ID $app.id
Set-AzdValue PORTAL_SP_OBJECT_ID $sp.id
