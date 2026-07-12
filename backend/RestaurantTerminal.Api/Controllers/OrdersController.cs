using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Hubs;
using RestaurantTerminal.Api.Models;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    OrderService orders,
    IHubContext<KitchenHub> kitchenHub,
    IHubContext<OrdersHub> ordersHub) : ControllerBase
{
    [HttpGet("kitchen-queue")]
    public async Task<IReadOnlyList<OrderResponse>> KitchenQueue(CancellationToken cancellationToken)
    {
        var queue = await orders.GetKitchenQueueAsync(cancellationToken);
        return queue.Select(ToResponse).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.CreateOrderAsync(request, cancellationToken);
        var response = ToResponse(order);
        await kitchenHub.Clients.All.SendAsync("KitchenOrderCreated", response, cancellationToken);
        await ordersHub.Clients.All.SendAsync("OrderCreated", response, cancellationToken);
        return CreatedAtAction(nameof(KitchenQueue), response);
    }

    [HttpPut("items/{itemId:guid}/status")]
    public async Task<ActionResult<OrderItemResponse>> UpdateItemStatus(
        Guid itemId,
        UpdateOrderItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var item = await orders.UpdateItemStatusAsync(itemId, request.Status, request.UserId, cancellationToken);
        var response = ToResponse(item);
        await kitchenHub.Clients.All.SendAsync("OrderItemStatusChanged", response, cancellationToken);
        await ordersHub.Clients.All.SendAsync("OrderItemStatusChanged", response, cancellationToken);
        return response;
    }

    [HttpPost("{orderId:guid}/complete")]
    public async Task<ActionResult<OrderResponse>> Complete(
        Guid orderId,
        CompleteOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await orders.CompleteOrderAsync(orderId, request.UserId, cancellationToken);
        var response = ToResponse(order);
        await kitchenHub.Clients.All.SendAsync("OrderCompleted", orderId, cancellationToken);
        await ordersHub.Clients.All.SendAsync("OrderCompleted", response, cancellationToken);
        return response;
    }

    public static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.TableAccountId,
            order.TableAccount?.RestaurantTableId ?? Guid.Empty,
            order.TableAccount?.RestaurantTable?.Name ?? string.Empty,
            order.WaiterName,
            order.CreatedAt,
            order.Items
                .OrderBy(CategorySort)
                .ThenBy(x => x.CategoryName)
                .ThenBy(x => x.ProductName)
                .Select(ToResponse)
                .ToList());
    }

    public static OrderItemResponse ToResponse(OrderItem item)
    {
        return new OrderItemResponse(
            item.Id,
            item.OrderId,
            item.ProductId,
            item.ProductName,
            item.CategoryName,
            item.Quantity,
            item.UnitPrice,
            item.Notes,
            item.Status,
            item.CreatedAt);
    }

    private static int CategorySort(OrderItem item)
    {
        return string.Equals(item.CategoryName, "Drinks", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }
}
