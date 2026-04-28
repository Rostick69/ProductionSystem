using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;

namespace ProductionSystem.Controllers;

// Контроллер для заказов — обрабатывает все запросы по адресу /api/orders
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // Контекст базы данных
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/orders — получить все заказы (вместе с данными изделия)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
    {
        return await _context.Orders
            .Include(o => o.Product)
            .ToListAsync();
    }

    // GET /api/orders/1 — получить заказ по номеру
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return order;
    }

    // POST /api/orders — создать новый заказ
    [HttpPost]
    public async Task<ActionResult<Order>> Create(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // PUT /api/orders/1 — обновить заказ
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Order order)
    {
        if (id != order.Id) return BadRequest();
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/orders/1 — удалить заказ
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}