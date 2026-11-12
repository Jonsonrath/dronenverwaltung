using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneInventory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDroneSsid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedSsid",
                table: "Drones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ssid",
                table: "Drones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drones_NormalizedSsid",
                table: "Drones",
                column: "NormalizedSsid",
                unique: true,
                filter: "\"NormalizedSsid\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Drones_NormalizedSsid",
                table: "Drones");

            migrationBuilder.DropColumn(
                name: "NormalizedSsid",
                table: "Drones");

            migrationBuilder.DropColumn(
                name: "Ssid",
                table: "Drones");
        }
    }
}
