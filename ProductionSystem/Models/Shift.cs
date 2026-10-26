using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.Models;

// Рабочая смена, в которую выполняются заказы
public class Shift
{
    public int Id { get; set; }

    // Название смены: "Утренняя", "Дневная", "Ночная"
    [Required(ErrorMessage = "Название обязательно")]
    public string Name { get; set; } = string.Empty;

    // Время начала и окончания в формате ЧЧ:ММ
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Время в формате ЧЧ:ММ")]
    public string StartTime { get; set; } = "08:00";

    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Время в формате ЧЧ:ММ")]
    public string EndTime { get; set; } = "16:00";

    // Мастер смены
    [Required(ErrorMessage = "Укажите мастера смены")]
    public string Supervisor { get; set; } = string.Empty;
}
