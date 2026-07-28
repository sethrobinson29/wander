# Wander

A Magic: The Gathering deck builder and playtesting app. Build and share decklists, write primers, and goldfish your decks in a virtual playtesting table.

## Stack

- **API:** ASP.NET Core (.NET 10) + Entity Framework Core + PostgreSQL
- **Frontend:** Blazor WebAssembly + MudBlazor
- **Auth:** ASP.NET Core Identity + JWT
- **Real-time:** SignalR (playtesting)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Running Locally

```bash
# Start the database
docker-compose up -d postgres

# Run the API
dotnet run --project src/Wander.Api

# Run the client (separate terminal)
dotnet run --project src/Wander.Client
```

API reference available at `http://localhost:{port}/scalar/v1`.

> `Wander.Client` only ships an `appsettings.Production.json` (`ApiBaseUrl` pointing at the production API) — there's no base `appsettings.json`. Locally, Blazor WASM runs as `Development` (the default `dotnet run` profile sets this) and never loads `appsettings.Production.json`, so `ApiBaseUrl` falls through to `wwwroot/appsettings.Development.json`, which points at `http://localhost:5156`.

## Initial Setup (Admin User + Card Data)

The database starts empty — no admin account and no card data. Do this once after your first run (or after resetting the `postgres` volume).

### 1. Create the admin account

Admin credentials are never committed. Locally they're supplied via [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (the `Wander.Api` project already has a `UserSecretsId` configured):

```bash
dotnet user-secrets set "Admin:Email" "admin@example.com" --project src/Wander.Api
dotnet user-secrets set "Admin:Password" "SomeStrongPassword1!" --project src/Wander.Api
```

Start (or restart) the API — on every startup it checks for an existing admin and seeds one from these secrets if none exists yet, and ensures the `Admin` role is assigned.

> In production these are set as `Admin__Email` / `Admin__Password` environment variables instead — see `.env.example`.

### 2. Run the Scryfall card sync

Card search and deck building need Scryfall's card data in the database first. This is a one-time manual trigger locally (it also runs automatically on a weekly schedule once the API is up).

Log in as the admin user to get a JWT, then trigger the sync job:

```bash
TOKEN=$(curl -s -X POST http://localhost:5156/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"SomeStrongPassword1!"}' \
  | jq -r .accessToken)

curl -s -X POST http://localhost:5156/admin/sync \
  -H "Authorization: Bearer $TOKEN"
```

Or, once the Blazor client is running (`dotnet run --project src/Wander.Client`), log in with the admin account and trigger it from the Admin Panel (`/admin`) instead.

The sync downloads Scryfall's bulk card data and upserts it — expect ~27k+ cards and a couple of minutes the first time. Card search will return empty results until it completes.

## Running Tests

```bash
# Unit tests only
dotnet test --filter "Category!=Integration"

# Integration tests (requires Docker + live Scryfall connection, slow)
dotnet test --filter "Category=Integration"
```

> Docker must be running for any test that hits the database.

