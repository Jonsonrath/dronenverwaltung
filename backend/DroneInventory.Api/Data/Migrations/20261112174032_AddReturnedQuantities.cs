using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnedQuantity",
                table: "LoanItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoanItems_ReturnedQuantity",
                table: "LoanItems",
                sql: "\"ReturnedQuantity\" IS NULL OR (\"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LoanItems_ReturnedQuantity",
                table: "LoanItems");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "LoanItems");
        }
    }
}
