# Post-deploy wiring that Bicep cannot express: sign-in redirect URIs, the bot endpoint, and Azure DevOps access for the managed identity.
$ErrorActionPreference = 'Stop'
$api = $env:API_URI
$bridge = $env:BRIDGE_URI
$rg = $env:AZURE_RESOURCE_GROUP
$org = ($env:ADO_ORG_URL).TrimEnd('/').Split('/')[-1]
$project = 'Zava-Scrum-Demo'
# The Azure Bot resource is created outside this template; BOT_NAME names it (an existing environment sets it to its current bot).
$botName = if ($env:BOT_NAME) { $env:BOT_NAME } else { 'bot-zava-scrum-demo' }

Write-Host "Setting portal sign-in redirect URIs"
az ad app update --id $env:PORTAL_APP_OBJECT_ID --web-redirect-uris "$api/.auth/login/aad/callback" "$bridge/.auth/login/aad/callback" | Out-Null

Write-Host "Pointing the bot at $api/api/messages"
az bot update --name $botName --resource-group $rg --endpoint "$api/api/messages" --query properties.endpoint -o tsv

Write-Host "Granting the managed identity access to Azure DevOps ($org/$project)"
$token = az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv
$headers = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
$principalId = $env:MANAGED_IDENTITY_PRINCIPAL_ID

$body = @{ accessLevel = @{ accountLicenseType = 'express' }; servicePrincipal = @{ origin = 'aad'; originId = $principalId; subjectKind = 'servicePrincipal' } } | ConvertTo-Json -Depth 5
$entitlement = Invoke-RestMethod -Method Post -Uri "https://vsaex.dev.azure.com/$org/_apis/serviceprincipalentitlements?api-version=7.1-preview.1" -Headers $headers -Body $body

$projectId = (Invoke-RestMethod -Uri "https://dev.azure.com/$org/_apis/projects/$($project)?api-version=7.1" -Headers $headers).id
$scope = (Invoke-RestMethod -Uri "https://vssps.dev.azure.com/$org/_apis/graph/descriptors/$($projectId)?api-version=7.1-preview.1" -Headers $headers).value
$groups = Invoke-RestMethod -Uri "https://vssps.dev.azure.com/$org/_apis/graph/groups?scopeDescriptor=$scope&api-version=7.1-preview.1" -Headers $headers
$contributors = ($groups.value | Where-Object { $_.displayName -eq 'Contributors' }).descriptor
$member = $entitlement.servicePrincipalEntitlement.servicePrincipal.descriptor
Invoke-RestMethod -Method Put -Uri "https://vssps.dev.azure.com/$org/_apis/graph/memberships/$member/$($contributors)?api-version=7.1-preview.1" -Headers $headers | Out-Null

Write-Host ""
Write-Host "Portal:         $api"
Write-Host "Simulated Room: $bridge  (opened from the portal Sessions page)"
Write-Host "Teams app package: appPackage/zava-scrum-agent.zip (upload with Teams > Apps > Manage your apps > Upload a custom app)"