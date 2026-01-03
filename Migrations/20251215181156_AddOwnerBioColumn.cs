using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerBioColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFrom",
                table: "Vehicles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableUntil",
                table: "Vehicles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceDocumentUrl",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoadworthinessDocumentUrl",
                table: "Vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "OwnerProfiles",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessRegistrationNumberPending",
                table: "OwnerProfiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyNamePending",
                table: "OwnerProfiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyVerificationStatus",
                table: "OwnerProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayoutDetailsPendingJson",
                table: "OwnerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutVerificationStatus",
                table: "OwnerProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuestEmail",
                table: "Bookings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestFirstName",
                table: "Bookings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestLastName",
                table: "Bookings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Bookings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GuestUserId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProfileChangeAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Field = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileChangeAudits", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileChangeAudits");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AvailableUntil",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InsuranceDocumentUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RoadworthinessDocumentUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessRegistrationNumberPending",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyNamePending",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyVerificationStatus",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutDetailsPendingJson",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutVerificationStatus",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "GuestEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GuestFirstName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GuestLastName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GuestUserId",
                table: "Bookings");
        }
    }
}
