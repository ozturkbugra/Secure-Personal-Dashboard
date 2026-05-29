using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class sabit_giderler_tablosu_eklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedExpenses",
                columns: table => new
                {
                    fixedexpense_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    expensetype_id = table.Column<int>(type: "int", nullable: false),
                    payment_day = table.Column<int>(type: "int", nullable: false),
                    frequency_count = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedExpenses", x => x.fixedexpense_id);
                    table.ForeignKey(
                        name: "FK_FixedExpenses_ExpenseTypes_expensetype_id",
                        column: x => x.expensetype_id,
                        principalTable: "ExpenseTypes",
                        principalColumn: "expensetype_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpenses_expensetype_id",
                table: "FixedExpenses",
                column: "expensetype_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedExpenses");
        }
    }
}
