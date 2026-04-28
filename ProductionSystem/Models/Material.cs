namespace ProductionSystem.Models;

// Модель материала — описывает сырьё которое используется для производства
public class Material
{
    // Уникальный номер материала (база данных заполняет сама)
    public int Id { get; set; }

    // Название материала, например "Сталь листовая"
    public string Name { get; set; } = string.Empty;

    // Единица измерения, например "кг", "м", "шт"
    public string Unit { get; set; } = string.Empty;

    // Количество материала на складе
    public double Quantity { get; set; }
}