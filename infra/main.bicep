// Zava Scrum Agent: greenfield deployment (ADR 0015a). `azd up` creates everything from an empty subscription.
// Declarative here: resource group, observability, Foundry (account, project, model deployments), identity, data (Cosmos, ACR, ACS),
// compute (Container Apps, Azure Bot) and every role assignment. Entra objects, Easy Auth, Azure DevOps, the Teams package and the
// Exchange grant cannot be expressed in Bicep and are scripted in infra/hooks (see docs/DEPLOY.md).
targetScope = 'subscription'

@minLength(2)
@maxLength(20)
@description('azd environment name. Becomes part of every resource name.')
param environmentName string

@description('Region for every resource. Must support the realtime models, Voice Live and hosted agents (infra/hooks/preflight.ps1 checks).')
param location string

@description('Microsoft Entra tenant of the subscription.')
param tenantId string = tenant().tenantId

@description('Azure DevOps organisation URL, for example https://dev.azure.com/your-organisation.')
param adoOrgUrl string

@description('UPN of the demo tenant administrator. Owns the sprint calendar mailbox and is the first portal user.')
param adminUpn string

@description('Client id of the portal Entra app registration, created by infra/hooks/entra-portal-app.ps1 in preprovision. Not a secret. Empty only while that hook has not run; entra-post.ps1 re-applies it.')
param portalClientId string = ''

@description('Entra object id of the administrator, resolved by infra/hooks/entra-portal-app.ps1. Not a secret.')
param adminObjectId string = ''

@allowed([ 'Direct', 'Hosted' ])
@description('How the voice gateway reaches Voice Live: directly, or through the Foundry hosted agent relay (ADR 0021). Both are real adapters of the same protocol.')
param voiceRoute string = 'Direct'

@description('The hosted agent that relays voice. Created by infra/hooks/agent-host.ps1 after deploy.')
param agentHostName string = 'zava-voice-relay'


@description('Deployment SKU of the realtime and live models: GlobalStandard, or DataZoneStandard to keep processing inside the data zone and use data zone quota.')
@allowed([ 'GlobalStandard', 'DataZoneStandard' ])
param realtimeSku string = 'DataZoneStandard'

@description('Capacity (thousands of tokens per minute) of the chat model deployment.')
param chatCapacity int = 100

@description('Capacity of each realtime model deployment in realtimeModels.')
param realtimeCapacity int = 10

@description('Realtime models deployed for Voice Live. Keep it in step with requiredModels in infra/hooks/regions.json (the preflight quota check). VoiceLive__Model must name one of these deployments. gpt-live-1 and gpt-realtime-2.1-mini are not listed: the first is a separate API surface that nothing here uses, the second was measured and rejected (ADR 0019). To test another model, add it here and to requiredModels in regions.json.')
param realtimeModels array = [
  { name: 'gpt-realtime-2.1', model: 'gpt-realtime-2.1', version: '2026-07-07' }
]

@description('Capacity of the Microsoft-Decision-1.1 deployment. A live standup needs more than 10 (rate limits are per minute); 100 was measured sufficient.')
param decisionCapacity int = 100

@description('Resources use the same prefix so a deployment is easy to find. Existing environments keep their mcm names until retired.')
var namePrefix = 'zava-scrum'

var token = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  // The tenant policy that forces public network access off on Cosmos DB honours this tag on the resource group (ADR 0006).
  SecurityControl: 'Ignore'
  owner: adminUpn
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${namePrefix}-${environmentName}'
  location: location
  tags: tags
}

module observability 'modules/observability.bicep' = {
  name: 'observability'
  scope: rg
  params: { location: location, namePrefix: namePrefix, token: token, tags: tags }
}

module identity 'modules/identity.bicep' = {
  name: 'identity'
  scope: rg
  params: { location: location, namePrefix: namePrefix, token: token, tags: tags }
}

module foundry 'modules/foundry.bicep' = {
  name: 'foundry'
  scope: rg
  params: {
    location: location
    namePrefix: namePrefix
    token: token
    tags: tags
    identityPrincipalId: identity.outputs.principalId
    realtimeSku: realtimeSku
    chatCapacity: chatCapacity
    realtimeCapacity: realtimeCapacity
    realtimeModels: realtimeModels
    decisionCapacity: decisionCapacity
  }
}

module data 'modules/data.bicep' = {
  name: 'data'
  scope: rg
  params: {
    location: location
    namePrefix: namePrefix
    token: token
    tags: tags
    identityPrincipalId: identity.outputs.principalId
    foundryProjectPrincipalId: foundry.outputs.projectPrincipalId
  }
}

module compute 'modules/compute.bicep' = {
  name: 'compute'
  scope: rg
  params: {
    location: location
    namePrefix: namePrefix
    token: token
    tags: tags
    tenantId: tenantId
    portalClientId: portalClientId
    adminObjectId: adminObjectId
    adminUpn: adminUpn
    adoOrgUrl: adoOrgUrl
    logAnalyticsName: observability.outputs.logAnalyticsName
    appInsightsConnectionString: observability.outputs.appInsightsConnectionString
    identityId: identity.outputs.id
    identityClientId: identity.outputs.clientId
    registryLoginServer: data.outputs.registryLoginServer
    cosmosEndpoint: data.outputs.cosmosEndpoint
    cosmosDatabase: data.outputs.cosmosDatabase
    acsEndpoint: data.outputs.acsEndpoint
    foundryName: foundry.outputs.accountName
    foundryEndpoint: foundry.outputs.endpoint
    foundryProjectName: foundry.outputs.projectName
    voiceRelayEndpoint: voiceRoute == 'Hosted' ? 'wss://${foundry.outputs.accountName}.services.ai.azure.com/api/projects/${foundry.outputs.projectName}/agents/${agentHostName}/endpoint/protocols/invocations_ws' : ''
  }
}

// azd reads these outputs into the environment; the hooks and the verify script use them.
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = data.outputs.registryLoginServer
output API_URI string = compute.outputs.apiUri
output BRIDGE_URI string = compute.outputs.bridgeUri
output API_APP_NAME string = compute.outputs.apiName
output BRIDGE_APP_NAME string = compute.outputs.bridgeName
output BOT_NAME string = compute.outputs.botName
output BOT_APP_ID string = identity.outputs.clientId
output MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output MANAGED_IDENTITY_PRINCIPAL_ID string = identity.outputs.principalId
output FOUNDRY_NAME string = foundry.outputs.accountName
output FOUNDRY_PROJECT_NAME string = foundry.outputs.projectName
output FOUNDRY_PROJECT_ENDPOINT string = 'https://${foundry.outputs.accountName}.services.ai.azure.com/api/projects/${foundry.outputs.projectName}'
output COSMOS_ACCOUNT_NAME string = data.outputs.cosmosName
output ACS_ENDPOINT string = data.outputs.acsEndpoint
