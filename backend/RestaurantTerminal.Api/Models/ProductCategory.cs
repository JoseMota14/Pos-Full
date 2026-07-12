namespace RestaurantTerminal.Api.Models;

public sealed class ProductCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = [];
}
