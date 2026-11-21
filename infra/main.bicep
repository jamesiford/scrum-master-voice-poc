// M7: deploys the PoC into the existing resource group, reusing the Foundry resource, Log Analytics and Application Insights.
// PROD: add Cosmos (Store__Mode=Cosmos), Azure Communication Services, private networking and a federated credential for the bot.
targetScope = 'resourceGroup'

@description('azd environment name.')
param environmentName string

@description('Region for new resources.')
param location string = resourceGroup().location

@description('Existing Foundry (AI Services) resource to reuse.')
param foundryName string = 'fdy-mcm-scrum-demo'

@description('Existing Log Analytics workspace to reuse.')
param logAnalyticsName string = 'log-fdy-mcm-scrum-demo-dybvsjflknh74'

@description('Existing Application Insights component to reuse.')
param appInsightsName string = 'appi-fdy-mcm-scrum-demo-9280'

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
  name: 'acs-mcm-scrum-demo'
  location: 'global'
  tags: tags
  properties: { dataLocation: 'United States' }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-mcm-scrum-${token}'
  location: location
  tags: tags
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acrmcmscrum${token}'
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

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-mcm-scrum-${token}'
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
      secrets: concat(commonSecrets, [ { name: 'bot-secret', value: botClientSecret }, { name: 'bridge-key', value: bridgeApiKey } ])
    }
    template: {
      containers: [
        {
          name: 'api'
          image: placeholderImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Azure__TenantId', value: tenantId }
            { name: 'Bot__AppId', value: botAppId }
            { name: 'Bot__TenantId', value: tenantId }
            { name: 'Bot__TunnelHostSuffix', value: '' }
            { name: 'Connections__ServiceConnection__Settings__ClientId', value: botAppId }
            { name: 'Connections__ServiceConnection__Settings__ClientSecret', secretRef: 'bot-secret' }
            { name: 'Bridge__RoomBaseUrl', value: 'https://${bridgeFqdn}' }
            { name: 'Bridge__ApiPublicUrl', value: 'https://${apiFqdn}' }
            { name: 'Bridge__ApiKey', secretRef: 'bridge-key' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
          ]
        }
      ]
      // The JSON store is a single file, so exactly one replica. PROD: Cosmos and scale out.
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [ acrPull ]
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