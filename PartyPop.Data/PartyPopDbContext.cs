using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;

namespace PartyPop.Data;

public class PartyPopDbContext : DbContext
{
    public PartyPopDbContext(DbContextOptions<PartyPopDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductDayUsage> ProductDayUsages => Set<ProductDayUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // טוען את כל קבצי IEntityTypeConfiguration מתיקיית Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartyPopDbContext).Assembly);
    }
}
