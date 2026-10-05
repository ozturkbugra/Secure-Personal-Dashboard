using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugraLife.Migrations
{
    /// <inheritdoc />
    public partial class fitness_modulu_eklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FitnessDays",
                columns: table => new
                {
                    fitnessday_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fitnessday_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fitnessday_order = table.Column<int>(type: "int", nullable: false),
                    fitnessday_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessDays", x => x.fitnessday_id);
                });

            migrationBuilder.CreateTable(
                name: "FitnessExercises",
                columns: table => new
                {
                    fitnessexercise_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fitnessexercise_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fitnessexercise_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fitnessexercise_image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fitnessexercise_created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessExercises", x => x.fitnessexercise_id);
                });

            migrationBuilder.CreateTable(
                name: "FitnessGroups",
                columns: table => new
                {
                    fitnessgroup_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fitnessday_id = table.Column<int>(type: "int", nullable: false),
                    fitnessgroup_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fitnessgroup_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fitnessgroup_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessGroups", x => x.fitnessgroup_id);
                    table.ForeignKey(
                        name: "FK_FitnessGroups_FitnessDays_fitnessday_id",
                        column: x => x.fitnessday_id,
                        principalTable: "FitnessDays",
                        principalColumn: "fitnessday_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FitnessGroupExercises",
                columns: table => new
                {
                    fitnessgroupexercise_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fitnessgroup_id = table.Column<int>(type: "int", nullable: false),
                    fitnessexercise_id = table.Column<int>(type: "int", nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    fitnessgroupexercise_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessGroupExercises", x => x.fitnessgroupexercise_id);
                    table.ForeignKey(
                        name: "FK_FitnessGroupExercises_FitnessExercises_fitnessexercise_id",
                        column: x => x.fitnessexercise_id,
                        principalTable: "FitnessExercises",
                        principalColumn: "fitnessexercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FitnessGroupExercises_FitnessGroups_fitnessgroup_id",
                        column: x => x.fitnessgroup_id,
                        principalTable: "FitnessGroups",
                        principalColumn: "fitnessgroup_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessGroupExercises_fitnessexercise_id",
                table: "FitnessGroupExercises",
                column: "fitnessexercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_FitnessGroupExercises_fitnessgroup_id",
                table: "FitnessGroupExercises",
                column: "fitnessgroup_id");

            migrationBuilder.CreateIndex(
                name: "IX_FitnessGroups_fitnessday_id",
                table: "FitnessGroups",
                column: "fitnessday_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FitnessGroupExercises");

            migrationBuilder.DropTable(
                name: "FitnessExercises");

            migrationBuilder.DropTable(
                name: "FitnessGroups");

            migrationBuilder.DropTable(
                name: "FitnessDays");
        }
    }
}
