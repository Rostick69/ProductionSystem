using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers;

/// <summary>Материалы (CRUD). Чтение списка и записи по id кэшируется в Redis.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MaterialsController : ControllerBase
{
    private const string ListKey = "materials_all";
    private static string ItemKey(int id) => $"material_{id}";

    private readonly AppDbContext _context;
    private readonly CacheService _cache;

    public MaterialsController(AppDbContext context, CacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>Список всех материалов (кэш Redis, TTL 60 с)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Material>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Material>>> GetAll()
    {
        var materials = await _cache.GetOrSetAsync(ListKey,
            async () => await _context.Materials.AsNoTracking().OrderBy(p => p.Id).ToListAsync());
        return Ok(materials);
    }

    /// <summary>Материал по идентификатору (кэш Redis, TTL 60 с)</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Material), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Material>> GetById(int id)
    {
        var material = await _cache.GetOrSetAsync(ItemKey(id),
            async () => await _context.Materials.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id));
        if (material == null) return NotFound(new { message = $"Материал {id} не найден" });
        return Ok(material);
    }

    /// <summary>Создать материал</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Material), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Material>> Create(Material material)
    {
        material.Id = 0; // id назначает база данных
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey); // список изменился — сбрасываем кэш
        return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
    }

    /// <summary>Обновить материал</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Material material)
    {
        if (id != material.Id) return BadRequest(new { message = "Id в адресе и в теле запроса не совпадают" });

        var existing = await _context.Materials.FindAsync(id);
        if (existing == null) return NotFound(new { message = $"Материал {id} не найден" });

        _context.Entry(existing).CurrentValues.SetValues(material);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id));
        return NoContent();
    }

    /// <summary>Удалить материал</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return NotFound(new { message = $"Материал {id} не найден" });

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id));
        return NoContent();
    }
}
