using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class RentalAgreementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalAgreementAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AcceptedNoSmoking = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedFinesAndTickets = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAccidentProcedure = table.Column<bool>(type: "boolean", nullable: false),
                    AgreementSnapshot = table.Column<string>(type: "text", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalAgreementAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalAgreementAcceptances_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalAgreementAcceptances_Users_RenterId",
                        column: x => x.RenterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalAgreementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    RequireNoSmokingConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    RequireFinesAndTicketsConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    RequireAccidentProcedureConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalAgreementTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalAgreementAcceptances_BookingId",
                table: "RentalAgreementAcceptances",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalAgreementAcceptances_RenterId",
                table: "RentalAgreementAcceptances",
                column: "RenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalAgreementAcceptances");

            migrationBuilder.DropTable(
                name: "RentalAgreementTemplates");
        }
    }
}
