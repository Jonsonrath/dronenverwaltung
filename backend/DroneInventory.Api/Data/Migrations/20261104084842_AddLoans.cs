using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DroneId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoanDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_Drones_DroneId",
                        column: x => x.DroneId,
                        principalTable: "Drones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LoanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanItems", x => x.Id);
                    table.CheckConstraint("CK_LoanItems_Quantity", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_LoanItems_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanItems_LoanId_Type",
                table: "LoanItems",
                columns: new[] { "LoanId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DroneId_Active",
                table: "Loans",
                column: "DroneId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_StudentGroupId",
                table: "Loans",
                column: "StudentGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanItems");

            migrationBuilder.DropTable(
                name: "Loans");
        }
    }
}
