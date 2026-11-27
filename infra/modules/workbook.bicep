@description('Existing environment region.')
param location string

@description('Existing Application Insights component name.')
param appInsightsName string

@description('Existing environment tags.')
param tags object

resource insights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// A shared workbook stores queries and metadata, never employee content or captured request bodies.
resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid(insights.id, 'zava-observability-evaluation')
  location: location
  kind: 'shared'
  tags: tags
  properties: {
    displayName: 'Zava content-free observability'
    category: 'workbook'
    sourceId: insights.id
    version: 'Notebook/1.0'
    serializedData: replace(loadTextContent('../workbooks/observability.json'), '{{insightsResourceId}}', insights.id)
  }
}

output workbookId string = workbook.id
