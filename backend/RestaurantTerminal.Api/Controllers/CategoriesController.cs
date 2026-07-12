using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(AppDbContext db, ProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<CategoryResponse>> List(CancellationToken cancellationToken)
    {
        return await db.ProductCategories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryResponse(x.Id, x.Name, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await products.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new CategoryResponse(category.Id, category.Name, category.SortOrder, category.IsActive));
    }
}
