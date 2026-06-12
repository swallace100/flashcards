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

`backend/.env` should contain:

```
ConnectionStrings__DefaultConnection=Server=(localdb)\mssqllocaldb;Database=flashcards;Trusted_Connection=true
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

## Authentication

The app uses **Microsoft Entra ID (Azure AD)** for user authentication. Only authenticated users can access the app or call the API.

**Frontend (Blazor WASM):** Uses `Microsoft.Authentication.WebAssembly.Msal` to run the OAuth 2.0 PKCE flow in the browser. Protected pages (`Home`, `CollectionPage`, `Review`) declare `@attribute [Authorize]`, which causes `AuthorizeRouteView` to redirect unauthenticated users to the Microsoft login page. The `/authentication/{action}` route (handled by `Authentication.razor`) is left without `[Authorize]` so it always renders and can process the login callback. After a successful login, MSAL caches the token in session storage and attaches it as a Bearer token to all API requests via `AuthorizationMessageHandler`.

**Backend (ASP.NET Core):** Uses `Microsoft.Identity.Web` to validate incoming JWT Bearer tokens. A fallback authorization policy requires all endpoints to have an authenticated user, so no route is accidentally left open.

**Configuration:** The frontend reads its Entra ID settings from `appsettings.json`. In production these values are injected at build time by GitHub Actions from repository secrets (see GitHub Actions secrets table below). The relevant secrets are:

| Secret | Description |
|--------|-------------|
| `AZURE_AD_TENANT_ID` | Your Entra ID tenant ID. |
| `AZURE_AD_CLIENT_ID` | The client ID of the app registration. |

The backend reads its Entra ID settings from App Service Application Settings (`AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience`).

**App registration (Azure Portal):** The app registration needs:
- A redirect URI pointing to `https://<your-swa-url>/authentication/login-callback`
- "Expose an API" with an `access_as_user` delegated scope defined (App ID URI: `api://<client-id>`)

## Spaced repetition

Cards use the SM-2 algorithm. After revealing the answer, rate the card:

| Button | Meaning                                       |
| ------ | --------------------------------------------- |
| Easy   | Remembered without effort (longer interval)   |
| Normal | Remembered normally (standard interval)       |
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
- **Database**: Azure SQL (Serverless tier) with Entra ID-only authentication. No SQL passwords anywhere.

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

**GitHub Actions secrets** — two secrets must be set in the repository (Settings → Secrets and variables → Actions):

| Secret | Description |
|--------|-------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_*` | Auto-generated by Azure when linking the Static Web App to GitHub. |
| `BACKEND_URL` | The full URL of your App Service backend (e.g. `https://your-backend.azurewebsites.net`). Injected into `appsettings.json` at build time so the real URL is never committed to the repo. |

Azure SQL Serverless auto-pauses after inactivity, so the first request after a break may take a few seconds to resume. For daily study use you'll never hit any limits. This setup costs pennies per month.
