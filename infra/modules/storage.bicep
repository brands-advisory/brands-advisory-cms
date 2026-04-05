// ---------------------------------------------------------------------------
// Azure Storage Account — Blob storage for article images
//
// Creates a StorageV2 account with a public blob container 'article-images'.
// Images are served directly from the public blob endpoint.
// Write access is controlled via RBAC (see storage-rbac.bicep).
// ---------------------------------------------------------------------------

@description('Azure region for all resources.')
param location string

@description('Globally unique name for the Storage Account (3-24 lowercase alphanumeric).')
param storageAccountName string

// ---------------------------------------------------------------------------
// Storage Account
// ---------------------------------------------------------------------------
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
}

resource imageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: 'article-images'
  parent: blobService
  properties: {
    publicAccess: 'Blob'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
@description('Name of the deployed Storage Account.')
output storageAccountName string = storageAccount.name

@description('Primary blob service endpoint URI.')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
