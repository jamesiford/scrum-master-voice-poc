<#
.SYNOPSIS
Prepares or applies the exact authorised demo seed/test-debris opt-in manifest; never resets or approves ADO changes.
.DESCRIPTION
Defaults to a read-only metadata preview. ConfigureEnvironment writes only the selected azd environment's local
declarative parameters before parent validation/provisioning. Apply calls the deployed Admin opt-in API after
configuration is deployed; the server atomically persists only matching IsDemo/DemoTenantId markers.
No team is selected by display name, no missing team is recreated, and no token is written.
.PARAMETER ManifestPath
Reviewed fixed tenant, sandbox, canonical seed ids and explicitly named test-debris fingerprints.
.PARAMETER ConfigureEnvironment
Sets only local DEMO_RESET_TENANT_ID/DEMO_RESET_TEAM_IDS azd parameters. Does not deploy or modify cloud resources.
.PARAMETER Apply
Applies only the opt-in markers through the validated Admin API. Never calls reset, reseed, approval or write APIs.
#>
[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\..\demo\film-reset-scope.json'),
    [switch]$ConfigureEnvironment,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
if ($ConfigureEnvironment -and $Apply) { throw 'Configure local parameters before deployment; Apply is a separate post-validation step.' }
$manifest = Get-Content -Raw $ManifestPath | ConvertFrom-Json
$values = azd env get-values --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $values.AZURE_ENV_NAME -ne $manifest.environment -or
    $values.AZURE_TENANT_ID -ne $manifest.tenantId -or $values.ADO_ORG_URL -ne $manifest.organisation) {
    throw 'The selected azd environment, tenant or sandbox differs from the reviewed manifest.'
}
$ids = @($manifest.canonicalSeedTeamIds) + @($manifest.testDebrisTeamIds)
if ($ids.Count -eq 0 -or ($ids | Sort-Object -Unique).Count -ne $ids.Count) { throw 'Manifest ids must be explicit and unique.' }
if ($ConfigureEnvironment) {
    azd env set DEMO_RESET_TENANT_ID $manifest.tenantId
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure the local reset tenant parameter.' }
    azd env set DEMO_RESET_TEAM_IDS ($ids -join ',')
    if ($LASTEXITCODE -ne 0) { throw 'Could not configure the local reset team parameter.' }
    [ordered]@{ environment = $manifest.environment; localParametersOnly = $true; tenantId = $manifest.tenantId; teamIds = $ids } | ConvertTo-Json
    return
}
$token = az account get-access-token --scope "api://$($values.PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $token) { throw 'A validated Admin portal token is required.' }
$headers = @{ Authorization = "Bearer $token" }
$base = $values.API_URI.TrimEnd('/')
try {
    $stored = Invoke-RestMethod -Uri "$base/api/teams?includeArchived=true" -Headers $headers
    foreach ($entry in $manifest.teams) {
        $team = @($stored | Where-Object id -eq $entry.teamId)
        if ($team.Count -gt 1) { throw "Duplicate registry id $($entry.teamId)." }
        if ($team.Count -eq 0) { continue } # The server permits absence only with completed-reset journal proof.
        foreach ($field in @('org', 'project', 'team', 'areaPath')) {
            if ($team[0].ado.$field -cne $entry.expectedAdo.$field) { throw "Reviewed ADO fingerprint changed for $($entry.teamId)." }
        }
        if ($team[0].demoTenantId -and $team[0].demoTenantId -ne $manifest.tenantId) { throw "Foreign demo tenant on $($entry.teamId)." }
    }
    if ($Apply) {
        Invoke-RestMethod -Method Post -Uri "$base/api/demo/reseed-scope" -Headers $headers -ContentType 'application/json' `
            -Body ($manifest | ConvertTo-Json -Depth 10) | Out-Null
        $stored = Invoke-RestMethod -Uri "$base/api/teams?includeArchived=true" -Headers $headers
        foreach ($team in $stored | Where-Object { $ids -contains $_.id }) {
            if (-not $team.isDemo -or $team.demoTenantId -ne $manifest.tenantId) { throw "Opt-in readback failed for $($team.id)." }
        }
    }
    [ordered]@{
        environment = $manifest.environment
        appliedMarkersOnly = [bool]$Apply
        teamIds = $ids
        missingIds = @($ids | Where-Object { $_ -notin $stored.id })
        untouchedIds = @($stored.id | Where-Object { $_ -notin $ids })
        markers = @($stored | Where-Object { $_.id -in $ids } | Select-Object id,isDemo,demoTenantId)
    } | ConvertTo-Json -Depth 4
} finally {
    Remove-Variable token,headers -ErrorAction SilentlyContinue
}
