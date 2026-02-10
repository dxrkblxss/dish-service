using Microsoft.EntityFrameworkCore;
using DishService.Models;

namespace DishService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishPriceOption> DishPriceOptions => Set<DishPriceOption>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasIndex(d => d.Name)
                .IsUnique();

            entity.HasMany(d => d.PriceOptions)
                .WithOne()
                .HasForeignKey(p => p.DishId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DishPriceOption>(entity =>
        {
            entity.Property(p => p.UnitAmount).HasPrecision(18, 4);
            entity.Property(p => p.Price).HasPrecision(18, 4);
        });
    }
}
