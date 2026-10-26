using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProductionSystem.Controllers;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;
using Xunit;

namespace ProductionSystem.Tests;

// Тесты контроллеров без Docker: вместо PostgreSQL — база в памяти, вместо Redis — кэш в памяти
public class ControllerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // у каждого теста своя пустая база
            .Options;
        return new AppDbContext(options);
    }

    private static (CacheService service, IDistributedCache raw) CreateCache()
    {
        var raw = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return (new CacheService(raw, NullLogger<CacheService>.Instance), raw);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreated_AndCanBeRead()
    {
        using var db = CreateDb();
        var (cache, _) = CreateCache();
        var controller = new ProductsController(db, cache);

        var result = await controller.Create(new Product { Name = "Болт М8", Description = "тест", Price = 10 });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<Product>(created.Value);
        Assert.True(product.Id > 0);

        var get = await controller.GetById(product.Id);
        var ok = Assert.IsType<OkObjectResult>(get.Result);
        Assert.Equal("Болт М8", Assert.IsType<Product>(ok.Value).Name);
    }

    [Fact]
    public async Task GetProduct_Unknown_Returns404()
    {
        using var db = CreateDb();
        var controller = new ProductsController(db, CreateCache().service);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_PutsListIntoCache_AndCreateInvalidatesIt()
    {
        using var db = CreateDb();
        var (cache, raw) = CreateCache();
        var controller = new ProductsController(db, cache);
        db.Products.Add(new Product { Name = "Гайка", Description = "", Price = 1 });
        await db.SaveChangesAsync();

        await controller.GetAll();
        Assert.NotNull(await raw.GetStringAsync("products_all")); // после чтения список в кэше

        await controller.Create(new Product { Name = "Шайба", Description = "", Price = 1 });
        Assert.Null(await raw.GetStringAsync("products_all"));    // после изменения кэш сброшен
    }

    [Fact]
    public async Task UpdateProduct_Unknown_Returns404()
    {
        using var db = CreateDb();
        var controller = new ProductsController(db, CreateCache().service);

        var result = await controller.Update(5, new Product { Id = 5, Name = "Нет такого", Description = "" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrder_WithUnknownProduct_Returns400()
    {
        using var db = CreateDb();
        var controller = new OrdersController(db, CreateCache().service);

        var result = await controller.Create(new Order { ProductId = 42, Quantity = 1 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteShift_RemovesIt()
    {
        using var db = CreateDb();
        var controller = new ShiftsController(db, CreateCache().service);
        db.Shifts.Add(new Shift { Name = "Ночная", StartTime = "22:00", EndTime = "06:00", Supervisor = "Сидоров" });
        await db.SaveChangesAsync();
        int id = db.Shifts.Single().Id;

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Shifts);
    }
}
