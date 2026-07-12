namespace RestaurantTerminal.Api.Models;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public ProductRoute Route { get; set; } = ProductRoute.Kitchen;
    public Guid CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public List<ProductImage> Images { get; set; } = [];

    public ProductImage? MainImage => Images
        .OrderByDescending(x => x.IsPrimary)
        .ThenBy(x => x.CreatedAt)
        .FirstOrDefault();
}
