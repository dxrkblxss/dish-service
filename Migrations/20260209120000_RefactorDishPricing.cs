using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DishService.Migrations
{
    public partial class RefactorDishPricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sold_by",
                table: "dishes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "dishes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "dish_price_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dish_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dish_price_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_dish_price_options_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dish_price_options_dish_id",
                table: "dish_price_options",
                column: "dish_id");

            migrationBuilder.DropColumn(name: "price", table: "dishes");
            migrationBuilder.DropColumn(name: "price_unit", table: "dishes");
            migrationBuilder.DropColumn(name: "is_by_weight", table: "dishes");
            migrationBuilder.DropColumn(name: "weight", table: "dishes");
            migrationBuilder.DropColumn(name: "unit", table: "dishes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "dishes",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "price_unit",
                table: "dishes",
                type: "text",
                nullable: false,
                defaultValue: "кг");

            migrationBuilder.AddColumn<bool>(
                name: "is_by_weight",
                table: "dishes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "weight",
                table: "dishes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "dishes",
                type: "text",
                nullable: false,
                defaultValue: "г");

            migrationBuilder.DropTable(name: "dish_price_options");

            migrationBuilder.DropColumn(name: "sold_by", table: "dishes");
            migrationBuilder.DropColumn(name: "is_available", table: "dishes");
        }
    }
}
