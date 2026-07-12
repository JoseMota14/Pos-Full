using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Tests.Integration;

public sealed class RestaurantFlowTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public RestaurantFlowTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProductOrderStatusReportAndCsvFlowWorksThroughApi()
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/categories", new CategoryRequest("Plates", 1));
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var productResponse = await _client.PostAsJsonAsync("/api/products", new ProductRequest(
            "Steak",
            null,
            12.5m,
            category!.Id,
            ProductRoute.Kitchen,
            "/media/steak.jpg",
            "Steak"));
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        var tableResponse = await _client.PostAsJsonAsync("/api/tables", new TableRequest("Table 1", 4));
        tableResponse.EnsureSuccessStatusCode();
        var table = await tableResponse.Content.ReadFromJsonAsync<TableResponse>();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            table!.Id,
            null,
            "Ana",
            [new CreateOrderItemRequest(product!.Id, 2, "Medium")]));
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        var item = order!.Items.Single();

        var statusResponse = await _client.PutAsJsonAsync(
            $"/api/orders/items/{item.Id}/status",
            new UpdateOrderItemStatusRequest(OrderItemStatus.BeingPrepared, null));
        statusResponse.EnsureSuccessStatusCode();
        var statusItem = await statusResponse.Content.ReadFromJsonAsync<OrderItemResponse>(JsonOptions);
        Assert.Equal(OrderItemStatus.BeingPrepared, statusItem!.Status);

        var account = await _client.GetFromJsonAsync<TableAccountResponse>($"/api/tables/{table.Id}/account", JsonOptions);
        Assert.Equal(25m, account!.Total);
        var accountJson = await _client.GetStringAsync($"/api/tables/{table.Id}/account");
        Assert.Contains("\"status\":\"Open\"", accountJson);
        Assert.Contains("\"status\":\"BeingPrepared\"", accountJson);
        Assert.Contains("\"waiterName\":\"Ana\"", accountJson);

        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddMinutes(5).ToString("O"));
        var report = await _client.GetFromJsonAsync<SalesReportRow[]>($"/api/reports/sales?from={from}&to={to}");
        Assert.Single(report!);
        Assert.Equal(2, report![0].QuantitySold);

        var csv = await _client.GetStringAsync($"/api/reports/sales/export.csv?from={from}&to={to}");
        Assert.Contains("Steak,Plates,2,25.00", csv);
    }
}

