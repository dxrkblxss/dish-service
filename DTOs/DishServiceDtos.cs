using DishService.Models;

namespace DishService.DTOs;

public record DishPriceOptionDto(string UnitOfMeasure, decimal UnitAmount, decimal Price, string? Label);
public record DishCreateDto(string Name, string? Description, SoldBy SoldBy, bool IsAvailable, string? ImageUrl, List<DishPriceOptionDto> PriceOptions);
public record DishUpdateDto(string? Name, string? Description, SoldBy? SoldBy, bool? IsAvailable, string? ImageUrl, List<DishPriceOptionDto>? PriceOptions);
