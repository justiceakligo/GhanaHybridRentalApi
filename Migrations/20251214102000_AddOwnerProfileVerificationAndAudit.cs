using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    public partial class AddOwnerProfileVerificationAndAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "OwnerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyNamePending",
                table: "OwnerProfiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessRegistrationNumberPending",
                table: "OwnerProfiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyVerificationStatus",
                table: "OwnerProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "unverified");

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
                defaultValue: "unverified");

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileChangeAudits", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProfileChangeAudits");

            migrationBuilder.DropColumn(name: "Bio", table: "OwnerProfiles");
            migrationBuilder.DropColumn(name: "CompanyNamePending", table: "OwnerProfiles");
            migrationBuilder.DropColumn(name: "BusinessRegistrationNumberPending", table: "OwnerProfiles");
            migrationBuilder.DropColumn(name: "CompanyVerificationStatus", table: "OwnerProfiles");
            migrationBuilder.DropColumn(name: "PayoutDetailsPendingJson", table: "OwnerProfiles");
            migrationBuilder.DropColumn(name: "PayoutVerificationStatus", table: "OwnerProfiles");
        }
    }
}
