using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.Models;

// Изделие — продукт, который выпускает предприятие
public class Product
{
    // Первичный ключ (база данных заполняет сама)
    public int Id { get; set; }

    // Название изделия, например "Болт М8"
    [Required(ErrorMessage = "Название обязательно")]
    [MinLength(2, ErrorMessage = "Название слишком короткое")]
    public string Name { get; set; } = string.Empty;

    // Описание изделия
    public string Description { get; set; } = string.Empty;

    // Цена за единицу, руб.
    [Range(0, 10_000_000, ErrorMessage = "Цена должна быть от 0 до 10 000 000")]
    public decimal Price { get; set; }
}
