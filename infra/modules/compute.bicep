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

@description('Microsoft Entra tenant id.')
param tenantId string

param adoOrgUrl string

@description('Explicit reset tenant; empty disables the feature.')
param demoResetTenantId string = ''

@description('Comma-separated exact demo reset ids; no matching by names or failed connection status.')
param demoResetTeamIds string = ''

@allowed([ 'false', 'true' ])
@description('Demo opt-in for the unsaved configured default team. Does not upgrade legacy registry records.')
param demoDefaultTeamIsDemo string = 'false'

@description('UPN of the administrator. DEMO: the roster member "Admin" is this person, so the API can resolve their Entra object id (ADR 0017).')
param adminUpn string = ''

@description('Client id of the portal Entra app registration. The API validates portal bearer tokens against it.')
param portalClientId string = ''

@description('Entra object id of the administrator; Teams decisions from it are accepted as an administrator.')
param adminObjectId string = ''

@description('Log Analytics workspace the environment writes to.')
param logAnalyticsName string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Resource id of the user-assigned identity.')
param identityId string

@description('Client id of the user-assigned identity. It is also the bot id.')
param identityClientId string

@description('Container registry login server.')
param registryLoginServer string

@description('Cosmos DB endpoint.')
param cosmosEndpoint string

@description('Cosmos DB database name.')
param cosmosDatabase string

@description('Communication Services endpoint.')
param acsEndpoint string

@description('Foundry account name.')
param foundryName string

@description('Foundry endpoint (cognitiveservices domain).')
param foundryEndpoint string

@description('Foundry project name.')
param foundryProjectName string

@description('The invocations_ws endpoint of the hosted agent that relays to Voice Live (ADR 0021). Empty means Voice Live is called directly.')
param voiceRelayEndpoint string = ''

@description('Selectable real meeting adapter.')
param bridgeMode string = 'Acs'

@allowed([ 'Share', 'MediaShare' ])
@description('Selectable existing canvas or native VBSS board adapter.')
param boardMode string = 'Share'

@description('HTTPS control origin of the opt-in Windows media host.')
param graphMediaBaseUrl string = ''

@secure()
@description('Shared secret for API to bridge control calls. Generated at deployment and kept only as a Container Apps secret; nobody passes it in.')
param bridgeApiKey string = newGuid()

@description('Image both apps start with until azd deploy replaces it.')
param placeholderImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

var demoResetTeamEnvironment = [for (id, index) in (demoResetTeamIds == '' ? [] : split(demoResetTeamIds, ',')): {
  name: 'Demo__ResetTeamIds__${index}'
  value: trim(id)
}]

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
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

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identityId}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'auto' }
      registries: [ { server: registryLoginServer, identity: identityId } ]
      secrets: [ { name: 'bridge-key', value: bridgeApiKey } ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: placeholderImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          // Traffic reaches a new revision only when /healthz answers, so a request right after a deploy waits for a ready replica
          // instead of timing out in front of one that is still starting (seen in the first westus3 verify run).
          probes: [
            { type: 'Startup', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 3, timeoutSeconds: 3, failureThreshold: 40 }
            { type: 'Readiness', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 10, timeoutSeconds: 3, failureThreshold: 3 }
            { type: 'Liveness', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 30, timeoutSeconds: 5, failureThreshold: 3 }
          ]
          env: concat([
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'Azure__TenantId', value: tenantId }
            { name: 'Identity__TenantId', value: tenantId }
            { name: 'Identity__AdminObjectIds', value: adminObjectId }
            { name: 'Demo__MemberEmails__admin', value: adminUpn }
            { name: 'Portal__ClientId', value: portalClientId }
            { name: 'Bot__AppId', value: identityClientId }
            { name: 'Bot__TenantId', value: tenantId }
            { name: 'Bot__TunnelHostSuffix', value: '' }
            // The Azure Bot is of type UserAssignedMSI, so the Agents SDK connection authenticates as the identity: no secret.
            { name: 'Connections__ServiceConnection__Settings__AuthType', value: 'UserManagedIdentity' }
            { name: 'Connections__ServiceConnection__Settings__ClientId', value: identityClientId }
            { name: 'Connections__ServiceConnection__Settings__TenantId', value: tenantId }
            { name: 'Foundry__ProjectEndpoint', value: 'https://${foundryName}.services.ai.azure.com/api/projects/${foundryProjectName}' }
            { name: 'Ado__Org', value: adoOrgUrl }
            { name: 'Demo__ResetTenantId', value: demoResetTenantId == '' ? '00000000-0000-0000-0000-000000000000' : demoResetTenantId }
            { name: 'Team__IsDemo', value: demoDefaultTeamIsDemo }
            { name: 'Team__DemoTenantId', value: demoResetTenantId }
            { name: 'Store__Mode', value: 'Cosmos' }
            { name: 'Cosmos__SessionsContainer', value: 'Sessions' }
            { name: 'Cosmos__Endpoint', value: cosmosEndpoint }
            { name: 'Cosmos__Database', value: cosmosDatabase }
            { name: 'Bridge__RoomBaseUrl', value: 'https://${bridgeFqdn}' }
            { name: 'Bridge__ApiPublicUrl', value: 'https://${apiFqdn}' }
            { name: 'Bridge__ApiKey', secretRef: 'bridge-key' }
            { name: 'Bridge__Mode', value: bridgeMode }
            { name: 'Board__Mode', value: boardMode }
            { name: 'Bridge__GraphMediaBaseUrl', value: graphMediaBaseUrl }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            { name: 'Telemetry__CaptureContent', value: 'false' }
          ], voiceRelayEndpoint == '' ? [] : [ { name: 'VoiceLive__RelayEndpoint', value: voiceRelayEndpoint } ],
          demoResetTeamEnvironment)
        }
      ]
      // One replica: the voice gateway and the scheduler keep session state in the process. PROD: shared state and scale out.
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
}

