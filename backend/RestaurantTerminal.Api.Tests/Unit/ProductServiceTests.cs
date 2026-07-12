using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Tests.Unit;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreatesProductWithPrimaryPhotoMetadata()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var service = new ProductService(db);
        var category = await service.CreateCategoryAsync(new CategoryRequest("Plates", 1));

        var product = await service.CreateProductAsync(new ProductRequest(
            "Steak",
            "Grilled",
            12.50m,
            category.Id,
            PhotoUrl: "/media/steak.jpg",
            PhotoAltText: "Steak plate"));

        Assert.Equal("Steak", product.Name);
        Assert.Equal(12.50m, product.Price);
        Assert.Equal("/media/steak.jpg", product.MainImage?.Url);
        Assert.Equal("Steak plate", product.MainImage?.AltText);
    }

    [Fact]
    public async Task RejectsNegativePrice()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var service = new ProductService(db);
        var category = await service.CreateCategoryAsync(new CategoryRequest("Drinks"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProductAsync(new ProductRequest("Water", null, -1m, category.Id)));
    }
}
