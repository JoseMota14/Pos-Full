namespace RestaurantTerminal.Api.Models;

public enum OrderItemStatus
{
    Ordered,
    BeingPrepared,
    Ready,
    Delivered,
    Cancelled
}

public enum TableAccountStatus
{
    Open,
    Closed,
    Paid,
    Cancelled
}

public enum ProductRoute
{
    Kitchen,
    Bar,
    None
}
