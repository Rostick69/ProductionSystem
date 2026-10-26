using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers;

/// <summary>Изделия (CRUD). Чтение списка и записи по id кэшируется в Redis.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private const string ListKey = "products_all";
    private static string ItemKey(int id) => $"product_{id}";

    private readonly AppDbContext _context;
    private readonly CacheService _cache;

    public ProductsController(AppDbContext context, CacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>Список всех изделий (кэш Redis, TTL 60 с)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _cache.GetOrSetAsync(ListKey,
            async () => await _context.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());
        return Ok(products);
    }

    /// <summary>Изделие по идентификатору (кэш Redis, TTL 60 с)</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _cache.GetOrSetAsync(ItemKey(id),
            async () => await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id));
        if (product == null) return NotFound(new { message = $"Изделие {id} не найдено" });
        return Ok(product);
    }

    /// <summary>Создать изделие</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        product.Id = 0; // id назначает база данных
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey); // список изменился — сбрасываем кэш
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Обновить изделие</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id) return BadRequest(new { message = "Id в адресе и в теле запроса не совпадают" });

        var existing = await _context.Products.FindAsync(id);
        if (existing == null) return NotFound(new { message = $"Изделие {id} не найдено" });

        _context.Entry(existing).CurrentValues.SetValues(product);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id), "orders_all");
        return NoContent();
    }

    /// <summary>Удалить изделие (вместе с его заказами)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { message = $"Изделие {id} не найдено" });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id), "orders_all");
        return NoContent();
    }
}
