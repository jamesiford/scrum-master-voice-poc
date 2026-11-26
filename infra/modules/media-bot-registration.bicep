@description('The calling application created by the idempotent media-host hook.')
param appId string

@description('Deployment tenant.')
param tenantId string

@description('Environment-unique Azure Bot resource name.')
param botName string

@description('The public certificate-covered media host DNS name.')
param fqdn string

resource bot 'Microsoft.BotService/botServices@2022-09-15' = {
  name: botName
  location: 'global'
  kind: 'azurebot'
  sku: { name: 'F0' }
  properties: {
    displayName: 'Zava Scrum Media'
    msaAppId: appId
    msaAppType: 'SingleTenant'
    msaAppTenantId: tenantId
    endpoint: 'https://${fqdn}/api/messages'
  }
}

resource teams 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: bot
  name: 'MsTeamsChannel'
  location: 'global'
  properties: {
    channelName: 'MsTeamsChannel'
    properties: {
      isEnabled: true
      enableCalling: true
      callingWebhook: 'https://${fqdn}/callback'
    }
  }
}
