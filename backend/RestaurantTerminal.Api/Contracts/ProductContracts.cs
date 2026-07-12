using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Contracts;

public sealed record CategoryRequest(string Name, int SortOrder = 0);
public sealed record CategoryResponse(Guid Id, string Name, int SortOrder, bool IsActive);

public sealed record ProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId,
    ProductRoute Route = ProductRoute.Kitchen,
    string? PhotoUrl = null,
    string? PhotoAltText = null);

public sealed record ProductPhotoRequest(string Url, string? AltText);
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool IsActive,
    ProductRoute Route,
    Guid CategoryId,
    string? CategoryName,
    string? PhotoUrl,
    string? PhotoAltText);
