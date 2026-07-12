using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Controllers;
using RestaurantTerminal.Api.Models;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Tests.Unit;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreatesOrderAndCalculatesOpenTableAccountTotal()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Soups"));
        var soup = await products.CreateProductAsync(new ProductRequest("Soup", null, 3.25m, category.Id));
        var table = new RestaurantTable { Name = "Table 1", Seats = 2 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, "Ana", [new CreateOrderItemRequest(soup.Id, 2, "No onion")]));
        var account = await orders.GetOpenAccountAsync(table.Id);

        Assert.Equal(6.50m, account.Total);
        Assert.Equal("Ana", account.Orders.Single().WaiterName);
        Assert.Equal("No onion", account.Orders.Single().Items.Single().Notes);
    }

    [Fact]
    public async Task EnforcesOrderItemStatusTransitions()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Desserts"));
        var cake = await products.CreateProductAsync(new ProductRequest("Cake", null, 4m, category.Id));
        var table = new RestaurantTable { Name = "Table 2", Seats = 4 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        var order = await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(cake.Id, 1, null)]));
        var item = order.Items.Single();
        await orders.UpdateItemStatusAsync(item.Id, OrderItemStatus.BeingPrepared, null);
        await orders.UpdateItemStatusAsync(item.Id, OrderItemStatus.Ready, null);
        await orders.UpdateItemStatusAsync(item.Id, OrderItemStatus.Delivered, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orders.UpdateItemStatusAsync(item.Id, OrderItemStatus.Ready, null));
    }

    [Fact]
    public async Task KitchenQueueOrdersByCreatedAtWithSqliteDateTimeOffset()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Sandwiches"));
        var sandwich = await products.CreateProductAsync(new ProductRequest("Club Sandwich", null, 7.5m, category.Id));
        var table = new RestaurantTable { Name = "Table 4", Seats = 4 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        var later = await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(sandwich.Id, 1, null)]));
        var earlier = await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(sandwich.Id, 1, null)]));
        later.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(5);
        earlier.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        var queue = await orders.GetKitchenQueueAsync();

        Assert.Equal([earlier.Id, later.Id], queue.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task PaidAccountRemainsAvailableAsCurrentTableAccount()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Soups"));
        var soup = await products.CreateProductAsync(new ProductRequest("Chicken Soup", null, 4m, category.Id));
        var table = new RestaurantTable { Name = "Table 5", Seats = 2 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(soup.Id, 1, null)]));
        await orders.PayAccountAsync(table.Id);

        var account = await orders.GetCurrentAccountAsync(table.Id);

        Assert.Equal(TableAccountStatus.Paid, account.Status);
        Assert.Equal(4m, account.Total);
    }

    [Fact]
    public async Task CompletingOrderMarksItemsDeliveredAndRemovesItFromKitchenQueue()
    {
        var (db, connection) = await TestDb.CreateAsync();
        await using var _ = db;
        await using var __ = connection;
        var products = new ProductService(db);
        var orders = new OrderService(db);
        var category = await products.CreateCategoryAsync(new CategoryRequest("Soups"));
        var soup = await products.CreateProductAsync(new ProductRequest("Chicken Soup", null, 4m, category.Id));
        var table = new RestaurantTable { Name = "Table 6", Seats = 2 };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync();

        var order = await orders.CreateOrderAsync(new CreateOrderRequest(table.Id, null, null, [new CreateOrderItemRequest(soup.Id, 1, null)]));

        await orders.CompleteOrderAsync(order.Id, null);
        var queue = await orders.GetKitchenQueueAsync();

        Assert.DoesNotContain(queue, x => x.Id == order.Id);
        Assert.All(order.Items, item => Assert.Equal(OrderItemStatus.Delivered, item.Status));
    }

    [Fact]
    public void OrderResponseSortsDrinksLast()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            WaiterName = "Ana",
            Items =
            [
                new OrderItem { Id = Guid.NewGuid(), ProductName = "Orange Juice", CategoryName = "Drinks", Quantity = 1 },
                new OrderItem { Id = Guid.NewGuid(), ProductName = "Chicken Soup", CategoryName = "Soups", Quantity = 1 }
            ]
        };

        var response = OrdersController.ToResponse(order);

        Assert.Equal(["Chicken Soup", "Orange Juice"], response.Items.Select(x => x.ProductName).ToArray());
        Assert.Equal("Ana", response.WaiterName);
    }
}

