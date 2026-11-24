# Runs in `postprovision`: Azure DevOps project, process, team, area, iterations, seed work items and sprint, then access for the API
# host's managed identity. Idempotent. ADR 0015 S4: an Entra agent identity cannot be added to Azure DevOps today, so the identity that
# reads and writes work items is the user-assigned managed identity the API runs as.
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$org = Get-AdoOrgName
$seedFile = Join-Path $PSScriptRoot '../../demo/seed-ado.json'
$project = (Get-Content $seedFile -Raw | ConvertFrom-Json).project

# The seeder signs in as the deployer (a Project Collection Administrator, checked by preflight).
if (-not $env:AZURE_TOKEN_CREDENTIALS) { $env:AZURE_TOKEN_CREDENTIALS = 'dev' }
dotnet run --project (Join-Path $PSScriptRoot '../../tools/SeedAdo') -- --tenant $env:AZURE_TENANT_ID --org $env:ADO_ORG_URL --file $seedFile
if ($LASTEXITCODE -ne 0) { throw 'The Azure DevOps seeder failed.' }
Write-Check PASS 'Azure DevOps project and seed' "$project in $org"

$h = Get-AdoHeaders
$principalId = $env:MANAGED_IDENTITY_PRINCIPAL_ID

# Entitlement. The POST is idempotent: a repeat returns the existing entitlement (the list endpoint does not support GET).
$body = @{ accessLevel = @{ accountLicenseType = 'express' }; servicePrincipal = @{ origin = 'aad'; originId = $principalId; subjectKind = 'servicePrincipal' } } | ConvertTo-Json -Depth 5
$result = Invoke-RestMethod -Method Post -Uri "https://vsaex.dev.azure.com/$org/_apis/serviceprincipalentitlements?api-version=7.1-preview.1" -Headers $h -Body $body
$mine = $result.servicePrincipalEntitlement
if (-not $result.isSuccess -or -not $mine) { throw "The managed identity $principalId could not be added to the organisation $org." }
Write-Check PASS 'Azure DevOps identity' "managed identity is in $org ($($mine.accessLevel.licensingSource) $($mine.accessLevel.accountLicenseType))"

$projectId = (Invoke-RestMethod "https://dev.azure.com/$org/_apis/projects/$($project)?api-version=7.1" -Headers $h).id
$scope = (Invoke-RestMethod "https://vssps.dev.azure.com/$org/_apis/graph/descriptors/$($projectId)?api-version=7.1-preview.1" -Headers $h).value
$groups = Invoke-RestMethod "https://vssps.dev.azure.com/$org/_apis/graph/groups?scopeDescriptor=$scope&api-version=7.1-preview.1" -Headers $h
$contributors = ($groups.value | Where-Object { $_.displayName -eq 'Contributors' }).descriptor
$member = $mine.servicePrincipal.descriptor
Invoke-RestMethod -Method Put -Uri "https://vssps.dev.azure.com/$org/_apis/graph/memberships/$member/$($contributors)?api-version=7.1-preview.1" -Headers $h | Out-Null
Write-Check PASS 'Azure DevOps access' "managed identity is a Contributor of $project"
