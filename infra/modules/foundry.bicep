@description('Region.')
param location string

@minLength(3)
@description('Name prefix.')
param namePrefix string

@minLength(8)
@description('Unique suffix for this environment.')
param token string

@description('Resource tags.')
param tags object

@description('Principal id of the user-assigned identity the apps run as.')
param identityPrincipalId string

@description('Deployment SKU of the realtime and live models.')
param realtimeSku string

@description('Chat model capacity.')
param chatCapacity int

@description('Realtime model capacity.')
param realtimeCapacity int

@description('Decision model capacity.')
param decisionCapacity int

var roleCognitiveServicesUser = 'a97b65f3-24c7-4388-baec-2e87135dc908'
var roleAzureAIUser = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

// Model names, versions and formats are the ones measured in West US 3 (ADR 0014, ADR 0015). Deployment names are what the code and
// appsettings.json refer to: Foundry:ReasoningDeployment, VoiceLive:Model, UtteranceAnalysis:DecisionDeployment.
var deployments = [
  { name: 'scrum-gpt-5.4-mini', format: 'OpenAI', model: 'gpt-5.4-mini', version: '2026-03-17', sku: 'GlobalStandard', capacity: chatCapacity }
  { name: 'gpt-realtime-2.1', format: 'OpenAI', model: 'gpt-realtime-2.1', version: '2026-07-07', sku: realtimeSku, capacity: realtimeCapacity }
  { name: 'gpt-live-1', format: 'OpenAI', model: 'gpt-live-1', version: '2026-09-10', sku: realtimeSku, capacity: realtimeCapacity }
  { name: 'zava-decision', format: 'Microsoft', model: 'Microsoft-Decision-1.1', version: '1', sku: 'GlobalStandard', capacity: decisionCapacity }
]

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: 'fdy-${namePrefix}-${token}'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: { name: 'S0' }
  identity: { type: 'SystemAssigned' }
  properties: {
    allowProjectManagement: true
    customSubDomainName: 'fdy-${namePrefix}-${token}'
    // Entra only: no account keys exist, so nothing can leak.
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: account
  name: 'proj-${namePrefix}'
  location: location
  tags: tags
  identity: { type: 'SystemAssigned' }
  properties: {
    displayName: 'Zava Scrum Agent'
    description: 'Project for the Zava Scrum Agent.'
  }
}

// One at a time: concurrent deployments on one account conflict.
@batchSize(1)
resource modelDeployments 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = [for d in deployments: {
  parent: account
  name: d.name
  sku: { name: d.sku, capacity: d.capacity }
  properties: {
    model: { format: d.format, name: d.model, version: d.version }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
  dependsOn: [ project ]
}]

resource cognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: account
  name: guid(account.id, identityPrincipalId, roleCognitiveServicesUser)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleCognitiveServicesUser)
  }
}

resource aiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: account
  name: guid(account.id, identityPrincipalId, roleAzureAIUser)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAzureAIUser)
  }
}

output accountName string = account.name
output endpoint string = account.properties.endpoint
output projectName string = project.name
output projectPrincipalId string = project.identity.principalId
