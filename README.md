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
- **InteractiveServer** is used only for admin edit pages (`/admin/projects`, `/admin/articles/{id}`) that require Syncfusion Grid and Rich Text Editor interactivity.

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
├── BrandsAdvisory/             # Blazor Web App (UI layer)
│   ├── Components/
│   │   ├── Layout/             # NavMenu, MainLayout
│   │   └── Pages/              # Public pages + Admin pages
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

The UI project depends only on `Core` interfaces. All Cosmos DB access is encapsulated in `Infrastructure`, registered via dependency injection in `Program.cs`.

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

All sensitive values use the `__PLACEHOLDER__` convention and must never be committed to source control. Set them in `appsettings.Development.json` locally and in **Azure App Service → Configuration** for production.

| Placeholder | Where to find it | Config key |
|---|---|---|
| `__TENANT_ID__` | Entra ID → App registration → Overview | `AzureAd:TenantId` |
| `__CLIENT_ID__` | Entra ID → App registration → Overview | `AzureAd:ClientId` |
| `__KEYVAULT_URI__` | Key Vault → Overview → Vault URI | `AzureAd:ClientCertificates:0:KeyVaultUrl` |
| `__KEYVAULT_CERT_NAME__` | Key Vault → Certificates → certificate name | `AzureAd:ClientCertificates:0:KeyVaultCertificateName` |
| `__COSMOS_ENDPOINT__` | Cosmos DB account → Overview → URI | `CosmosDb:EndpointUri` |

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

| Secret name | Value |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Download from Azure App Service → Get publish profile |
| `AZURE_AD_TENANT_ID` | Entra ID tenant ID |
| `AZURE_AD_CLIENT_ID` | App registration client ID |
| `AZURE_AD_CERT_THUMBPRINT` | Thumbprint of the authentication certificate |
| `COSMOS_ENDPOINT` | Cosmos DB endpoint URI |
| `COSMOS_KEY` | Cosmos DB primary key |
| `OWNER_OID` | Your Entra ID object ID |

### Legal Page

Before going live, fill in the placeholder values in [`src/BrandsAdvisory/Components/Pages/Legal.razor`](src/BrandsAdvisory/Components/Pages/Legal.razor):

- `__STREET_ADDRESS__` — Street and house number
- `__ZIP_CITY__` — Postal code and city
- `__CONTACT_EMAIL__` — Public contact email address

---

## Local Development

```bash
# Restore and build
dotnet restore
dotnet build

# Run the app
dotnet run --project src/BrandsAdvisory
```

The app starts at `https://localhost:7000` (or as configured in `launchSettings.json`).

---

## License

MIT — see [LICENSE](LICENSE)
