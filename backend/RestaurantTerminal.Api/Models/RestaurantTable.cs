namespace RestaurantTerminal.Api.Models;

public sealed class RestaurantTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Seats { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TableAccount> Accounts { get; set; } = [];
}
