# Copy this file to config.ps1 and fill in your values.
# NEVER commit config.ps1 to source control.

$config = @{
    # Azure
    SubscriptionId       = "__SUBSCRIPTION_ID__"
    ResourceGroup        = "__RESOURCE_GROUP__"

    # App Service
    AppName              = "__APP_NAME__"
    PlanName             = "__PLAN_NAME__"

    # Cosmos DB
    CosmosAccountName    = "__COSMOS_ACCOUNT_NAME__"
    CosmosDatabaseId     = "__COSMOS_DATABASE_ID__"
    CosmosContainerName  = "__COSMOS_CONTAINER_NAME__"
    CosmosEndpointUri    = "__COSMOS_ENDPOINT_URI__"

    # Key Vault
    KeyVaultName         = "__KEY_VAULT_NAME__"
    KeyVaultUrl          = "__KEY_VAULT_URL__"
    CertName             = "__CERT_NAME__"

    # Entra ID
    TenantId             = "__TENANT_ID__"
    ClientId             = "__CLIENT_ID__"

    # Storage
    StorageAccountName   = "__STORAGE_ACCOUNT_NAME__"
    StorageBlobEndpoint  = "__STORAGE_BLOB_ENDPOINT__"

    # Syncfusion
    SyncfusionLicenseKey = "__SYNCFUSION_LICENSE_KEY__"

    # GitHub Actions OIDC
    AzureClientId        = "__AZURE_CLIENT_ID__"
}
