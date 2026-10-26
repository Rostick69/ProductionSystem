using Microsoft.EntityFrameworkCore;
using ProductionSystem.Models;

namespace ProductionSystem.Data;

// Контекст базы данных — связывает модели с таблицами PostgreSQL
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Shift> Shifts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Заказ -> изделие: при удалении изделия удаляются и его заказы
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Product)
            .WithMany()
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Заказ -> смена: при удалении смены у заказа просто сбрасывается ShiftId
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Shift)
            .WithMany()
            .HasForeignKey(o => o.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
