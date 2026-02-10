using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DishService.Migrations
{
    public partial class AddPriceUnitToDish : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "price_unit",
                table: "dishes",
                type: "text",
                nullable: false,
                defaultValue: "кг");

            migrationBuilder.Sql(
                "UPDATE dishes SET price_unit = 'шт' WHERE is_by_weight = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "price_unit",
                table: "dishes");
        }
    }
}
