# brands-advisory-cms

Blazor Web App blueprint for a freelancer portfolio site.  
Built with **.NET 10**, **Azure Web App**, **Cosmos DB**, **Entra ID** authentication, and **Syncfusion** components.

The site is publicly readable (Static SSR for SEO) and supports owner-only editing via Microsoft Entra ID — no separate admin user store required.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | [.NET 10 / Blazor Web App](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) |
| Hosting | [Azure Web App (App Service)](https://learn.microsoft.com/en-us/azure/app-service/) |
| Database | [Azure Cosmos DB NoSQL](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/) |
| Authentication | [Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity/) via [Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web) |
| UI Components | [Syncfusion Blazor](https://www.syncfusion.com/blazor-components) (Community License) |
| CI/CD | [GitHub Actions](https://docs.github.com/en/actions) |

---

## Architecture Decisions

### Render Modes

- **Static SSR** is the default render mode for all public pages (`/`, `/projects`, `/articles`, `/articles/{slug}`). This ensures fast initial load times and full SEO indexability without JavaScript requirements.
- **InteractiveWebAssembly** is used for admin edit pages (`/admin/about`, `/admin/projects`, `/admin/articleeditor/{id?}`) that require Syncfusion Grid and Rich Text Editor interactivity. These pages live in the `BrandsAdvisory.Client` Blazor WebAssembly project and are served by the host server. Data is fetched via minimal API endpoints (`/api/about`, `/api/projects`, `/api/articles`) that require the `SiteAdmin` role.

### Owner-Only Editing

There is no separate admin user database. The site owner is identified by the **`SiteAdmin` App Role** assigned in the Entra ID Enterprise Application. The role is checked on every request server-side via `IOwnerService` — UI visibility alone is never relied upon.

```
User logs in via Entra ID
       ↓
App Role claims included in token
       ↓
IOwnerService.IsOwner() checks user.IsInRole("SiteAdmin")
       ↓
IsOwner cascaded as bool to all Blazor components
```

To grant access: **Entra ID → Enterprise Applications → your app → Users and groups → Add user/group → assign role `SiteAdmin`**.

### Data Model

All content is stored in a single Cosmos DB container (`content`) with a `type` field as logical partition:

| type | Description |
|---|---|
| `about` | Single document, id = `about` |
| `article` | One document per article, partition key = `article` |
| `project` | One document per project, partition key = `project` |

---

## Project Structure

```
brands-advisory-cms.slnx
src/
├── BrandsAdvisory/             # Blazor Web App host (SSR + API)
│   ├── Components/
│   │   ├── Layout/             # NavMenu, MainLayout
│   │   └── Pages/              # Public pages (Static SSR)
│   ├── Endpoints/              # Minimal API endpoints for admin data
│   │   ├── AboutEndpoints.cs
│   │   ├── ArticleEndpoints.cs
│   │   └── ProjectEndpoints.cs
│   ├── Models/
│   │   └── UserInfo.cs         # DTO for /api/user (auth state for WASM)
│   └── Program.cs
├── BrandsAdvisory.Client/      # Blazor WebAssembly client (admin pages)
│   ├── Pages/Admin/            # Admin edit pages (InteractiveWebAssembly)
│   │   ├── AboutEditor.razor
│   │   ├── ArticleEditor.razor
│   │   └── Projects.razor
│   ├── Services/
│   │   ├── ApiAuthenticationStateProvider.cs  # Calls /api/user
│   │   ├── HttpAboutRepository.cs
│   │   ├── HttpArticleRepository.cs
│   │   └── HttpProjectRepository.cs
│   └── Program.cs
├── BrandsAdvisory.Core/        # Domain layer (no infrastructure dependencies)
│   ├── Interfaces/
│   │   ├── IRepository.cs          # Generic base repository interface
│   │   ├── IArticleRepository.cs
│   │   ├── IProjectRepository.cs
│   │   ├── IAboutRepository.cs
│   │   └── IOwnerService.cs
│   ├── Models/
│   │   ├── CosmosDocument.cs       # Base class for all Cosmos DB documents
│   │   ├── Article.cs
│   │   ├── Project.cs
│   │   ├── AboutContent.cs
│   │   └── ProfileLink.cs
│   └── Services/
│       └── OwnerService.cs
└── BrandsAdvisory.Infrastructure/  # Data access layer (Cosmos DB)
    └── Repositories/
        ├── CosmosRepository.cs     # Generic base repository (Cosmos DB SDK)
        ├── ArticleRepository.cs
        ├── ProjectRepository.cs
        └── AboutRepository.cs
```

Public pages use Static SSR and depend only on `Core` interfaces, served from Cosmos DB via `Infrastructure`. Admin pages run as WebAssembly in `BrandsAdvisory.Client` and call the host server's minimal API endpoints — all protected by the `SiteAdmin` role.

---

## Setup

### Required Azure Resources

1. **Azure Cosmos DB account** — NoSQL API, database `brands-advisory`, container `content` with partition key `/type`
2. **Azure App Service** — .NET 10, Linux or Windows
3. **Microsoft Entra ID App Registration** — for authentication

### App Registration (Microsoft Entra ID)

1. Go to [portal.azure.com](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**
2. Name: `brands-advisory-cms`
3. Supported account types: **Single tenant**
4. Redirect URI: `https://<your-app>.azurewebsites.net/signin-oidc`
5. After creation:
   - **Certificates & secrets** → New client secret → copy the value → `__CLIENT_SECRET__`
   - **Overview** → copy **Application (client) ID** → `__CLIENT_ID__`
   - **Overview** → copy **Directory (tenant) ID** → `__TENANT_ID__`
6. To find your **Owner OID**: go to **Microsoft Entra ID** → **Users** → select your user → copy **Object ID** → `__OWNER_OID__`

### Configuration Placeholders

All sensitive values use the `__PLACEHOLDER__` convention and must never be committed to source control. Set them via [`dotnet user-secrets`](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) locally (see [Local Development](#local-development)) and in **Azure App Service → Configuration** for production.

| Placeholder | Where to find it | Config key |
|---|---|---|
| `__TENANT_ID__` | Entra ID → App registration → Overview | `AzureAd:TenantId` |
| `__CLIENT_ID__` | Entra ID → App registration → Overview | `AzureAd:ClientId` |
| `__KEYVAULT_URI__` | Key Vault → Overview → Vault URI | `AzureAd:ClientCertificates:0:KeyVaultUrl` |
| `__KEYVAULT_CERT_NAME__` | Key Vault → Certificates → certificate name | `AzureAd:ClientCertificates:0:KeyVaultCertificateName` |
| `__COSMOS_ENDPOINT__` | Cosmos DB account → Overview → URI | `CosmosDb:EndpointUri` |
| `__DATABASE_ID__` | The Cosmos DB database name (e.g. `brands-advisory`) | `CosmosDb:DatabaseId` |
| `__CONTAINER_NAME__` | The Cosmos DB container name (e.g. `content`) | `CosmosDb:ContainerName` |
| `__SYNCFUSION_LICENSE_KEY__` | [Syncfusion License & Downloads](https://www.syncfusion.com/account/downloads) → Community license key (free) | `Syncfusion:LicenseKey` |

> **Note:** The Syncfusion license key is stored only in server-side configuration and never exposed in client files. At startup, the Blazor WebAssembly app fetches it from the server via `/api/config`. The `BrandsAdvisory.Client/wwwroot/appsettings.json` contains no secrets.

> **Note:** The app uses certificate-based authentication via **Azure Key Vault** (`SourceType: KeyVault`). `Microsoft.Identity.Web` loads the certificate automatically at startup using the configured Managed Identity (production) or Azure CLI credentials (local development via `az login`). Assign the **Key Vault Certificate User** role to the App Service Managed Identity and to your developer account in the Key Vault access policies.

> **Cosmos DB access uses Managed Identity** (`DefaultAzureCredential`) — no primary key is stored anywhere. Locally, `az login` credentials are used automatically. In production, the App Service System-Assigned Managed Identity receives the **Cosmos DB Built-in Data Contributor** role via the `cosmos-rbac` Bicep module.

**Local development** — copy `set-secrets.sh.example` to `set-secrets.sh`, fill in the values, then run it from the repository root:

```bash
cp set-secrets.sh.example set-secrets.sh
# Edit set-secrets.sh and replace all __PLACEHOLDER__ values
bash set-secrets.sh
```

This uses [`dotnet user-secrets`](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), which stores secrets outside the project directory and never touches source control.

### GitHub Secrets (for GitHub Actions deployment)

The workflow in `.github/workflows/deploy-app.yml` requires two secrets in the repository (`Settings → Secrets and variables → Actions → New repository secret`):

| Secret name | Value |
|---|---|
| `AZURE_WEBAPP_NAME` | Name of the Azure App Service web app (e.g. `brands-advisory-cms`) |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Contents of the publish profile XML — Azure Portal → App Service → **Get publish profile** → copy the full file content |

All application configuration (Entra ID, Cosmos DB, Syncfusion key) is set directly in the App Service configuration via Bicep — no additional GitHub secrets are needed for those values.

### Deploying Infrastructure (Bicep)

Infrastructure is defined in `infra/main.bicep`. Deploy with the Azure CLI:

**1. Create a local parameter file** (gitignored):

```bash
cp infra/main.bicepparam infra/main.local.bicepparam
# Edit infra/main.local.bicepparam and replace all __PLACEHOLDER__ values
```

**2. Deploy to Azure:**

```bash
az deployment group create \
  --resource-group <your-resource-group> \
  --template-file infra/main.bicep \
  --parameters infra/main.local.bicepparam
```

This deploys:
- Azure Cosmos DB account, database, and container
- Azure App Service Plan + Web App (.NET 10, Linux)
- All App Service configuration (Entra ID, Cosmos DB endpoint, Syncfusion key)
- Key Vault RBAC — **Key Vault Certificate User** role for the Web App Managed Identity
- Cosmos DB RBAC — **Built-in Data Contributor** role for the Web App Managed Identity

> **Note:** The Key Vault itself is not created by the Bicep template. It must already exist and the certificate must be uploaded under the configured name before deploying the application.

### Legal Page

Before going live, fill in the placeholder values in [`src/BrandsAdvisory/Components/Pages/Legal.razor`](src/BrandsAdvisory/Components/Pages/Legal.razor):

- `__STREET_ADDRESS__` — Street and house number
- `__ZIP_CITY__` — Postal code and city
- `__CONTACT_EMAIL__` — Public contact email address

---

## Local Development

### Prerequisites

**1. Trust the local HTTPS developer certificate** (once per machine):

```bash
dotnet dev-certs https --trust
```

Without this, the OIDC callback over HTTPS will fail with "Correlation failed" in the browser.

**2. Add the local redirect URI to the Entra ID App Registration:**

In the Azure Portal → **App registrations** → your app → **Authentication** → add:

```
https://localhost:7000/signin-oidc
```

**3. Log in with the Azure CLI** (once per session, needed for Key Vault certificate loading and Cosmos DB access via `DefaultAzureCredential`):

```bash
az login
```

**4. Set local secrets** (once, see [Setup](#setup) above):

```bash
cp set-secrets.sh.example set-secrets.sh
# Edit set-secrets.sh and replace all __PLACEHOLDER__ values
bash set-secrets.sh
```

### Running the app

Always use the `https` profile — the OIDC flow requires HTTPS for cookies to work correctly:

```bash
dotnet run --project src/BrandsAdvisory --launch-profile https
```

The app starts at `https://localhost:7000`.

> **Note:** Running with `--launch-profile http` (plain HTTP) will cause "Correlation failed" on the login callback because secure cookies cannot be set over HTTP.

---

## License

MIT — see [LICENSE](LICENSE)
