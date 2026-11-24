// M7: deploys the PoC into the existing resource group, reusing the Foundry resource, Log Analytics and Application Insights.
// PROD: private networking, no key-based ACS connection string (use Entra), and a federated credential for the bot.
targetScope = 'resourceGroup'

@description('azd environment name.')
param environmentName string

@description('Region for new resources.')
param location string = resourceGroup().location

@description('Prefix for the resource names this template creates (acs, identity, registry, Cosmos DB, Container Apps environment). An existing environment keeps its names by setting NAME_PREFIX to the prefix it was deployed with (see docs/rebrand/AZURE.md).')
param namePrefix string = 'zava-scrum'

@description('Cosmos DB database name. Defaults to the name prefix without dashes.')
param cosmosDatabaseName string = replace(namePrefix, '-', '')

@description('Existing Foundry (AI Services) resource to reuse.')
param foundryName string

@description('Foundry project name inside the Foundry resource (used for the project endpoint).')
param foundryProjectName string

@description('Azure DevOps organisation URL, for example https://dev.azure.com/your-organisation.')
param adoOrgUrl string

@description('Existing Log Analytics workspace to reuse.')
param logAnalyticsName string

@description('Existing Application Insights component to reuse.')
param appInsightsName string

@description('Existing Azure Bot resource whose endpoint is set after deploy.')
param botAppId string

@secure()
param botClientSecret string

@description('Entra app registration that signs users in to the portal and Simulated Room.')
param portalClientId string

@secure()
param portalClientSecret string

@secure()
@description('Shared secret for API-to-bridge control calls.')
param bridgeApiKey string

@description('Mailbox that owns the sprint calendar meetings (Calendar:Organizer). Empty disables Graph calendar creation.')
param calendarOrganizer string = ''

@description('Entra app with Calendars.ReadWrite (application) scoped to the organizer mailbox.')
param calendarClientId string = ''

@secure()
param calendarClientSecret string = ''

@description('Roster member emails as a JSON object keyed by member id (Demo:MemberEmails), e.g. {"daniel":"daniel@contoso.com"}.')
param memberEmailsJson string = '{}'

var memberEmails = json(memberEmailsJson)
var memberEmailEnv = [for member in items(memberEmails): { name: 'Demo__MemberEmails__${member.key}', value: member.value }]

var tenantId = subscription().tenantId
var token = toLower(uniqueString(resourceGroup().id, environmentName))
var tags = { 'azd-env-name': environmentName }
var placeholderImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: foundryName
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource insights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource acs 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: 'acs-${namePrefix}-demo'
  location: 'global'
  tags: tags
  properties: { dataLocation: 'United States' }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${namePrefix}-${token}'
  location: location
  tags: tags
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acr${replace(namePrefix, '-', '')}${token}'
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

var roleAcrPull = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var roleCognitiveServicesUser = 'a97b65f3-24c7-4388-baec-2e87135dc908'
var roleAzureAIUser = '53ca6127-db72-4b80-b1b0-d745d6d5456d'

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: registry
  name: guid(registry.id, identity.id, roleAcrPull)
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAcrPull)
  }
}

resource cognitiveUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: foundry
  name: guid(foundry.id, identity.id, roleCognitiveServicesUser)
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleCognitiveServicesUser)
  }
}

resource aiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: foundry
  name: guid(foundry.id, identity.id, roleAzureAIUser)
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAzureAIUser)
  }
}

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
    // PROD: disable key auth entirely and use private endpoints.
    disableLocalAuth: true
    // Allowed by the SecurityControl=Ignore tag on the resource group; PROD: private endpoint instead.
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
  name: guid(cosmos.id, identity.id, 'data-contributor')
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: identity.properties.principalId
    scope: cosmos.id
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${namePrefix}-${token}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

var apiName = 'ca-api-${token}'
var bridgeName = 'ca-bridge-${token}'
var apiFqdn = '${apiName}.${environment.properties.defaultDomain}'
var bridgeFqdn = '${bridgeName}.${environment.properties.defaultDomain}'

