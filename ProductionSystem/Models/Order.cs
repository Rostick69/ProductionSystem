using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.Models;

// Заказ на изготовление изделия. Связан с изделием (обязательно) и сменой (необязательно)
public class Order
{
    public int Id { get; set; }

    // Внешний ключ на изделие
    [Range(1, int.MaxValue, ErrorMessage = "Укажите изделие")]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Внешний ключ на смену (может быть не назначена)
    public int? ShiftId { get; set; }
    public Shift? Shift { get; set; }

    // Количество изделий в заказе
    [Range(1, 1_000_000, ErrorMessage = "Количество должно быть от 1 до 1 000 000")]
    public int Quantity { get; set; }

    // Статус заказа: "Новый", "В работе", "Готов"
    [Required]
    public string Status { get; set; } = "Новый";

    // Дата создания (UTC)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
