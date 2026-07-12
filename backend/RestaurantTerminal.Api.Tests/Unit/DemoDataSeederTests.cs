using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Tests.Unit;

public sealed class DemoDataSeederTests
{
    [Fact]
    public async Task SeedsBaseCategoriesProductsAndTablesOnce()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;

        await DemoDataSeeder.SeedAsync(db);
        await DemoDataSeeder.SeedAsync(db);

        Assert.Equal(3, await db.ProductCategories.CountAsync());
        Assert.Equal(11, await db.Products.CountAsync());
        Assert.Equal(4, await db.RestaurantTables.CountAsync());
        Assert.Contains(await db.ProductCategories.Select(x => x.Name).ToListAsync(), x => x == "Drinks");
        Assert.Contains(await db.ProductCategories.Select(x => x.Name).ToListAsync(), x => x == "Soups");
        Assert.Contains(await db.ProductCategories.Select(x => x.Name).ToListAsync(), x => x == "Sandwiches");
        Assert.All(
            await db.Products.Include(x => x.Images).ToListAsync(),
            product => Assert.StartsWith("/media/", product.MainImage?.Url));
    }

    [Fact]
    public async Task AddsMissingDemoDataWhenSomeDataAlreadyExists()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        db.ProductCategories.Add(new ProductCategory { Name = "Drinks", SortOrder = 10 });
        await db.SaveChangesAsync();

        await DemoDataSeeder.SeedAsync(db);

        Assert.Equal(3, await db.ProductCategories.CountAsync());
        Assert.Equal(11, await db.Products.CountAsync());
    }
}
