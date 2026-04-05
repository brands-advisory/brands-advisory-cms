# Blazor Portfolio CMS
### A production-ready blueprint for the modern Microsoft stack

This project demonstrates how to build and deploy a production-ready .NET web application end-to-end on the Microsoft stack:

- **GitHub Copilot** — AI-assisted development throughout
- **Blazor Web App** — Static SSR for SEO-optimized public pages, InteractiveWebAssembly for the admin interface
- **Azure-native** — Cosmos DB, Key Vault, Managed Identity, zero secrets stored anywhere
- **Infrastructure as Code** — Bicep templates for all Azure resources
- **CI/CD with GitHub Actions** — automated deployment for both app and infrastructure using OIDC Federated Credentials (no client secrets)

The application itself is a freelancer portfolio site — publicly readable with owner-only content editing via Microsoft Entra ID. No separate admin user database required.

> This repository is intentionally kept as a clean, well-documented blueprint. Fork it and adapt it to your own use case.

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

## Initial Setup (once per project)

Before GitHub Actions can deploy infrastructure and code, a one-time manual setup is required to bootstrap the deployment identity and permissions.

### 1. Create Resource Group
```bash
az group create \
  --name <resource-group-name> \
  --location <location>
```

### 2. Create Deployment Service Principal

Use `Create-ServicePrincipalForDeployment.ps1` from [cloud-admin-toolkit](https://github.com/brands-advisory/cloud-admin-toolkit):
```powershell
.\Create-ServicePrincipalForDeployment.ps1 -ConfigName <project-name>
```

### 3. Assign Roles on Resource Group

Two roles are required on the resource group:
```bash
# Contributor — create and manage all resources
az role assignment create \
  --assignee <service-principal-app-id> \
  --role "Contributor" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>"

# User Access Administrator — required for Bicep to set RBAC role assignments
# on Cosmos DB, Key Vault, Storage. Must be on RG level — resources don't
# exist yet when the service principal is created (chicken-and-egg problem).
az role assignment create \
  --assignee <service-principal-app-id> \
  --role "User Access Administrator" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>"
```

### 4. Add OIDC Federated Credential

Use `Add-FederatedCredentialForGitHub.ps1` from [cloud-admin-toolkit](https://github.com/brands-advisory/cloud-admin-toolkit):
```powershell
.\Add-FederatedCredentialForGitHub.ps1 -ConfigName <project-name>
```

After this step no client secret is stored anywhere. OIDC handles authentication via GitHub's identity provider.

### 5. Create App Registration with Certificate

Use `Create-AppRegistrationWithCertificate.ps1` from [cloud-admin-toolkit](https://github.com/brands-advisory/cloud-admin-toolkit):
```powershell
.\Create-AppRegistrationWithCertificate.ps1 -ConfigName <project-name>
```

This creates the Entra ID App Registration for user authentication and generates a self-signed certificate.

### 6. Configure and Run Setup Script
```powershell
# Copy and fill in all values
cp config.example.ps1 config.ps1

# Set dotnet user-secrets, GitHub Secrets, and generate
# main.local.bicepparam in one step
.\setup.ps1 -All
```

### 7. Push to main — CI/CD takes over
```bash
git push origin main
```

deploy-infrastructure.yml creates:
- Key Vault
- Cosmos DB account, database, container
- Storage Account with article-images container
- App Service Plan + Web App
- All RBAC role assignments (Managed Identity)

deploy-app.yml builds and deploys the application.

### 8. Upload Certificate to Key Vault (one-time manual step)

After the first infrastructure deployment, upload the certificate:
Azure Portal → <key-vault-name> → Certificates → Generate/Import → Import
→ Upload the .pem file generated by Create-AppRegistrationWithCertificate.ps1
→ Certificate name must match CERT_NAME in config.ps1

This is the only step that cannot be automated — storing the private key in CI/CD would defeat the purpose of using Key Vault.

### 9. Trigger final deployment

After uploading the certificate, trigger a new deployment:
```bash
git commit --allow-empty -m "chore: trigger deployment after certificate upload"
git push origin main
```

The application is now fully deployed and operational.

---

> **What is manual vs automated:**
>
> | Step | How |
> |---|---|
> | Resource Group | Manual — az group create |
> | Service Principal + OIDC | cloud-admin-toolkit scripts |
> | App Registration + Certificate | cloud-admin-toolkit scripts |
> | All Azure resources (KV, Cosmos, Storage, App Service) | Bicep via GitHub Actions |
> | Certificate upload to Key Vault | Manual — one-time after first deployment |
> | Application deployment | GitHub Actions |
> | Configuration | setup.ps1 + config.ps1 |

## CI/CD

Two GitHub Actions workflows handle automated deployment:

| Workflow | Trigger | What it does |
|---|---|---|
| `deploy-app.yml` | Push to `main` when `src/**` changes | Builds and deploys the Blazor app to Azure Web App |
| `deploy-infrastructure.yml` | Push to `main` when `infra/**` changes | Deploys Bicep templates to Azure |

Both workflows use **OIDC Federated Credentials** for authentication — no client secrets are stored in GitHub.

### Authentication Setup (OIDC)

1. Create a Service Principal with **Contributor** and **User Access Administrator** roles on the resource group (see step 3 in [Initial Setup](#initial-setup-once-per-project))
2. Add a Federated Credential to the Service Principal:
   - **Issuer:** `https://token.actions.githubusercontent.com`
   - **Subject:** `repo:{org}/{repo}:ref:refs/heads/main`
   - **Audiences:** `api://AzureADTokenExchange`

See: https://aka.ms/azureactions-oidc

Scripts for creating the service principal and configuring the federated credential are available in [rbrands/cloud-admin-toolkit](https://github.com/rbrands/cloud-admin-toolkit).

### Required GitHub Secrets

Add the following secrets in `Settings → Secrets and variables → Actions → New repository secret`:

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | App ID of the deployment service principal |
| `AZURE_TENANT_ID` | Entra ID Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID |
| `AZURE_RESOURCE_GROUP` | Resource group name (e.g. `rg-brands-advisory`) |
| `AZURE_WEBAPP_NAME` | Azure Web App name (e.g. `brands-advisory`) |
| `APP_NAME` | Azure Web App name |
| `PLAN_NAME` | App Service Plan name |
| `COSMOS_ACCOUNT_NAME` | Cosmos DB account name |
| `COSMOS_DATABASE_ID` | Database name |
| `COSMOS_CONTAINER_NAME` | Container name |
| `KEY_VAULT_NAME` | Key Vault name |
| `CERT_NAME` | Certificate name in Key Vault |
| `CLIENT_ID` | brands-advisory-cms App Registration Client ID |
| `TENANT_ID` | Entra ID Tenant ID |
| `SYNCFUSION_LICENSE_KEY` | Syncfusion Community License key |
| `STORAGE_ACCOUNT_NAME` | Azure Storage Account name (e.g. `stbrandsadvisory`) |

### Deploying Infrastructure (Bicep)

Infrastructure is defined in `infra/main.bicep`.

**1. Generate the local parameter file** (if not already done via `setup.ps1 -Bicep`):

```powershell
.\setup.ps1 -Bicep
# Generates infra/main.local.bicepparam from config.ps1
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

> **Note:** The Key Vault is created automatically by the Bicep template. After the first deployment, upload the authentication certificate to Key Vault manually: **Azure Portal → kv-{name} → Certificates → Generate/Import**. The Key Vault URI is an output of the Bicep deployment and does not need to be configured separately.

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

```powershell
Copy-Item config.example.ps1 config.ps1
# Edit config.ps1 and replace all __PLACEHOLDER__ values
.\setup.ps1 -Secrets
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
---

Maintained by  
**Robert Brands**  
Freelance IT Consultant | Solution Architect | Cloud Adoption & GenAI