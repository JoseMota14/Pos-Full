# Migrations

The prototype uses `Database.EnsureCreated()` so it can run immediately on a local SQLite file.

When the schema stabilizes, add `Microsoft.EntityFrameworkCore.Design` and create real migrations with:

```powershell
dotnet ef migrations add InitialCreate --project backend\RestaurantTerminal.Api
dotnet ef database update --project backend\RestaurantTerminal.Api
```
