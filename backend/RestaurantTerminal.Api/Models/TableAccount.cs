namespace RestaurantTerminal.Api.Models;

public sealed class TableAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantTableId { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }
    public TableAccountStatus Status { get; set; } = TableAccountStatus.Open;
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public List<Order> Orders { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    public decimal Total => Orders
        .SelectMany(x => x.Items)
        .Where(x => x.Status != OrderItemStatus.Cancelled)
        .Sum(x => x.Quantity * x.UnitPrice);
}
