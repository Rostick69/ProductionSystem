namespace ProductionClient;

// Изделие — такая же структура, как в API
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public override string ToString() => $"#{Id} {Name} — {Price} руб. ({Description})";
}
