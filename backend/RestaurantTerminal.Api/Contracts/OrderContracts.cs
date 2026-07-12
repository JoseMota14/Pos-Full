using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Contracts;

public sealed record CreateOrderRequest(Guid TableId, Guid? WaiterId, string? WaiterName, IReadOnlyList<CreateOrderItemRequest> Items);
public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity, string? Notes);
public sealed record UpdateOrderItemStatusRequest(OrderItemStatus Status, Guid? UserId);
public sealed record CompleteOrderRequest(Guid? UserId);

public sealed record OrderResponse(
    Guid Id,
    Guid TableAccountId,
    Guid TableId,
    string TableName,
    string? WaiterName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid OrderId,
    Guid ProductId,
    string ProductName,
    string? CategoryName,
    int Quantity,
    decimal UnitPrice,
    string? Notes,
    OrderItemStatus Status,
    DateTimeOffset CreatedAt);
