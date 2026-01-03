using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAutoPopulationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_BookingId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraKmRate",
                table: "Vehicles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturesJson",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InclusionsJson",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MileageAllowancePerDay",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationsJson",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransmissionType",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BookingId",
                table: "PaymentTransactions",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_BookingId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ExtraKmRate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FeaturesJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InclusionsJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MileageAllowancePerDay",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SpecificationsJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TransmissionType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BookingId",
                table: "PaymentTransactions",
                column: "BookingId");
        }
    }
}
