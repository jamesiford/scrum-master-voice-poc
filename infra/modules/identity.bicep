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

// One user-assigned identity for both apps and the Azure Bot. It replaces three secrets: the bot client secret (the Azure Bot is of
// type UserAssignedMSI and the Agents SDK connection uses UserManagedIdentity), the calendar client secret (Graph through the identity)
// and the ACS connection string (Entra role on the Communication Services resource).
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${namePrefix}-${token}'
  location: location
  tags: tags
}

output id string = identity.id
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
