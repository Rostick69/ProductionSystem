using Microsoft.EntityFrameworkCore;
using ProductionSystem.Models;

namespace ProductionSystem.Data;

// Применение миграций и заполнение базы тестовыми данными при первом запуске
public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // PostgreSQL в контейнере может стартовать не мгновенно — делаем несколько попыток
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning("База данных недоступна (попытка {Attempt}/10): {Message}", attempt, ex.Message);
                await Task.Delay(3000);
            }
        }

        // Seeding: только если таблицы пустые
        if (await db.Products.AnyAsync())
            return;

        var shifts = new[]
        {
            new Shift { Name = "Утренняя", StartTime = "06:00", EndTime = "14:00", Supervisor = "Иванов И. И." },
            new Shift { Name = "Дневная", StartTime = "14:00", EndTime = "22:00", Supervisor = "Петров П. П." },
            new Shift { Name = "Ночная", StartTime = "22:00", EndTime = "06:00", Supervisor = "Сидоров С. С." }
        };
        var products = new[]
        {
            new Product { Name = "Болт М8×40", Description = "Болт с шестигранной головкой, сталь 45", Price = 12.50m },
            new Product { Name = "Гайка М8", Description = "Гайка шестигранная, класс прочности 8", Price = 4.20m },
            new Product { Name = "Кронштейн КР-1", Description = "Кронштейн настенный, сталь 3 мм", Price = 310m },
            new Product { Name = "Вал приводной ВП-25", Description = "Вал Ø25 мм, длина 500 мм", Price = 1850m }
        };
        var materials = new[]
        {
            new Material { Name = "Сталь 45 (пруток Ø10)", Unit = "м", Quantity = 1200 },
            new Material { Name = "Сталь листовая 3 мм", Unit = "кг", Quantity = 850.5 },
            new Material { Name = "Краска порошковая RAL 9005", Unit = "кг", Quantity = 64 },
            new Material { Name = "Упаковка (коробка №3)", Unit = "шт", Quantity = 500 }
        };

        db.Shifts.AddRange(shifts);
        db.Products.AddRange(products);
        db.Materials.AddRange(materials);
        await db.SaveChangesAsync();

        db.Orders.AddRange(
            new Order { ProductId = products[0].Id, ShiftId = shifts[0].Id, Quantity = 5000, Status = "В работе" },
            new Order { ProductId = products[1].Id, ShiftId = shifts[0].Id, Quantity = 5000, Status = "Новый" },
            new Order { ProductId = products[2].Id, ShiftId = shifts[1].Id, Quantity = 120, Status = "Готов" },
            new Order { ProductId = products[3].Id, ShiftId = null, Quantity = 15, Status = "Новый" });
        await db.SaveChangesAsync();

        logger.LogInformation("База данных заполнена тестовыми данными");
    }
}
