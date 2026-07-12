# Pos-Full

Restaurant terminal/POS prototype with:

- ASP.NET Core API, SignalR, EF Core, SQLite
- React, Vite, TypeScript frontend
- Docker Compose for one-command full-stack runs

## Recommended Flow

For normal testing, use Docker Compose first:

```powershell
docker compose up --build
```

Open:

```txt
http://localhost:8080
```

For active development, run backend and frontend separately:

```powershell
dotnet run --project backend\RestaurantTerminal.Api\RestaurantTerminal.Api.csproj
```

```powershell
cd frontend\restaurant-terminal-web
npm install
npm run dev
```

Open the Vite app at:

```txt
http://localhost:5173
```

The Vite dev server proxies `/api` and `/hubs` to the backend.

## Docker Commands

Build and run the full app:

```powershell
docker compose up --build
```

Run in the background:

```powershell
docker compose up --build -d
```

Stop containers:

```powershell
docker compose down
```

Rebuild only the app image:

```powershell
docker compose build restaurant-terminal
```

Run backend tests in Docker:

```powershell
docker compose --profile test run --rm backend-tests
```

Run frontend tests in Docker:

```powershell
docker compose --profile test run --rm frontend-tests
```

Run both test services:

```powershell
docker compose --profile test run --rm backend-tests
docker compose --profile test run --rm frontend-tests
```

## Database Commands

Local backend SQLite database:

```txt
backend\RestaurantTerminal.Api\restaurant-terminal.db
```

Docker SQLite database:

```txt
/app/data/restaurant-terminal.db
```

Docker persists the database in the named volume:

```txt
restaurant-terminal-data
```

Reset Docker database and containers:

```powershell
docker compose down -v
```

Reset local database:

```powershell
Remove-Item backend\RestaurantTerminal.Api\restaurant-terminal.db -ErrorAction SilentlyContinue
```

The app currently uses `Database.EnsureCreated()` on startup and seeds demo categories, products, and tables automatically.

## Backend Commands

Restore backend packages:

```powershell
dotnet restore backend\RestaurantTerminal.Api\RestaurantTerminal.Api.csproj
```

Run backend API:

```powershell
dotnet run --project backend\RestaurantTerminal.Api\RestaurantTerminal.Api.csproj
```

Build backend:

```powershell
dotnet build backend\RestaurantTerminal.Api\RestaurantTerminal.Api.csproj
```

Run backend tests:

```powershell
dotnet test backend\RestaurantTerminal.Api.Tests\RestaurantTerminal.Api.Tests.csproj -v minimal
```

Build the solution:

```powershell
dotnet build RestaurantTerminal.slnx
```

## Frontend Commands

Install packages:

```powershell
cd frontend\restaurant-terminal-web
npm install
```

Run frontend dev server:

```powershell
npm run dev
```

Run frontend tests:

```powershell
npm test
```

Build frontend:

```powershell
npm run build
```

Preview built frontend:

```powershell
npm run preview
```

Copy a fresh frontend build into the backend static host:

```powershell
Copy-Item -Path frontend\restaurant-terminal-web\dist\* -Destination backend\RestaurantTerminal.Api\wwwroot -Recurse -Force
```

## EF Core Flow

Current prototype behavior:

- The API uses SQLite.
- The API calls `Database.EnsureCreated()` on startup.
- Demo data is seeded automatically.
- No EF migration files are required for the current prototype.

When the schema stabilizes, switch from `EnsureCreated()` to migrations.

Add EF design package:

```powershell
dotnet add backend\RestaurantTerminal.Api\RestaurantTerminal.Api.csproj package Microsoft.EntityFrameworkCore.Design
```

Install EF CLI tool if needed:

```powershell
dotnet tool install --global dotnet-ef
```

Create the first migration:

```powershell
dotnet ef migrations add InitialCreate --project backend\RestaurantTerminal.Api --startup-project backend\RestaurantTerminal.Api
```

Apply migrations locally:

```powershell
dotnet ef database update --project backend\RestaurantTerminal.Api --startup-project backend\RestaurantTerminal.Api
```

After migrations are introduced, update startup flow from:

```csharp
db.Database.EnsureCreated();
```

to:

```csharp
db.Database.Migrate();
```

Then keep this flow for schema changes:

```powershell
dotnet ef migrations add YourMigrationName --project backend\RestaurantTerminal.Api --startup-project backend\RestaurantTerminal.Api
dotnet ef database update --project backend\RestaurantTerminal.Api --startup-project backend\RestaurantTerminal.Api
dotnet test backend\RestaurantTerminal.Api.Tests\RestaurantTerminal.Api.Tests.csproj -v minimal
docker compose build restaurant-terminal
```

## Useful API URLs

```txt
GET  /api/categories
GET  /api/products
GET  /api/tables
GET  /api/orders/kitchen-queue
GET  /api/reports/sales?from=2026-07-01T10:00:00Z&to=2026-07-06T23:00:00Z
GET  /api/reports/sales/export.csv?from=2026-07-01T10:00:00Z&to=2026-07-06T23:00:00Z
```

SignalR hubs:

```txt
/hubs/kitchen
/hubs/orders
```
