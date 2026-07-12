using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Services;

public sealed class OrderService(AppDbContext db)
{
    private static readonly Dictionary<OrderItemStatus, OrderItemStatus[]> AllowedTransitions = new()
    {
        [OrderItemStatus.Ordered] = [OrderItemStatus.BeingPrepared, OrderItemStatus.Ready, OrderItemStatus.Cancelled],
        [OrderItemStatus.BeingPrepared] = [OrderItemStatus.Ready, OrderItemStatus.Cancelled],
        [OrderItemStatus.Ready] = [OrderItemStatus.Delivered, OrderItemStatus.Cancelled],
        [OrderItemStatus.Delivered] = [],
        [OrderItemStatus.Cancelled] = []
    };

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        var table = await db.RestaurantTables.FirstOrDefaultAsync(x => x.Id == request.TableId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("A valid table is required.");

        var account = await db.TableAccounts
            .Include(x => x.Orders)
            .ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.RestaurantTableId == table.Id && x.Status == TableAccountStatus.Open, cancellationToken);

        if (account is null)
        {
            account = new TableAccount { RestaurantTableId = table.Id };
            db.TableAccounts.Add(account);
        }

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products
            .Include(x => x.Category)
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var order = new Order
        {
            TableAccount = account,
            WaiterId = request.WaiterId,
            WaiterName = string.IsNullOrWhiteSpace(request.WaiterName) ? null : request.WaiterName.Trim()
        };
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            }

            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new InvalidOperationException("Order contains an inactive or unknown product.");
            }

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = product.Category?.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                Notes = item.Notes?.Trim()
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderItem> UpdateItemStatusAsync(Guid itemId, OrderItemStatus status, Guid? userId, CancellationToken cancellationToken = default)
    {
        var item = await db.OrderItems.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Order item was not found.");

        if (item.Status == status)
        {
            return item;
        }

        if (!AllowedTransitions[item.Status].Contains(status))
        {
            throw new InvalidOperationException($"Cannot move item from {item.Status} to {status}.");
        }

        var previous = item.Status;
        item.Status = status;
        db.OrderStatusEvents.Add(new OrderStatusEvent
        {
            OrderItemId = item.Id,
            FromStatus = previous,
            ToStatus = status,
            ChangedByUserId = userId
        });

        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Order> CompleteOrderAsync(Guid orderId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders
            .Include(x => x.TableAccount)!.ThenInclude(x => x!.RestaurantTable)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");

        foreach (var item in order.Items.Where(x => x.Status != OrderItemStatus.Delivered && x.Status != OrderItemStatus.Cancelled))
        {
            var previous = item.Status;
            item.Status = OrderItemStatus.Delivered;
            db.OrderStatusEvents.Add(new OrderStatusEvent
            {
                OrderItemId = item.Id,
                FromStatus = previous,
                ToStatus = OrderItemStatus.Delivered,
                ChangedByUserId = userId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<TableAccount> GetOpenAccountAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        return await db.TableAccounts
            .Include(x => x.RestaurantTable)
            .Include(x => x.Orders)
            .ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.RestaurantTableId == tableId && x.Status == TableAccountStatus.Open, cancellationToken)
            ?? throw new KeyNotFoundException("Table does not have an open account.");
    }

    public async Task<TableAccount> GetCurrentAccountAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var accounts = await db.TableAccounts
            .Include(x => x.RestaurantTable)
            .Include(x => x.Orders)
            .ThenInclude(x => x.Items)
            .Where(x => x.RestaurantTableId == tableId)
            .ToListAsync(cancellationToken);

        return accounts
            .OrderBy(x => x.Status == TableAccountStatus.Open ? 0 : 1)
            .ThenByDescending(x => x.OpenedAt)
            .FirstOrDefault()
            ?? throw new KeyNotFoundException("Table does not have an account.");
    }

    public async Task<TableAccount> CloseAccountAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var account = await GetOpenAccountAsync(tableId, cancellationToken);
        account.Status = TableAccountStatus.Closed;
        account.ClosedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<TableAccount> PayAccountAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var account = await GetOpenAccountAsync(tableId, cancellationToken);
        account.Status = TableAccountStatus.Paid;
        account.ClosedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<IReadOnlyList<Order>> GetKitchenQueueAsync(CancellationToken cancellationToken = default)
    {
        var queue = await db.Orders
            .Include(x => x.TableAccount)!.ThenInclude(x => x!.RestaurantTable)
            .Include(x => x.Items.Where(i => i.Status != OrderItemStatus.Delivered && i.Status != OrderItemStatus.Cancelled))
            .Where(x => x.Items.Any(i => i.Status != OrderItemStatus.Delivered && i.Status != OrderItemStatus.Cancelled))
            .ToListAsync(cancellationToken);

        return queue.OrderBy(x => x.CreatedAt).ToList();
    }

    private async Task<Order> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await db.Orders
            .Include(x => x.TableAccount)!.ThenInclude(x => x!.RestaurantTable)
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == orderId, cancellationToken);
    }
}
