using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<TableAccount> TableAccounts => Set<TableAccount>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusEvent> OrderStatusEvents => Set<OrderStatusEvent>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<RestaurantTable>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(12, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(12, 2);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductImage>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TableAccount>()
            .HasOne(x => x.RestaurantTable)
            .WithMany(x => x.Accounts)
            .HasForeignKey(x => x.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.TableAccount)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.TableAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
