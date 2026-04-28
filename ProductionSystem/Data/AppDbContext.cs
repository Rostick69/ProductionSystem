using Microsoft.EntityFrameworkCore;
using ProductionSystem.Models;

namespace ProductionSystem.Data;

// Контекст базы данных — связывает наши модели с реальными таблицами в PostgreSQL
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Таблица изделий
    public DbSet<Product> Products { get; set; }

    // Таблица материалов
    public DbSet<Material> Materials { get; set; }

    // Таблица заказов
    public DbSet<Order> Orders { get; set; }
}