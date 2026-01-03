using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTripStartCompleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InstantWithdrawalEnabled",
                table: "OwnerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPayoutAmount",
                table: "OwnerProfiles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PayoutFrequency",
                table: "OwnerProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPickupDateTime",
                table: "Bookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PostTripFuelLevel",
                table: "Bookings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostTripNotes",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostTripOdometer",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostTripPhotosJson",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostTripRecordedAt",
                table: "Bookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostTripRecordedBy",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PreTripFuelLevel",
                table: "Bookings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreTripNotes",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreTripOdometer",
                table: "Bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreTripPhotosJson",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreTripRecordedAt",
                table: "Bookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreTripRecordedBy",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DepositRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalRefundId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RefundDetailsJson = table.Column<string>(type: "text", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdminNotified = table.Column<bool>(type: "boolean", nullable: false),
                    AdminNotifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositRefunds_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepositRefunds_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InstantWithdrawals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FeePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalTransferId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PayoutDetailsJson = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstantWithdrawals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstantWithdrawals_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefundAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositRefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundAuditLogs_DepositRefunds_DepositRefundId",
                        column: x => x.DepositRefundId,
                        principalTable: "DepositRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundAuditLogs_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositRefunds_BookingId",
                table: "DepositRefunds",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositRefunds_ProcessedByUserId",
                table: "DepositRefunds",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstantWithdrawals_OwnerId",
                table: "InstantWithdrawals",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundAuditLogs_DepositRefundId",
                table: "RefundAuditLogs",
                column: "DepositRefundId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundAuditLogs_PerformedByUserId",
                table: "RefundAuditLogs",
                column: "PerformedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstantWithdrawals");

            migrationBuilder.DropTable(
                name: "RefundAuditLogs");

            migrationBuilder.DropTable(
                name: "DepositRefunds");

            migrationBuilder.DropColumn(
                name: "InstantWithdrawalEnabled",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "MinimumPayoutAmount",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutFrequency",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "ActualPickupDateTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripFuelLevel",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripOdometer",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripPhotosJson",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripRecordedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PostTripRecordedBy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripFuelLevel",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripOdometer",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripPhotosJson",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripRecordedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PreTripRecordedBy",
                table: "Bookings");
        }
    }
}
