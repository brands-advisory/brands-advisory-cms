// ---------------------------------------------------------------------------
// Storage RBAC — Storage Blob Data Contributor role assignment
//
// Grants the Web App Managed Identity read/write/delete access to blobs
// in the Storage Account. Used to upload article images via the API.
//
// Built-in role: Storage Blob Data Contributor
// Role Definition ID: ba92f5b4-2d11-453d-a403-e96b0029c9fe
// ---------------------------------------------------------------------------

@description('Name of the existing Storage Account.')
param storageAccountName string

@description('Principal ID (object ID) to assign the role to. Typically the Web App Managed Identity.')
param principalId string

@description('Type of the principal. Defaults to ServicePrincipal for Managed Identities.')
param principalType string = 'ServicePrincipal'

// ---------------------------------------------------------------------------
// Reference the Storage Account
// ---------------------------------------------------------------------------
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

// ---------------------------------------------------------------------------
// Role Assignment — Storage Blob Data Contributor
// ---------------------------------------------------------------------------
resource storageBlobDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  // Use a deterministic GUID based on storage account + principal to make deployment idempotent
  name: guid(storageAccount.id, principalId, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  scope: storageAccount
  properties: {
    // Storage Blob Data Contributor (built-in)
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    )
    principalId: principalId
    principalType: principalType
  }
}
