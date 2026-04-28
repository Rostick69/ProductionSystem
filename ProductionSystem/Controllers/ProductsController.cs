using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;

namespace ProductionSystem.Controllers;

// Контроллер для изделий — обрабатывает все запросы по адресу /api/products
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Контекст базы данных
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/products — получить все изделия
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
    }

    // GET /api/products/1 — получить изделие по номеру
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return product;
    }

    // POST /api/products — добавить новое изделие
    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/1 — обновить изделие
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id) return BadRequest();
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
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
        return NoContent();
    }
}