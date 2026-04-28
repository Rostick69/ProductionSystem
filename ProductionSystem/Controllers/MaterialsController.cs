using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ProductionSystem.Data;
using ProductionSystem.Models;
using System.Text.Json;

namespace ProductionSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly AppDbContext _context;
    // Redis кэш
    private readonly IDistributedCache _cache;

    public MaterialsController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET /api/materials — сначала ищем в кэше, потом в базе
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Material>>> GetAll()
    {
        var cached = await _cache.GetStringAsync("materials_all");
        if (cached != null)
            return Ok(JsonSerializer.Deserialize<List<Material>>(cached));

        var materials = await _context.Materials.ToListAsync();
        await _cache.SetStringAsync("materials_all",
            JsonSerializer.Serialize(materials),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            });
        return Ok(materials);
    }

    // GET /api/materials/1 — получить материал по номеру (тоже с кэшем)
    [HttpGet("{id}")]
    public async Task<ActionResult<Material>> GetById(int id)
    {
        var cached = await _cache.GetStringAsync($"material_{id}");
        if (cached != null)
            return Ok(JsonSerializer.Deserialize<Material>(cached));

        var material = await _context.Materials.FindAsync(id);
        if (material == null) return NotFound();

        await _cache.SetStringAsync($"material_{id}",
            JsonSerializer.Serialize(material),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            });
        return Ok(material);
    }

    // POST /api/materials — добавить новый материал
    [HttpPost]
    public async Task<ActionResult<Material>> Create(Material material)
    {
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync("materials_all");
        return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
    }

    // PUT /api/materials/1 — обновить материал
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Material material)
    {
        if (id != material.Id) return BadRequest();
        _context.Entry(material).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync("materials_all");
        await _cache.RemoveAsync($"material_{id}");
        return NoContent();
    }

    // DELETE /api/materials/1 — удалить материал
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return NotFound();
        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync("materials_all");
        await _cache.RemoveAsync($"material_{id}");
        return NoContent();
    }
}