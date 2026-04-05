// ---------------------------------------------------------------------------
// main.bicepparam — Parameter values for main.bicep
//
// Copy this file, replace all __PLACEHOLDER__ values with real values,
// and keep the copy out of source control.
// ---------------------------------------------------------------------------

using './main.bicep'

// App Service
param appName = '__APP_NAME__'
param planName = '__PLAN_NAME__'

// Cosmos DB
param cosmosAccountName = '__COSMOS_ACCOUNT_NAME__'
param cosmosDatabaseId = '__DATABASE_ID__'
param cosmosContainerName = '__CONTAINER_NAME__'

// Key Vault
param keyVaultName = '__KEY_VAULT_NAME__'
param keyVaultCertificateName = '__CERT_NAME__'

// Entra ID / App Registration
param tenantId = '__TENANT_ID__'
param clientId = '__CLIENT_ID__'

// Storage
param storageAccountName = '__STORAGE_ACCOUNT_NAME__'
