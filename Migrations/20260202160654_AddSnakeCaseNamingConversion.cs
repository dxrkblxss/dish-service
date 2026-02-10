using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DishService.Migrations
{
    /// <inheritdoc />
    public partial class AddSnakeCaseNamingConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Dishes",
                table: "Dishes");

            migrationBuilder.RenameTable(
                name: "Dishes",
                newName: "dishes");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "dishes",
                newName: "weight");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "dishes",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "dishes",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "dishes",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "dishes",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "dishes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "dishes",
                newName: "image_url");

            migrationBuilder.RenameIndex(
                name: "IX_Dishes_Name",
                table: "dishes",
                newName: "ix_dishes_name");

            migrationBuilder.AddPrimaryKey(
                name: "pk_dishes",
                table: "dishes",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_dishes",
                table: "dishes");

            migrationBuilder.RenameTable(
                name: "dishes",
                newName: "Dishes");

            migrationBuilder.RenameColumn(
                name: "weight",
                table: "Dishes",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "Dishes",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Dishes",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Dishes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Dishes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Dishes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "image_url",
                table: "Dishes",
                newName: "ImageUrl");

            migrationBuilder.RenameIndex(
                name: "ix_dishes_name",
                table: "Dishes",
                newName: "IX_Dishes_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dishes",
                table: "Dishes",
                column: "Id");
        }
    }
}
