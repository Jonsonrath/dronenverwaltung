using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanDamageReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DamageReport",
                table: "Loans",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamageReport",
                table: "Loans");
        }
    }
}
