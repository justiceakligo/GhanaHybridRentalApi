using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProtectionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProtectionAmount",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProtectionPlanId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectionSnapshotJson",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProtectionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PricingMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DailyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FixedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IncludesMinorDamageWaiver = table.Column<bool>(type: "boolean", nullable: false),
                    MinorWaiverCap = table.Column<decimal>(type: "numeric", nullable: true),
                    Deductible = table.Column<decimal>(type: "numeric", nullable: true),
                    ExcludesJson = table.Column<string>(type: "text", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtectionPlans", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProtectionPlans");

            migrationBuilder.DropColumn(
                name: "ProtectionAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProtectionPlanId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProtectionSnapshotJson",
                table: "Bookings");
        }
    }
}
