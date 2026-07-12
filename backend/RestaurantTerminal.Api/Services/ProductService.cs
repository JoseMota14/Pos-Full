using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Services;

public sealed class ProductService(AppDbContext db)
{
    public async Task<ProductCategory> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Category name is required.");
        }

        var category = new ProductCategory { Name = request.Name.Trim(), SortOrder = request.SortOrder };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Product> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Product name is required.");
        }

        if (request.Price < 0)
        {
            throw new InvalidOperationException("Product price cannot be negative.");
        }

        var categoryExists = await db.ProductCategories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive, cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("A valid category is required.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            Route = request.Route
        };

        if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            product.Images.Add(new ProductImage { Url = request.PhotoUrl.Trim(), AltText = request.PhotoAltText?.Trim() });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateProductAsync(Guid id, ProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Product was not found.");

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.Route = request.Route;

        if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            SetPrimaryPhoto(product, request.PhotoUrl, request.PhotoAltText);
        }

        await db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> SetPhotoAsync(Guid productId, ProductPhotoRequest request, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("Product was not found.");

        SetPrimaryPhoto(product, request.Url, request.AltText);
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task DeactivateProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException("Product was not found.");
        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void SetPrimaryPhoto(Product product, string url, string? altText)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Photo URL is required.");
        }

        foreach (var image in product.Images)
        {
            image.IsPrimary = false;
        }

        product.Images.Add(new ProductImage { Url = url.Trim(), AltText = altText?.Trim(), IsPrimary = true });
    }
}
