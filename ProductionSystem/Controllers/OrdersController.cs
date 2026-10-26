using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers;

/// <summary>Заказы (CRUD). Заказ ссылается на изделие и смену. Список заказов кэшируется в Redis.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private const string ListKey = "orders_all";

    private readonly AppDbContext _context;
    private readonly CacheService _cache;

    public OrdersController(AppDbContext context, CacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>Список заказов вместе с изделием и сменой (кэш Redis, TTL 60 с)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Order>>> GetAll()
    {
        var orders = await _cache.GetOrSetAsync(ListKey, async () =>
            await _context.Orders.AsNoTracking()
                .Include(o => o.Product)
                .Include(o => o.Shift)
                .OrderBy(o => o.Id)
                .ToListAsync());
        return Ok(orders);
    }

    /// <summary>Заказ по идентификатору</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _context.Orders.AsNoTracking()
            .Include(o => o.Product)
            .Include(o => o.Shift)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound(new { message = $"Заказ {id} не найден" });
        return Ok(order);
    }

    /// <summary>Создать заказ</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Order>> Create(Order order)
    {
        string? error = await CheckReferences(order);
        if (error != null) return BadRequest(new { message = error });

        order.Id = 0;
        order.Product = null; // связанные объекты не создаём через заказ, только по id
        order.Shift = null;
        order.CreatedAt = DateTime.UtcNow;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Обновить заказ (например, сменить статус)</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Order order)
    {
        if (id != order.Id) return BadRequest(new { message = "Id в адресе и в теле запроса не совпадают" });

        var existing = await _context.Orders.FindAsync(id);
        if (existing == null) return NotFound(new { message = $"Заказ {id} не найден" });

        string? error = await CheckReferences(order);
        if (error != null) return BadRequest(new { message = error });

        existing.ProductId = order.ProductId;
        existing.ShiftId = order.ShiftId;
        existing.Quantity = order.Quantity;
        existing.Status = order.Status;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey);
        return NoContent();
    }

    /// <summary>Удалить заказ</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { message = $"Заказ {id} не найден" });

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ListKey);
        return NoContent();
    }

    // Проверка внешних ключей: без неё несуществующий ProductId дал бы ошибку 500 от базы
    private async Task<string?> CheckReferences(Order order)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == order.ProductId))
            return $"Изделие {order.ProductId} не существует";
        if (order.ShiftId.HasValue && !await _context.Shifts.AnyAsync(s => s.Id == order.ShiftId.Value))
            return $"Смена {order.ShiftId} не существует";
        return null;
    }
}
