@description('Region for the public Windows media host.')
param location string

@description('Environment-unique suffix.')
param token string

@description('Resource tags.')
param tags object

@description('Administrator object id for certificate import and artefact upload.')
param adminObjectId string

@description('VM size, initially four vCPU for audio and the later board encoder.')
param vmSize string = 'Standard_D4s_v5'

@description('Windows Server 2022 image SKU. Use 2022-datacenter-g2 for Gen2-only VM sizes.')
param imageSku string = '2022-datacenter'

@description('Protect the media vault from purge. Disable only for an explicitly disposable proof environment.')
param vaultPurgeProtection bool = true

@description('Optional certificate-covered owned DNS name; empty uses the Azure public IP DNS name.')
param serviceFqdn string = ''

@secure()
@description('Generated Windows administrator password, never exported or passed through azd.')
param adminPassword string = '${newGuid()}Aa1!'

resource publicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-media-${token}'
  location: location
  tags: tags
  sku: { name: 'Standard' }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
    dnsSettings: { domainNameLabel: 'zava-media-${token}' }
  }
}

resource nsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-media-${token}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'CallingHttpsAndMedia'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRanges: [ '80', '443', '8445' ]
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

// The NIC requires a VNet, but this is a public host, not a private networking or private endpoint design.
resource network 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-media-${token}'
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: [ '10.40.0.0/24' ] }
    subnets: [
      {
        name: 'media'
        properties: { addressPrefix: '10.40.0.0/24', networkSecurityGroup: { id: nsg.id } }
      }
    ]
  }
}

// No AVM media-bot pattern exists. Use the published VM resource module, with the repository's native small support resources.
module vm 'br/public:avm/res/compute/virtual-machine:0.23.0' = {
  name: 'media-vm'
  params: {
    name: 'vm-media-${token}'
    computerName: 'zava-media'
    location: location
    tags: union(tags, { 'azd-service-name': 'media-bot' })
    enableTelemetry: false
    vmSize: vmSize
    availabilityZone: -1
    osType: 'Windows'
    adminUsername: 'zavaMediaAdmin'
    adminPassword: adminPassword
    imageReference: { publisher: 'MicrosoftWindowsServer', offer: 'WindowsServer', sku: imageSku, version: 'latest' }
    osDisk: { createOption: 'FromImage', managedDisk: { storageAccountType: 'StandardSSD_LRS' } }
    managedIdentities: { systemAssigned: true }
    bootDiagnostics: true
    nicConfigurations: [
      {
        name: 'nic-media-${token}'
        ipConfigurations: [
          { name: 'media', subnetResourceId: '${network.id}/subnets/media', pipConfiguration: { publicIPAddressResourceId: publicIp.id } }
        ]
      }
    ]
  }
}

resource vault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: 'kv-media-${token}'
  location: location
  tags: tags
  properties: union({
    tenantId: tenant().tenantId
    sku: { name: 'standard', family: 'A' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }, vaultPurgeProtection ? { enablePurgeProtection: true } : {})
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: 'stmedia${token}'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    publicNetworkAccess: 'Enabled'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = {
  parent: storage
  name: 'default'
}

resource packages 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = {
  parent: blobs
  name: 'media-packages'
  properties: { publicAccess: 'None' }
}

resource vmSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, 'vm-media-${token}', 'media-secrets')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: vm.outputs.systemAssignedMIPrincipalId!
    principalType: 'ServicePrincipal'
  }

}

// HTTP-01 imports directly from the VM; private keys never enter run-command output.
resource vmCertificates 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, 'vm-media-${token}', 'media-certificates')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a4417e6f-fecd-4de8-b567-7b0420556985')
    principalId: vm.outputs.systemAssignedMIPrincipalId!
    principalType: 'ServicePrincipal'
  }
}

resource vmPackages 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(packages.id, 'vm-media-${token}', 'media-packages')
  scope: packages
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
    principalId: vm.outputs.systemAssignedMIPrincipalId!
    principalType: 'ServicePrincipal'
  }
}

resource certificateOperator 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (adminObjectId != '') {
  name: guid(vault.id, adminObjectId, 'media-certificate-operator')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a4417e6f-fecd-4de8-b567-7b0420556985')
    principalId: adminObjectId
    principalType: 'User'
  }
}

resource secretOperator 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (adminObjectId != '') {
  name: guid(vault.id, adminObjectId, 'media-secret-operator')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
    principalId: adminObjectId
    principalType: 'User'
  }
}

resource packageOperator 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (adminObjectId != '') {
  name: guid(packages.id, adminObjectId, 'media-package-operator')
  scope: packages
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: adminObjectId
    principalType: 'User'
  }
}

output vmName string = vm.outputs.name
output publicIpAddress string = publicIp.properties.ipAddress
output fqdn string = serviceFqdn != '' ? serviceFqdn : publicIp.properties.dnsSettings.fqdn
output vaultName string = vault.name
output storageName string = storage.name
