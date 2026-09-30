using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class sabit_gidere_odeme_turu_ve_krediye_kesim_gunu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "statement_day",
                table: "PaymentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "paymenttype_id",
                table: "FixedExpenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpenses_paymenttype_id",
                table: "FixedExpenses",
                column: "paymenttype_id");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedExpenses_PaymentTypes_paymenttype_id",
                table: "FixedExpenses",
                column: "paymenttype_id",
                principalTable: "PaymentTypes",
                principalColumn: "paymenttype_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixedExpenses_PaymentTypes_paymenttype_id",
                table: "FixedExpenses");

            migrationBuilder.DropIndex(
                name: "IX_FixedExpenses_paymenttype_id",
                table: "FixedExpenses");

            migrationBuilder.DropColumn(
                name: "statement_day",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "paymenttype_id",
                table: "FixedExpenses");
        }
    }
}
