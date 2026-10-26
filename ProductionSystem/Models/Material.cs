using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.Models;

// Материал — сырьё на складе, из которого делают изделия
public class Material
{
    public int Id { get; set; }

    // Название материала, например "Сталь листовая"
    [Required(ErrorMessage = "Название обязательно")]
    [MinLength(2, ErrorMessage = "Название слишком короткое")]
    public string Name { get; set; } = string.Empty;

    // Единица измерения: "кг", "м", "шт"
    [Required(ErrorMessage = "Единица измерения обязательна")]
    public string Unit { get; set; } = string.Empty;

    // Остаток на складе
    [Range(0, double.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
    public double Quantity { get; set; }
}
