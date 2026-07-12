using Microsoft.EntityFrameworkCore;

namespace RestaurantTerminal.Api.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureCompatibleAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlite())
        {
            return;
        }

        await db.Database.OpenConnectionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('Orders');";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(1));
            }
        }

        if (!columns.Contains("WaiterName"))
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN WaiterName TEXT NULL", cancellationToken);
        }
    }
}
