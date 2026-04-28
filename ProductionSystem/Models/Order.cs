namespace ProductionSystem.Models;

// Модель заказа — описывает что и в каком количестве заказал клиент
public class Order
{
    // Уникальный номер заказа (база данных заполняет сама)
    public int Id { get; set; }

    // Ссылка на изделие — какое именно изделие заказали
    public int ProductId { get; set; }

    // Само изделие (Entity Framework заполняет автоматически)
    public Product? Product { get; set; }

    // Количество изделий в заказе
    public int Quantity { get; set; }

    // Статус заказа: "Новый", "В работе", "Готов"
    public string Status { get; set; } = "Новый";

    // Дата создания заказа (заполняется автоматически)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}