namespace RestaurantTerminal.Api.Models;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableAccountId { get; set; }
    public TableAccount? TableAccount { get; set; }
    public Guid? WaiterId { get; set; }
    public AppUser? Waiter { get; set; }
    public string? WaiterName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}
