namespace RestaurantTerminal.Api.Models;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableAccountId { get; set; }
    public TableAccount? TableAccount { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
}
