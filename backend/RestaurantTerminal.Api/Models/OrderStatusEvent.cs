namespace RestaurantTerminal.Api.Models;

public sealed class OrderStatusEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public OrderItemStatus FromStatus { get; set; }
    public OrderItemStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ChangedByUserId { get; set; }
}
