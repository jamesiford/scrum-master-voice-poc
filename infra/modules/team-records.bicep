@description('Existing Entra-only Cosmos account.')
param cosmosAccountName string

@description('Existing application database.')
param databaseName string

@description('Application managed identity principal, scoped only to the new container.')
param identityPrincipalId string

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' existing = {
  parent: cosmos
  name: databaseName
}

resource records 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: database
  name: 'TeamRecords'
  properties: {
    resource: {
      id: 'TeamRecords'
      partitionKey: { paths: [ '/teamId' ], kind: 'Hash' }
      indexingPolicy: {
        automatic: true
        indexingMode: 'consistent'
        includedPaths: [ { path: '/*' } ]
        excludedPaths: [ { path: '/json/*' } ]
      }
    }
  }
}

resource contributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmos
  name: guid(cosmos.id, identityPrincipalId, 'data-contributor-team-records')
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: identityPrincipalId
    scope: '${cosmos.id}/dbs/${database.name}/colls/${records.name}'
  }
}
