using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ProductionSystem.Data;
using ProductionSystem.Models;
using System.Text.Json;

namespace ProductionSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    // Redis кэш
    private readonly IDistributedCache _cache;

    public ProductsController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET /api/products — сначала ищем в кэше, потом в базе
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        // Пробуем взять из кэша
        var cached = await _cache.GetStringAsync("products_all");
        if (cached != null)
            return Ok(JsonSerializer.Deserialize<List<Product>>(cached));

        // Если в кэше нет — берём из базы и сохраняем в кэш на 60 секунд
        var products = await _context.Products.ToListAsync();
        await _cache.SetStringAsync("products_all",
            JsonSerializer.Serialize(products),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            });
        return Ok(products);
    }

    // GET /api/products/1 — получить изделие по номеру (тоже с кэшем)
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var cached = await _cache.GetStringAsync($"product_{id}");
        if (cached != null)
            return Ok(JsonSerializer.Deserialize<Product>(cached));

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        await _cache.SetStringAsync($"product_{id}",
            JsonSerializer.Serialize(product),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            });
        return Ok(product);
    }

    // POST /api/products — добавить новое изделие
    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        // Сбрасываем кэш списка после добавления
        await _cache.RemoveAsync("products_all");
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/1 — обновить изделие
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id) return BadRequest();
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        // Сбрасываем кэш после обновления
        await _cache.RemoveAsync("products_all");
        await _cache.RemoveAsync($"product_{id}");
        return NoContent();
    }

    // DELETE /api/products/1 — удалить изделие
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        // Сбрасываем кэш после удаления
        await _cache.RemoveAsync("products_all");
        await _cache.RemoveAsync($"product_{id}");
        return NoContent();
    }
}