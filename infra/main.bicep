// ---------------------------------------------------------------------------
// main.bicep — Brands Advisory CMS infrastructure entry point
//
// Deploys:
//   - Azure Cosmos DB account, database, and container
//   - Azure App Service Plan + Web App (.NET 10, Linux)
//   - Key Vault Certificate User role assignment for the Web App identity
//
// The Key Vault itself is NOT created here; it is assumed to already exist.
// Set existingKeyVault = true (default) to reference it for RBAC.
// ---------------------------------------------------------------------------

targetScope = 'resourceGroup'

// ---------------------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------------------

@description('Azure region for all new resources.')
param location string = 'germanywestcentral'

@description('Name of the App Service web app.')
param appName string

@description('Name of the App Service Plan.')
param planName string

@description('Globally unique name for the Cosmos DB account.')
param cosmosAccountName string

@description('Cosmos DB database name.')
param cosmosDatabaseId string

@description('Cosmos DB container name.')
param cosmosContainerName string

@description('Name of the existing Key Vault.')
param keyVaultName string

@description('If true, the existing Key Vault is referenced for the RBAC assignment.')
param existingKeyVault bool = true

@description('URI of the Azure Key Vault, e.g. https://<vault-name>.vault.azure.net/.')
param keyVaultUrl string

@description('Name of the certificate stored in Key Vault.')
param keyVaultCertificateName string

@description('Entra ID tenant ID.')
param tenantId string

@description('App Registration client (application) ID.')
param clientId string

@description('Syncfusion Community License key.')
param syncfusionLicenseKey string

@description('Globally unique name for the Storage Account (3-24 lowercase alphanumeric).')
param storageAccountName string

// ---------------------------------------------------------------------------
// Module: Cosmos DB
// ---------------------------------------------------------------------------
module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    accountName: cosmosAccountName
    databaseName: cosmosDatabaseId
    containerName: cosmosContainerName
  }
}

// ---------------------------------------------------------------------------
// Module: Storage Account (article images)
// ---------------------------------------------------------------------------
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: storageAccountName
  }
}

// ---------------------------------------------------------------------------
// Module: App Service + Web App
// ---------------------------------------------------------------------------
module appService 'modules/app-service.bicep' = {
  name: 'appService'
  params: {
    location: location
    appName: appName
    planName: planName
    keyVaultUrl: keyVaultUrl
    keyVaultCertificateName: keyVaultCertificateName
    cosmosEndpoint: cosmos.outputs.cosmosEndpoint
    cosmosDatabaseId: cosmosDatabaseId
    cosmosContainerName: cosmosContainerName
    tenantId: tenantId
    clientId: clientId
    syncfusionLicenseKey: syncfusionLicenseKey
    storageBlobEndpoint: storage.outputs.blobEndpoint
  }
}

// ---------------------------------------------------------------------------
// Module: Key Vault RBAC (only when Key Vault exists)
// ---------------------------------------------------------------------------
module keyVaultRbac 'modules/keyvault-rbac.bicep' = if (existingKeyVault) {
  name: 'keyVaultRbac'
  params: {
    keyVaultName: keyVaultName
    principalId: appService.outputs.principalId
  }
}

// ---------------------------------------------------------------------------
// Module: Cosmos DB RBAC (Built-in Data Contributor for Managed Identity)
// ---------------------------------------------------------------------------
module cosmosRbac 'modules/cosmos-rbac.bicep' = {
  name: 'cosmosRbac'
  params: {
    cosmosAccountName: cosmosAccountName
    principalId: appService.outputs.principalId
  }
}

// ---------------------------------------------------------------------------
// Module: Storage RBAC (Blob Data Contributor for Managed Identity)
// ---------------------------------------------------------------------------
module storageRbac 'modules/storage-rbac.bicep' = {
  name: 'storageRbac'
  params: {
    storageAccountName: storageAccountName
    principalId: appService.outputs.principalId
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('Name of the deployed Web App.')
output webAppName string = appService.outputs.webAppName

@description('Cosmos DB account endpoint URI.')
output cosmosEndpoint string = cosmos.outputs.cosmosEndpoint

@description('Next steps after deployment.')
output deploymentInstructions string = '''
Post-deployment steps:
1. Add the App Service URL to the Redirect URIs in the Entra ID App Registration
   (Authentication → Add a platform → Web → https://<appName>.azurewebsites.net/signin-oidc)
2. Verify the certificate is uploaded to Key Vault under the configured name
3. Configure a custom domain and SSL binding in the App Service if required
4. Set WEBSITE_RUN_FROM_PACKAGE=1 before deploying the application package
'''
