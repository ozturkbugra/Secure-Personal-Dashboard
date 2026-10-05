using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class sepet_listesine_tarih : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "shoppinglist_date",
                table: "ShoppingLists",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shoppinglist_date",
                table: "ShoppingLists");
        }
    }
}
