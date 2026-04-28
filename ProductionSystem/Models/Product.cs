namespace ProductionSystem.Models;

// Модель изделия — описывает продукт который производит завод
public class Product
{
    // Уникальный номер изделия (база данных заполняет сама)
    public int Id { get; set; }

    // Название изделия, например "Болт М8"
    public string Name { get; set; } = string.Empty;

    // Описание изделия
    public string Description { get; set; } = string.Empty;

    // Цена изделия
    public decimal Price { get; set; }
}