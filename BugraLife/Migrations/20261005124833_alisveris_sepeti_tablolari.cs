using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class alisveris_sepeti_tablolari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new
                {
                    shoppinglist_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    shoppinglist_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    shoppinglist_order = table.Column<int>(type: "int", nullable: false),
                    shoppinglist_active = table.Column<bool>(type: "bit", nullable: false),
                    shoppinglist_created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLists", x => x.shoppinglist_id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingPriceHistories",
                columns: table => new
                {
                    shoppingpricehistory_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingPriceHistories", x => x.shoppingpricehistory_id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingItems",
                columns: table => new
                {
                    shoppingitem_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    shoppinglist_id = table.Column<int>(type: "int", nullable: false),
                    shoppingitem_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    shoppingitem_quantity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shoppingitem_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shoppingitem_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    shoppingitem_isbought = table.Column<bool>(type: "bit", nullable: false),
                    shoppingitem_historylogged = table.Column<bool>(type: "bit", nullable: false),
                    shoppingitem_order = table.Column<int>(type: "int", nullable: false),
                    shoppingitem_created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingItems", x => x.shoppingitem_id);
                    table.ForeignKey(
                        name: "FK_ShoppingItems_ShoppingLists_shoppinglist_id",
                        column: x => x.shoppinglist_id,
                        principalTable: "ShoppingLists",
                        principalColumn: "shoppinglist_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_shoppinglist_id",
                table: "ShoppingItems",
                column: "shoppinglist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingItems");

            migrationBuilder.DropTable(
                name: "ShoppingPriceHistories");

            migrationBuilder.DropTable(
                name: "ShoppingLists");
        }
    }
}