var commonSecrets = [
  { name: 'portal-secret', value: portalClientSecret }
]

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'auto' }
      registries: [ { server: registry.properties.loginServer, identity: identity.id } ]
      secrets: concat(commonSecrets, [ { name: 'bot-secret', value: botClientSecret }, { name: 'bridge-key', value: bridgeApiKey }, { name: 'calendar-secret', value: empty(calendarClientSecret) ? 'unset' : calendarClientSecret } ])
    }
    template: {
      containers: [
        {
          name: 'api'
          image: placeholderImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat([
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Azure__TenantId', value: tenantId }
            { name: 'Bot__AppId', value: botAppId }
            { name: 'Bot__TenantId', value: tenantId }
            { name: 'Bot__TunnelHostSuffix', value: '' }
            { name: 'Foundry__ProjectEndpoint', value: 'https://${foundryName}.services.ai.azure.com/api/projects/${foundryProjectName}' }
            { name: 'Ado__Org', value: adoOrgUrl }
            { name: 'Connections__ServiceConnection__Settings__TenantId', value: tenantId }
            { name: 'Connections__ServiceConnection__Settings__AuthorityEndpoint', value: 'https://login.microsoftonline.com/${tenantId}' }
            { name: 'Connections__ServiceConnection__Settings__ClientId', value: botAppId }
            { name: 'Connections__ServiceConnection__Settings__ClientSecret', secretRef: 'bot-secret' }
            { name: 'Store__Mode', value: 'Cosmos' }
            { name: 'Cosmos__Endpoint', value: cosmos.properties.documentEndpoint }
            { name: 'Cosmos__Database', value: cosmosDatabaseName }
            { name: 'Bridge__RoomBaseUrl', value: 'https://${bridgeFqdn}' }
            { name: 'Bridge__ApiPublicUrl', value: 'https://${apiFqdn}' }
            { name: 'Bridge__ApiKey', secretRef: 'bridge-key' }
            { name: 'Calendar__Organizer', value: calendarOrganizer }
            { name: 'Calendar__ClientId', value: calendarClientId }
            { name: 'Calendar__ClientSecret', secretRef: 'calendar-secret' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
          ], memberEmailEnv)
        }
      ]
      // The JSON store is a single file, so exactly one replica. PROD: Cosmos and scale out.
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [ acrPull, cosmosRole ]
}

resource bridge 'Microsoft.App/containerApps@2024-03-01' = {
  name: bridgeName
  location: location
  tags: union(tags, { 'azd-service-name': 'bridge' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'auto' }
      registries: [ { server: registry.properties.loginServer, identity: identity.id } ]
      secrets: concat(commonSecrets, [ { name: 'bridge-key', value: bridgeApiKey }, { name: 'acs-connection', value: acs.listKeys().primaryConnectionString } ])
    }
    template: {
      containers: [
        {
          name: 'bridge'
          image: placeholderImage
          resources: { cpu: json('2.0'), memory: '4Gi' }
          env: [
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'AZURE_TENANT_ID', value: tenantId }
            { name: 'API_URL', value: 'https://${apiFqdn}' }
            { name: 'SPEECH_ENDPOINT', value: foundry.properties.endpoint }
            { name: 'PORT', value: '8080' }
            { name: 'Bridge__ApiKey', secretRef: 'bridge-key' }
            { name: 'ACS_CONNECTION_STRING', secretRef: 'acs-connection' }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [ acrPull ]
}

// Sign-in is required everywhere except the endpoints that carry their own credential: the bot validates the Bot Service
// token (F7 stays enforced in the app), and the bridge socket is authorised by a short-lived bridge token.
resource apiAuth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: api
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: {
      unauthenticatedClientAction: 'RedirectToLoginPage'
      redirectToProvider: 'azureactivedirectory'
      excludedPaths: [ '/api/messages', '/healthz', '/ws/bridge/*' ]
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          openIdIssuer: 'https://login.microsoftonline.com/${tenantId}/v2.0'
          clientId: portalClientId
          clientSecretSettingName: 'portal-secret'
        }
      }
    }
  }
}

resource bridgeAuth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: bridge
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: {
      unauthenticatedClientAction: 'RedirectToLoginPage'
      redirectToProvider: 'azureactivedirectory'
      excludedPaths: [ '/teams/*', '/healthz' ]
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          openIdIssuer: 'https://login.microsoftonline.com/${tenantId}/v2.0'
          clientId: portalClientId
          clientSecretSettingName: 'portal-secret'
        }
      }
    }
  }
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.properties.loginServer
output API_URI string = 'https://${apiFqdn}'
output BRIDGE_URI string = 'https://${bridgeFqdn}'
output MANAGED_IDENTITY_CLIENT_ID string = identity.properties.clientId
output MANAGED_IDENTITY_PRINCIPAL_ID string = identity.properties.principalId