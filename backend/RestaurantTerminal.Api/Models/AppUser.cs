namespace RestaurantTerminal.Api.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}
