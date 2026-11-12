using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibleStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsibleStudentId",
                table: "StudentGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleStudentName",
                table: "Loans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_ResponsibleStudentId",
                table: "StudentGroups",
                column: "ResponsibleStudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_Students_ResponsibleStudentId",
                table: "StudentGroups",
                column: "ResponsibleStudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_Students_ResponsibleStudentId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_ResponsibleStudentId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "ResponsibleStudentId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "ResponsibleStudentName",
                table: "Loans");
        }
    }
}
