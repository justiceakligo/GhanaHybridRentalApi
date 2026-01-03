using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMileageChargingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncludedKilometers",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MileageChargingEnabled",
                table: "Vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerExtraKm",
                table: "Vehicles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludedKilometers",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MileageChargingEnabled",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PricePerExtraKm",
                table: "Vehicles");
        }
    }
}
