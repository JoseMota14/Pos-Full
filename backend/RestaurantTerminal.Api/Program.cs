using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Hubs;
using RestaurantTerminal.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("TerminalClients", policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("RestaurantTerminal")
        ?? "Data Source=restaurant-terminal.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

app.UseCors("TerminalClients");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapHub<OrdersHub>("/hubs/orders");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DatabaseInitializer.EnsureCompatibleAsync(db);
    await DemoDataSeeder.SeedAsync(db);
}

if (File.Exists(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")))
{
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;
