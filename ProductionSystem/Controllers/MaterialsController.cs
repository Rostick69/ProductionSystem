using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;

namespace ProductionSystem.Controllers;

// Контроллер для материалов — обрабатывает все запросы по адресу /api/materials
[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    // Контекст базы данных
    private readonly AppDbContext _context;

    public MaterialsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/materials — получить все материалы
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Material>>> GetAll()
    {
        return await _context.Materials.ToListAsync();
    }

    // GET /api/materials/1 — получить материал по номеру
    [HttpGet("{id}")]
    public async Task<ActionResult<Material>> GetById(int id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return NotFound();
        return material;
    }

    // POST /api/materials — добавить новый материал
    [HttpPost]
    public async Task<ActionResult<Material>> Create(Material material)
    {
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
    }

    // PUT /api/materials/1 — обновить материал
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Material material)
    {
        if (id != material.Id) return BadRequest();
        _context.Entry(material).State = EntityState.Modified;
        await _context.SaveChangesAsync();
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
        return NoContent();
    }
}