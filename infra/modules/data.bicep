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

@description('Principal id of the Foundry project identity, which pulls hosted agent images.')
param foundryProjectPrincipalId string

var roleAcrPull = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var roleCommunicationOwner = '09976791-48a7-449e-bb21-39d1a415f350'
var cosmosDatabaseName = replace(namePrefix, '-', '')

// ---- Container registry ---------------------------------------------------------------------------------------------------
// Permission mode is explicit (ADR 0015 S3). In legacy mode a Foundry project identity needs AcrPull; the repository-scoped role
// "Container Registry Repository Reader" only works in ABAC mode and fails with ImageError: Container registry authentication failed.
resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: 'acr${replace(namePrefix, '-', '')}${token}'
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
    roleAssignmentMode: 'LegacyRegistryPermissions'
  }
}

resource acrPullApps 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: registry
  name: guid(registry.id, identityPrincipalId, roleAcrPull)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAcrPull)
  }
}

resource acrPullFoundry 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: registry
  name: guid(registry.id, foundryProjectPrincipalId, roleAcrPull)
  properties: {
    principalId: foundryProjectPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAcrPull)
  }
}

// ---- Cosmos DB: Entra only; one container, because ICollectionStore keeps every collection as an item in it ------------------
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: 'cosmos-${namePrefix}-${token}'
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    capabilities: [ { name: 'EnableServerless' } ]
    locations: [ { locationName: location, failoverPriority: 0 } ]
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    disableLocalAuth: true
    // Allowed by the SecurityControl=Ignore tag on the resource group. PROD: private endpoint.
    publicNetworkAccess: 'Enabled'
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmos
  name: cosmosDatabaseName
  properties: { resource: { id: cosmosDatabaseName } }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDb
  name: 'collections'
  properties: { resource: { id: 'collections', partitionKey: { paths: [ '/id' ], kind: 'Hash' } } }
}

// Cosmos DB Built-in Data Contributor.
resource cosmosRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmos
  name: guid(cosmos.id, identityPrincipalId, 'data-contributor')
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: identityPrincipalId
    scope: cosmos.id
  }
}

// ---- Communication Services: the bridge signs in with Entra, so there is no connection string ----------------------------------
resource acs 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: 'acs-${namePrefix}-${token}'
  location: 'global'
  tags: tags
  properties: { dataLocation: 'United States' }
}

resource acsOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: acs
  name: guid(acs.id, identityPrincipalId, roleCommunicationOwner)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleCommunicationOwner)
  }
}

output registryName string = registry.name
output registryLoginServer string = registry.properties.loginServer
output cosmosName string = cosmos.name
output cosmosEndpoint string = cosmos.properties.documentEndpoint
output cosmosDatabase string = cosmosDb.name
output acsEndpoint string = 'https://${acs.properties.hostName}'
