# Runs in `postdeploy` after agent-host.ps1: checks and records the governance of the hosted agent's own Entra Agent ID (ADR 0022). Idempotent.
#   - the identity is the one Foundry created for the agent (type agentIdentity, ServiceIdentity); no parallel identity is made;
#   - it holds no client secret or certificate (federated, platform-issued tokens only);
#   - it has a named sponsor (the deployer is added if there is none), which Entra needs for lifecycle and attestation;
#   - its sign-ins are visible with agentType (printed as evidence, not required: sign-in logs lag by minutes).
# Graph is called with an explicit resource (rule 12). The sponsor and Conditional Access policy steps that need a tenant administrator are
# human gates in docs/DEPLOY.md.
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$principal = $env:AGENT_HOST_PRINCIPAL_ID
if (-not $principal) { throw 'AGENT_HOST_PRINCIPAL_ID is not set; run agent-host.ps1 first.' }
$sp = Invoke-Graph GET "https://graph.microsoft.com/v1.0/servicePrincipals/$principal`?`$select=id,displayName,servicePrincipalType,passwordCredentials,keyCredentials"
if ($sp.'@odata.type' -ne '#microsoft.graph.agentIdentity') { Write-Check FAIL 'agent identity' "$($sp.displayName) is $($sp.'@odata.type'), not an agent identity"; return }
Write-Check PASS 'agent identity' "$($sp.displayName) (agentIdentity, $($sp.servicePrincipalType))"
$secrets = @($sp.passwordCredentials).Count + @($sp.keyCredentials).Count
if ($secrets -gt 0) { Write-Check FAIL 'agent identity credentials' "$secrets secret or certificate credentials found; there must be none" }
else { Write-Check PASS 'agent identity credentials' 'no client secret and no certificate' }

$beta = Invoke-Graph GET "https://graph.microsoft.com/beta/servicePrincipals/$principal/microsoft.graph.agentIdentity?`$select=id,agentIdentityBlueprintId"
Write-Check PASS 'agent blueprint' $beta.agentIdentityBlueprintId

$sponsors = (Invoke-Graph GET "https://graph.microsoft.com/beta/servicePrincipals/$principal/sponsors?`$select=id,displayName,userPrincipalName").value
if (-not $sponsors) {
  try {
    $admin = Invoke-Graph GET "https://graph.microsoft.com/v1.0/users/$([uri]::EscapeDataString($env:ADMIN_UPN))?`$select=id"
    Invoke-Graph POST "https://graph.microsoft.com/beta/servicePrincipals/$principal/sponsors/`$ref" @{ '@odata.id' = "https://graph.microsoft.com/beta/users/$($admin.id)" } | Out-Null
    $sponsors = (Invoke-Graph GET "https://graph.microsoft.com/beta/servicePrincipals/$principal/sponsors?`$select=id,displayName,userPrincipalName").value
  }
  catch { Write-Check GATE 'agent sponsor' "Could not add a sponsor ($($_.Exception.Message.Split("`n")[0])). A tenant administrator adds one in Entra admin centre > Agent ID > the agent > Sponsors." ; return }
}
Write-Check PASS 'agent sponsor' (($sponsors | ForEach-Object { $_.userPrincipalName }) -join ', ')

try {
  $f = "signInEventTypes/any(t: t eq 'servicePrincipal') and servicePrincipalId eq '$principal'"
  $rows = (Invoke-Graph GET "https://graph.microsoft.com/beta/auditLogs/signIns?`$filter=$([uri]::EscapeDataString($f))&`$top=5").value
  if ($rows) { Write-Check PASS 'agent sign-ins' "$(@($rows).Count) recent, agentType $($rows[0].agent.agentType), parent app $($rows[0].agent.parentAppId)" }
  else { Write-Check SKIP 'agent sign-ins' 'none yet; the first voice or warm-up call creates one (logs lag by a few minutes)' }
}
catch { Write-Check SKIP 'agent sign-ins' 'the sign-in log could not be read (needs AuditLog.Read.All)' }
