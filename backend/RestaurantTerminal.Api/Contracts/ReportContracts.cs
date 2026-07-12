namespace RestaurantTerminal.Api.Contracts;

public sealed record SalesReportRow(
    Guid ProductId,
    string ProductName,
    string? Category,
    int QuantitySold,
    decimal TotalRevenue);
