using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using DishService.Data;
using DishService.Models;
using DishService.Middleware;
using DishService.Extensions;

namespace DishService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = false;
            options.TimestampFormat = "[HH:mm:ss]";
        });
        builder.Logging.AddDebug();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not set");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        builder.Services.AddStackExchangeRedisCache(
            options => options.Configuration = builder.Configuration.GetConnectionString("Redis"));

        var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

        const string AllDishesKey = "dishes:all";
        static string DishKey(Guid id) => $"dishes:{id}";

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dishes Service API", Version = "v1" });
        });

        var app = builder.Build();

        app.UseForwardedHeaders();

        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                var fallbackPrefix = app.Configuration["Swagger:BasePath"]
                    ?? Environment.GetEnvironmentVariable("SWAGGER_BASEPATH")
                    ?? string.Empty;

                var prefix = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault();
                if (string.IsNullOrEmpty(prefix))
                    prefix = fallbackPrefix;

                var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
                var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;

                var baseUrl = $"{scheme}://{host}{prefix}";
                swaggerDoc.Servers =
                [
                    new() { Url = baseUrl }
                ];
            });
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            c.RoutePrefix = "swagger";
        });

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors("AllowAll");

        app.MapGet("/health", (HttpContext ctx) => Results.Ok(new { correlation_id = ctx.GetCorrelationId() }));

        app.MapGet("/", async (AppDbContext db, IDistributedCache cache, HttpContext ctx) =>
        {
            var correlationId = ctx.GetCorrelationId();

            var cachedDishes = await cache.GetStringAsync(AllDishesKey);
            if (!string.IsNullOrEmpty(cachedDishes))
            {
                var cached = JsonSerializer.Deserialize<object>(cachedDishes);
                return Results.Ok(new { data = cached, correlation_id = correlationId });
            }

            var dishes = await db.Dishes
                .Include(d => d.PriceOptions)
                .ToListAsync();
            var jsonData = JsonSerializer.Serialize(dishes);

            await cache.SetStringAsync(AllDishesKey, jsonData, cacheOptions);

            return Results.Ok(new { data = dishes, correlation_id = correlationId });
        });

        app.MapGet("/{id}", async (Guid id, AppDbContext db, IDistributedCache cache, HttpContext ctx) =>
        {
            var correlationId = ctx.GetCorrelationId();

            var cachedDish = await cache.GetStringAsync(DishKey(id));

            if (!string.IsNullOrEmpty(cachedDish))
            {
                var cached = JsonSerializer.Deserialize<object>(cachedDish);
                return Results.Ok(new { data = cached, correlation_id = correlationId });
            }

            var dish = await db.Dishes
                .Include(d => d.PriceOptions)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dish is null)
                return Results.NotFound();

            var jsonData = JsonSerializer.Serialize(dish);

            await cache.SetStringAsync(DishKey(id), jsonData, cacheOptions);

            return Results.Ok(new { data = dish, correlation_id = correlationId });
        });

        app.MapPost("/", async (DishCreateDto dto, AppDbContext db, IDistributedCache cache, HttpContext ctx) =>
        {
            var correlationId = ctx.GetCorrelationId();
            var dish = new Dish
            {
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                SoldBy = dto.SoldBy,
                IsAvailable = dto.IsAvailable,
                ImageUrl = dto.ImageUrl
            };

            var options = dto.PriceOptions.Select(option => new DishPriceOption
            {
                Id = Guid.NewGuid(),
                DishId = dish.Id,
                Label = string.IsNullOrWhiteSpace(option.Label) ? $"{option.UnitAmount} {option.UnitOfMeasure}" : option.Label,
                UnitOfMeasure = option.UnitOfMeasure.Trim(),
                UnitAmount = option.UnitAmount,
                Price = option.Price
            }).ToList();

            var validationError = ValidateDish(dish, options);
            if (validationError is not null)
                return Results.BadRequest(validationError);

            dish.PriceOptions.AddRange(options);

            db.Dishes.Add(dish);
            await db.SaveChangesAsync();

            await cache.RemoveAsync(AllDishesKey);

            return Results.Created($"/{dish.Id}", new { data = dish, correlation_id = correlationId });
        });

        app.MapDelete("/{id}", async (Guid id, AppDbContext db, IDistributedCache cache, HttpContext ctx) =>
        {
            var affected = await db.Dishes
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync();

            if (affected > 0)
            {
                await cache.RemoveAsync(DishKey(id));
                await cache.RemoveAsync(AllDishesKey);
            }

            if (affected == 0) return Results.NotFound();

            return Results.NoContent();
        });

        app.MapPut("/{id}", async (Guid id, DishUpdateDto dto, AppDbContext db, IDistributedCache cache, HttpContext ctx) =>
        {
            var dish = await db.Dishes
                .Include(d => d.PriceOptions)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dish is null) return Results.NotFound();

            if (dto.Name is null && dto.Description is null && dto.SoldBy is null && dto.IsAvailable is null && dto.ImageUrl is null && dto.PriceOptions is null)
            {
                return Results.BadRequest("Invalid update data");
            }

            if (dto.Name is not null) dish.Name = dto.Name.Trim();
            if (dto.Description is not null) dish.Description = dto.Description;
            if (dto.SoldBy.HasValue) dish.SoldBy = dto.SoldBy.Value;
            if (dto.IsAvailable.HasValue) dish.IsAvailable = dto.IsAvailable.Value;
            if (dto.ImageUrl is not null) dish.ImageUrl = dto.ImageUrl;

            if (dto.PriceOptions is not null)
            {
                dish.PriceOptions.Clear();
                foreach (var option in dto.PriceOptions)
                {
                    dish.PriceOptions.Add(new DishPriceOption
                    {
                        Id = Guid.NewGuid(),
                        DishId = dish.Id,
                        Label = string.IsNullOrWhiteSpace(option.Label) ? $"{option.UnitAmount} {option.UnitOfMeasure}" : option.Label,
                        UnitOfMeasure = option.UnitOfMeasure.Trim(),
                        UnitAmount = option.UnitAmount,
                        Price = option.Price
                    });
                }
            }

            var validationError = ValidateDish(dish, dish.PriceOptions);
            if (validationError is not null)
                return Results.BadRequest(validationError);

            await db.SaveChangesAsync();

            await cache.RemoveAsync(DishKey(id));
            await cache.RemoveAsync(AllDishesKey);

            return Results.NoContent();
        });

        app.MapPatch("/{id}", () => Results.StatusCode(StatusCodes.Status405MethodNotAllowed));

        app.Run();
    }

    static string? ValidateDish(Dish dish, IEnumerable<DishPriceOption> priceOptions)
    {
        if (string.IsNullOrWhiteSpace(dish.Name))
            return "Name is required.";

        if (priceOptions is null || !priceOptions.Any())
            return "At least one price option is required.";

        foreach (var option in priceOptions)
        {
            if (string.IsNullOrWhiteSpace(option.UnitOfMeasure))
                return "Price option unitOfMeasure is required.";
            if (option.UnitAmount <= 0)
                return "Price option unitAmount must be greater than 0.";
            if (option.Price <= 0)
                return "Price option price must be greater than 0.";
        }

        return null;
    }
}

public record DishPriceOptionDto(string UnitOfMeasure, decimal UnitAmount, decimal Price, string? Label);
public record DishCreateDto(string Name, string? Description, SoldBy SoldBy, bool IsAvailable, string? ImageUrl, List<DishPriceOptionDto> PriceOptions);
public record DishUpdateDto(string? Name, string? Description, SoldBy? SoldBy, bool? IsAvailable, string? ImageUrl, List<DishPriceOptionDto>? PriceOptions);
