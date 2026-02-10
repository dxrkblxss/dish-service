namespace DishService.Models;

public class DishPriceOption
{
    public Guid Id { get; set; }

    public Guid DishId { get; set; }

    public string Label { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal UnitAmount { get; set; }

    public decimal Price { get; set; }
}