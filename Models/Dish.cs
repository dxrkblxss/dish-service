using System.ComponentModel.DataAnnotations;

namespace DishService.Models;

public enum SoldBy
{
    Piece,
    Weight
}


public class Dish
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SoldBy SoldBy { get; set; }

    public bool IsAvailable { get; set; }

    public List<DishPriceOption> PriceOptions { get; set; } = [];

    public string? ImageUrl { get; set; }
}