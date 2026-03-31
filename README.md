# Wander

A Magic: The Gathering deck builder and playtesting app. Build and share decklists, write primers, and golfish your decks in a virtual playtesting table.

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
```

API reference available at `http://localhost:{port}/scalar/v1`.

## Running Tests

```bash
dotnet test
```

> Docker must be running — the test suite includes a database connectivity test.

