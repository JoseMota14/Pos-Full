namespace RestaurantTerminal.Api.Models;

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Ordered;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderStatusEvent> StatusEvents { get; set; } = [];
}
