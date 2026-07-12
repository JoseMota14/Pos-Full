using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Contracts;

public sealed record TableRequest(string Name, int Seats);
public sealed record TableResponse(Guid Id, string Name, int Seats, bool IsActive);
public sealed record TableAccountResponse(
    Guid Id,
    Guid TableId,
    string TableName,
    TableAccountStatus Status,
    decimal Total,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<OrderResponse> Orders);
