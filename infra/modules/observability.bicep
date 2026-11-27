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

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${namePrefix}-${token}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${namePrefix}-${token}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    // The application sends telemetry with the connection string, not an instrumentation key; Entra-only ingestion stays a PROD item.
    DisableLocalAuth: false
  }
}

module workbook 'workbook.bicep' = {
  name: 'observability-workbook'
  params: {
    location: location
    appInsightsName: insights.name
    tags: tags
  }
  dependsOn: [ insights ]
}

output logAnalyticsName string = logs.name
output appInsightsName string = insights.name
output appInsightsConnectionString string = insights.properties.ConnectionString
output workbookId string = workbook.outputs.workbookId
