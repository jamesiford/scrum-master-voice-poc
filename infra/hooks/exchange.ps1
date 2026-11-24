# Exchange Online and Graph features that send mail and create calendar meetings from the agent's mailbox.
#   pwsh infra/hooks/exchange.ps1 -Mode Apply    (azd postprovision) re-applies the settings if the grant was already made
#   pwsh infra/hooks/exchange.ps1 -Mode Grant    one interactive sign-in by an Exchange administrator, then applies
# Replaces the Entra Calendars.ReadWrite and Mail.Send application permissions and the application access policy with Exchange
# RBAC for Applications (Learn: exchange/permissions-exo/application-rbac), which replaces access policies. The managed identity gets
# the two application roles on ONE mailbox (the organiser), not on every mailbox in the tenant.
# HUMAN GATE (docs/DEPLOY.md): Connect-ExchangeOnline needs an interactive sign-in by an Exchange administrator.
param([ValidateSet('Apply', 'Grant')][string]$Mode = 'Apply')
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$organiser = $env:ADMIN_UPN
$clientId = $env:MANAGED_IDENTITY_CLIENT_ID
$principalId = $env:MANAGED_IDENTITY_PRINCIPAL_ID
$scopeName = "Zava-Scrum-$($env:AZURE_ENV_NAME)-mailbox"

function Set-AppSettings {
  $vars = @("Calendar__Organizer=$organiser", 'Email__Mode=Graph')
  if ($env:DEMO_MEMBER_EMAILS_JSON) {
    foreach ($p in ($env:DEMO_MEMBER_EMAILS_JSON | ConvertFrom-Json).PSObject.Properties) { $vars += "Demo__MemberEmails__$($p.Name)=$($p.Value)" }
  }
  az containerapp update -n $env:API_APP_NAME -g $env:AZURE_RESOURCE_GROUP --set-env-vars @vars -o none
  Write-Check PASS 'calendar and email' "Graph adapters selected for $organiser"
}

if ($Mode -eq 'Apply') {
  if ($env:EXCHANGE_GRANTED -eq 'true') { Set-AppSettings }
  else { Write-Check GATE 'Exchange grant' "Calendar and email run on the outbox until an Exchange administrator runs: pwsh infra/hooks/exchange.ps1 -Mode Grant" }
  return
}

if (-not (Get-Module -ListAvailable ExchangeOnlineManagement)) { Install-Module ExchangeOnlineManagement -Scope CurrentUser -Force }
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $organiser -Device -ShowBanner:$false

if (-not (Get-ServicePrincipal -Identity $clientId -ErrorAction SilentlyContinue)) {
  New-ServicePrincipal -AppId $clientId -ObjectId $principalId -DisplayName "zava-scrum-$($env:AZURE_ENV_NAME)" | Out-Null
}
if (-not (Get-ManagementScope -Identity $scopeName -ErrorAction SilentlyContinue)) {
  New-ManagementScope -Name $scopeName -RecipientRestrictionFilter "PrimarySmtpAddress -eq '$organiser'" | Out-Null
}
foreach ($role in 'Application Calendars.ReadWrite', 'Application Mail.Send') {
  $has = Get-ManagementRoleAssignment -RoleAssignee $clientId -Role $role -ErrorAction SilentlyContinue
  if (-not $has) { New-ManagementRoleAssignment -App $clientId -Role $role -CustomResourceScope $scopeName | Out-Null }
}
Write-Check PASS 'Exchange RBAC for Applications' "managed identity holds Calendars.ReadWrite and Mail.Send on $organiser only"
Disconnect-ExchangeOnline -Confirm:$false
Set-AzdValue EXCHANGE_GRANTED 'true'
Set-AppSettings
