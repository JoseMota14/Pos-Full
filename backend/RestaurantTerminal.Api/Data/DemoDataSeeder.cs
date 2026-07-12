using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var drinks = await GetOrCreateCategoryAsync(db, "Drinks", 10, cancellationToken);
        var soups = await GetOrCreateCategoryAsync(db, "Soups", 20, cancellationToken);
        var sandwiches = await GetOrCreateCategoryAsync(db, "Sandwiches", 30, cancellationToken);


        await AddProductIfMissingAsync(db, "Caldo verde", "Caldo verde", 6.20m, ProductRoute.Kitchen, sandwiches, "/media/caldo-verde.jpg", cancellationToken);
        await AddProductIfMissingAsync(db, "Still Water", "500 ml bottle", 1.50m, ProductRoute.Bar, drinks, "/media/still-water.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Sparkling Water", "500 ml bottle", 1.70m, ProductRoute.Bar, drinks, "/media/sparkling-water.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Orange Juice", "Fresh orange juice", 2.80m, ProductRoute.Bar, drinks, "/media/orange-juice.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "House Lemonade", "Lemon, mint, and soda", 3.20m, ProductRoute.Bar, drinks, "/media/house-lemonade.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Tomato Soup", "Creamy tomato soup", 3.50m, ProductRoute.Kitchen, soups, "/media/tomato-soup.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Vegetable Soup", "Seasonal vegetable soup", 3.25m, ProductRoute.Kitchen, soups, "/media/vegetable-soup.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Chicken Soup", "Chicken broth with noodles", 4.00m, ProductRoute.Kitchen, soups, "/media/chicken-soup.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Ham and Cheese Sandwich", "Toasted ham and cheese", 5.50m, ProductRoute.Kitchen, sandwiches, "/media/ham-cheese-sandwich.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Chicken Club Sandwich", "Chicken, bacon, lettuce, and tomato", 7.80m, ProductRoute.Kitchen, sandwiches, "/media/chicken-club-sandwich.svg", cancellationToken);
        await AddProductIfMissingAsync(db, "Tuna Sandwich", "Tuna mayo with cucumber", 6.20m, ProductRoute.Kitchen, sandwiches, "/media/tuna-sandwich.svg", cancellationToken);
        

        await AddTableIfMissingAsync(db, "Table 1", 2, cancellationToken);
        await AddTableIfMissingAsync(db, "Table 2", 4, cancellationToken);
        await AddTableIfMissingAsync(db, "Table 3", 4, cancellationToken);
        await AddTableIfMissingAsync(db, "Terrace 1", 2, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ProductCategory> GetOrCreateCategoryAsync(
        AppDbContext db,
        string name,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var category = await db.ProductCategories.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (category is not null)
        {
            return category;
        }

        category = new ProductCategory { Name = name, SortOrder = sortOrder };
        db.ProductCategories.Add(category);
        return category;
    }

    private static async Task AddProductIfMissingAsync(
        AppDbContext db,
        string name,
        string description,
        decimal price,
        ProductRoute route,
        ProductCategory category,
        string photoUrl,
        CancellationToken cancellationToken)
    {
        var existingProduct = await db.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (existingProduct is not null)
        {
            EnsurePrimaryImage(existingProduct, photoUrl, name);
            return;
        }

        db.Products.Add(new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Route = route,
            Category = category,
            Images =
            [
                new ProductImage
                {
                    Url = photoUrl,
                    AltText = name,
                    IsPrimary = true
                }
            ]
        });
    }

    private static void EnsurePrimaryImage(Product product, string photoUrl, string altText)
    {
        var primaryImage = product.Images.FirstOrDefault(x => x.IsPrimary) ?? product.Images.FirstOrDefault();
        if (primaryImage is null)
        {
            product.Images.Add(new ProductImage
            {
                Url = photoUrl,
                AltText = altText,
                IsPrimary = true
            });
            return;
        }

        if (string.Equals(primaryImage.Url, "/placeholder-food.svg", StringComparison.OrdinalIgnoreCase))
        {
            primaryImage.Url = photoUrl;
            primaryImage.AltText = altText;
            primaryImage.IsPrimary = true;
        }
    }

    private static async Task AddTableIfMissingAsync(AppDbContext db, string name, int seats, CancellationToken cancellationToken)
    {
        if (await db.RestaurantTables.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return;
        }

        db.RestaurantTables.Add(new RestaurantTable { Name = name, Seats = seats });
    }
}
