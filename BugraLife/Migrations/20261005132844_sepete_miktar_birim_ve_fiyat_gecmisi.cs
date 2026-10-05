using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class sepete_miktar_birim_ve_fiyat_gecmisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "ShoppingPriceHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "ShoppingPriceHistories",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shoppingitem_amount",
                table: "ShoppingItems",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shoppingitem_unit",
                table: "ShoppingItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit",
                table: "ShoppingPriceHistories");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "ShoppingPriceHistories");

            migrationBuilder.DropColumn(
                name: "shoppingitem_amount",
                table: "ShoppingItems");

            migrationBuilder.DropColumn(
                name: "shoppingitem_unit",
                table: "ShoppingItems");
        }
    }
}
