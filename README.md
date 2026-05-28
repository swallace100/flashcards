# Flashcards

A personal spaced repetition flashcard app. Supports multiple collections and importing cards from Excel spreadsheets. Built with Blazor WebAssembly (frontend) and ASP.NET Core (backend), backed by PostgreSQL.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 17

## First-time setup

**1. Create the database**

In DBeaver or psql, create a database named `flashcards`, then run the schema:

```powershell
psql -h localhost -U postgres -d flashcards -f database/schema.sql
```

**2. Configure the API secret**

Copy the example env file and fill in your password:

```powershell
Copy-Item Backend/.env.example Backend/.env
```

Edit `Backend/.env`:

```
ConnectionStrings__DefaultConnection=Host=localhost;Database=flashcards;Username=postgres;Password=YOUR_PASSWORD
```

## Running the app

Open two terminals and run each project:

```powershell
# Terminal 1 — API (http://localhost:5288)
cd Backend
dotnet run
```

```powershell
# Terminal 2 — Frontend (http://localhost:5174)
cd Frontend
dotnet run
```

Then open the URL shown in the frontend terminal in your browser.

## Importing flashcards from Excel

1. Create an `.xlsx` file with columns: `front`, `back`, `notes` (notes is optional)
2. In the app, create a collection
3. POST the file to the API:

```powershell
curl -X POST http://localhost:5288/collections/{id}/import -F "file=@path/to/your/file.xlsx"
```

## Spaced repetition

Cards use the SM-2 algorithm. After revealing the answer, rate the card:

| Button | Meaning |
|--------|---------|
| Easy   | Remembered without effort — longer interval |
| Normal | Remembered correctly — standard interval |
| Hard   | Remembered with difficulty — shorter interval |
| Again  | Forgot — resets to day 1 |

A 45-second timer runs per card. If it expires before you rate the card, it automatically counts as **Again**.

## Deploying to Azure

When deploying to Azure App Service, set `ConnectionStrings__DefaultConnection` as an Application Setting — no `.env` file needed. The Blazor frontend can be hosted on Azure Static Web Apps; update `FlashcardsApp/wwwroot/appsettings.json` with the production API URL before publishing.
