using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(AppDbContext db, ProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductResponse>> List(CancellationToken cancellationToken)
    {
        var items = await db.Products
            .Include(x => x.Category)
            .Include(x => x.Images)
            .OrderBy(x => x.Category!.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await products.CreateProductAsync(request, cancellationToken);
        var loaded = await Load(product.Id, cancellationToken);
        return CreatedAtAction(nameof(List), ToResponse(loaded));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        await products.UpdateProductAsync(id, request, cancellationToken);
        return ToResponse(await Load(id, cancellationToken));
    }

    [HttpPut("{id:guid}/photo")]
    public async Task<ActionResult<ProductResponse>> SetPhoto(Guid id, ProductPhotoRequest request, CancellationToken cancellationToken)
    {
        await products.SetPhotoAsync(id, request, cancellationToken);
        return ToResponse(await Load(id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await products.DeactivateProductAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<Models.Product> Load(Guid id, CancellationToken cancellationToken)
    {
        return await db.Products.Include(x => x.Category).Include(x => x.Images).FirstAsync(x => x.Id == id, cancellationToken);
    }

    private static ProductResponse ToResponse(Models.Product product)
    {
        var image = product.MainImage;
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.IsActive,
            product.Route,
            product.CategoryId,
            product.Category?.Name,
            image?.Url,
            image?.AltText);
    }
}
