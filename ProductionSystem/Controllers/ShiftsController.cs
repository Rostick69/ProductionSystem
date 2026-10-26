using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers;

/// <summary>Смены (CRUD). Чтение списка и записи по id кэшируется в Redis.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftsController : ControllerBase
{
    private const string ListKey = "shifts_all";
    private static string ItemKey(int id) => $"shift_{id}";

    private readonly AppDbContext _context;
    private readonly CacheService _cache;

    public ShiftsController(AppDbContext context, CacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>Список всех смен (кэш Redis, TTL 60 с)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Shift>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Shift>>> GetAll()
    {
        var shifts = await _cache.GetOrSetAsync(ListKey,
            async () => await _context.Shifts.AsNoTracking().OrderBy(p => p.Id).ToListAsync());
        return Ok(shifts);
    }

    /// <summary>Смена по идентификатору (кэш Redis, TTL 60 с)</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Shift), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Shift>> GetById(int id)
    {
        var shift = await _cache.GetOrSetAsync(ItemKey(id),
            async () => await _context.Shifts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id));
        if (shift == null) return NotFound(new { message = $"Смена {id} не найдена" });
        return Ok(shift);
    }

    /// <summary>Создать смену</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Shift), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Shift>> Create(Shift shift)
    {
        shift.Id = 0; // id назначает база данных
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey); // список изменился — сбрасываем кэш
        return CreatedAtAction(nameof(GetById), new { id = shift.Id }, shift);
    }

    /// <summary>Обновить смену</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Shift shift)
    {
        if (id != shift.Id) return BadRequest(new { message = "Id в адресе и в теле запроса не совпадают" });

        var existing = await _context.Shifts.FindAsync(id);
        if (existing == null) return NotFound(new { message = $"Смена {id} не найдена" });

        _context.Entry(existing).CurrentValues.SetValues(shift);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id), "orders_all");
        return NoContent();
    }

    /// <summary>Удалить смену (у её заказов смена сбрасывается)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var shift = await _context.Shifts.FindAsync(id);
        if (shift == null) return NotFound(new { message = $"Смена {id} не найдена" });

        _context.Shifts.Remove(shift);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey, ItemKey(id), "orders_all");
        return NoContent();
    }
}
