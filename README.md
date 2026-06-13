# Flashcards

A personal spaced repetition flashcard app. Supports multiple collections and importing cards from Excel spreadsheets. Built with Blazor WebAssembly (frontend) and ASP.NET Core (backend), backed by SQL Server (Azure SQL in production).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (included with Visual Studio, or via the [SQL Server Express LocalDB installer](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb))

## First-time setup

**1. Create the database**

```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -Q "CREATE DATABASE flashcards"
sqlcmd -S "(localdb)\mssqllocaldb" -d flashcards -i database/schema.sql
```

**2. Configure the connection string**

Copy the example env file:

```powershell
Copy-Item backend/.env.example backend/.env
```

**3. Configure authentication**

You need an Entra ID app registration (see [Setting up Entra ID](#setting-up-entra-id) below). Once you have one, add your IDs to `backend/.env`:

```
ConnectionStrings__DefaultConnection=Server=(localdb)\mssqllocaldb;Database=flashcards;Trusted_Connection=true
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-client-id>
AzureAd__Audience=api://<your-client-id>
```

And add them to `frontend/wwwroot/appsettings.Development.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5288",
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ValidateAuthority": true
  }
}
```

## Running the app

Open two terminals and run each project:

```powershell
# Terminal 1: API (http://localhost:5288)
cd backend
dotnet run
```

```powershell
# Terminal 2: Frontend (http://localhost:5029)
cd frontend
dotnet run
```

Then open the URL shown in the frontend terminal in your browser.

## Importing flashcards from Excel

Prepare an `.xlsx` file with columns: `front`, `back`, `notes` (notes is optional). A download template is available in the app's upload section.

**Via the UI:** Open a collection, tap the upload zone, choose your `.xlsx` file, and tap Upload.

**Via the API:**

```powershell
curl -X POST http://localhost:5288/collections/{id}/import -F "file=@path/to/your/file.xlsx"
```

Replace `{id}` with the collection's numeric ID.

## Setting up Entra ID

The app requires an Azure AD app registration for user authentication. Create one before running locally or deploying.

**1. Create the app registration**

Azure Portal → Microsoft Entra ID → App registrations → New registration:
- Name: anything (e.g. `flashcards`)
- Supported account types: *Accounts in this organizational directory only*
- Redirect URI: leave blank for now

Note the **Application (client) ID** and **Directory (tenant) ID** from the overview page — you'll need these throughout.

**2. Add redirect URIs**

Authentication → Add a platform → Single-page application. Add:
- `https://<your-swa-url>/authentication/login-callback` — production
- `http://localhost:5029/authentication/login-callback` — local dev

**3. Expose an API**

Expose an API → Set (next to Application ID URI) → Save the default `api://<client-id>`. Then add a scope:
- Scope name: `access_as_user`
- Who can consent: Admins and users
- State: Enabled

## Authentication

The app uses **Microsoft Entra ID (Azure AD)** for user authentication. Only authenticated users can access the app or call the API.

**Frontend (Blazor WASM):** Uses `Microsoft.Authentication.WebAssembly.Msal` to run the OAuth 2.0 PKCE flow in the browser. Protected pages (`Home`, `CollectionPage`, `Review`) declare `@attribute [Authorize]`, which causes `AuthorizeRouteView` to redirect unauthenticated users to the Microsoft login page. The `/authentication/{action}` route (handled by `Authentication.razor`) is left without `[Authorize]` so it always renders and can process the login callback. After a successful login, MSAL caches the token in session storage and attaches it as a Bearer token to all API requests via `AuthorizationMessageHandler`.

**Backend (ASP.NET Core):** Uses `Microsoft.Identity.Web` to validate incoming JWT Bearer tokens. A fallback authorization policy requires all endpoints to have an authenticated user, so no route is accidentally left open.

## Spaced repetition

Cards use the SM-2 algorithm. After revealing the answer, rate the card:

| Button | Meaning                                       |
| ------ | --------------------------------------------- |
| Easy   | Remembered without effort (longer interval)   |
| Good   | Remembered correctly (standard interval)      |
| Hard   | Remembered with difficulty (shorter interval) |
| Again  | Forgot (resets to day 1)                      |

A 45-second timer runs per card. If it expires before you rate the card, it automatically counts as Again.

## Provisioning Azure resources

All Azure resources are defined in [`infra/main.bicep`](infra/main.bicep). To provision from scratch:

```powershell
az group create --name Flashcards --location japaneast

az deployment group create `
  --resource-group Flashcards `
  --template-file infra/main.bicep `
  --parameters entraAdminLogin="you@example.com" entraAdminObjectId="<your-object-id>"
```

Your Entra ID object ID can be found in **Azure Portal → Microsoft Entra ID → Users → your user → Object ID**.

After the first deploy, the outputs will include the Static Web App URL. Re-deploy with it to wire up CORS:

```powershell
az deployment group create `
  --resource-group Flashcards `
  --template-file infra/main.bicep `
  --parameters entraAdminLogin="you@example.com" entraAdminObjectId="<your-object-id>" frontendOrigin="https://<your-swa-url>"
```

Then run the post-deployment steps (schema + Managed Identity grant) described in the Deploying to Azure section below.

## Deploying to Azure

The live app runs entirely on Azure:

- **Frontend**: Blazor WASM hosted on Azure Static Web Apps, deployed automatically via GitHub Actions on push to `main`. The production API URL lives in `frontend/wwwroot/appsettings.json` (and `appsettings.Development.json` overrides it to `http://localhost:5288` for local dev).
- **Backend**: ASP.NET Core hosted on Azure App Service (Linux, F1 free tier).
- **Database**: Azure SQL (Basic DTU tier) with Entra ID-only authentication. No SQL passwords anywhere.

Authentication between the backend and the database uses the App Service's system-assigned Managed Identity, granted access via:

```sql
CREATE USER [your-app-service] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your-app-service];
ALTER ROLE db_datawriter ADD MEMBER [your-app-service];
```

The `ConnectionStrings__DefaultConnection` Application Setting on the App Service uses:

```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;Authentication=Active Directory Managed Identity;Encrypt=True
```

No `.env` file is needed in production. App Service Application Settings take its place.

**GitHub Actions secrets** — set these in the repository (Settings → Secrets and variables → Actions):

| Secret | Description |
|--------|-------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_*` | Auto-generated by Azure when linking the Static Web App to GitHub. |
| `BACKEND_URL` | Full URL of your App Service (e.g. `https://your-backend.azurewebsites.net`). Injected into `appsettings.json` at build time. |
| `AZURE_AD_TENANT_ID` | Directory (tenant) ID from the app registration. |
| `AZURE_AD_CLIENT_ID` | Application (client) ID from the app registration. |

**App Service Application Settings** — in addition to `ConnectionStrings__DefaultConnection`, add:

| Setting | Value |
|---------|-------|
| `AzureAd__TenantId` | Directory (tenant) ID |
| `AzureAd__ClientId` | Application (client) ID |
| `AzureAd__Audience` | `api://<your-client-id>` |

Azure SQL Basic DTU is always on, so there is no cold-start delay. This setup costs around $5/month.
