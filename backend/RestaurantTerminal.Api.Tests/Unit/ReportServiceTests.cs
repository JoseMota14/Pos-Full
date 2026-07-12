using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Models;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Tests.Unit;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task AggregatesSalesAndFormatsCsv()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var reports = new ReportService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Plates"));
        var pasta = await products.CreateProductAsync(new ProductRequest("Pasta", null, 8m, category.Id));
        var table = new RestaurantTable { Name = "Table 3", Seats = 4 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(pasta.Id, 3, null)]));

        var rows = await reports.GetSalesAsync(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
        var row = Assert.Single(rows);
        Assert.Equal(3, row.QuantitySold);
        Assert.Equal(24m, row.TotalRevenue);

        var csv = reports.ToCsv(rows);
        Assert.Contains("ProductId,ProductName,Category,QuantitySold,TotalRevenue", csv);
        Assert.Contains("Pasta,Plates,3,24.00", csv);
    }
}