resource bridge 'Microsoft.App/containerApps@2024-03-01' = {
  name: bridgeName
  location: location
  tags: union(tags, { 'azd-service-name': 'bridge' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identityId}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'auto' }
      registries: [ { server: registryLoginServer, identity: identityId } ]
      secrets: [ { name: 'bridge-key', value: bridgeApiKey } ]
    }
    template: {
      containers: [
        {
          name: 'bridge'
          image: placeholderImage
          resources: { cpu: json('2.0'), memory: '4Gi' }
          // Traffic reaches a new revision only when /healthz answers, so a request right after a deploy waits for a ready replica
          // instead of timing out in front of one that is still starting (seen in the first westus3 verify run).
          probes: [
            { type: 'Startup', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 3, timeoutSeconds: 3, failureThreshold: 40 }
            { type: 'Readiness', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 10, timeoutSeconds: 3, failureThreshold: 3 }
            { type: 'Liveness', httpGet: { path: '/healthz', port: 8080 }, periodSeconds: 30, timeoutSeconds: 5, failureThreshold: 3 }
          ]
          env: [
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'AZURE_TENANT_ID', value: tenantId }
            { name: 'API_URL', value: 'https://${apiFqdn}' }
            { name: 'SPEECH_ENDPOINT', value: foundryEndpoint }
            { name: 'PORT', value: '8080' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            { name: 'Telemetry__CaptureContent', value: 'false' }
            { name: 'Bridge__ApiKey', secretRef: 'bridge-key' }
            // Entra sign-in to Communication Services; the identity holds the Communication and Email Service Owner role.
            { name: 'Acs__Endpoint', value: acsEndpoint }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
}

// ---- Azure Bot: user-assigned managed identity, so no app registration and no secret ----------------------------------------
resource bot 'Microsoft.BotService/botServices@2022-09-15' = {
  name: 'bot-${namePrefix}-${token}'
  location: 'global'
  tags: tags
  kind: 'azurebot'
  sku: { name: 'F0' }
  properties: {
    displayName: 'Zava Scrum Agent'
    endpoint: 'https://${apiFqdn}/api/messages'
    msaAppId: identityClientId
    msaAppType: 'UserAssignedMSI'
    msaAppMSIResourceId: identityId
    msaAppTenantId: tenantId
  }
}

resource teamsChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: bot
  name: 'MsTeamsChannel'
  location: 'global'
  properties: {
    channelName: 'MsTeamsChannel'
    properties: { isEnabled: true }
  }
}

output apiName string = api.name
output bridgeName string = bridge.name
output apiUri string = 'https://${apiFqdn}'
output bridgeUri string = 'https://${bridgeFqdn}'
output botName string = bot.name
