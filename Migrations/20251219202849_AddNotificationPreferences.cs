using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "Vehicles",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationPreferencesJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationPreferencesJson",
                table: "OwnerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TargetPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SendImmediately = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProtectionPlanId",
                table: "Bookings",
                column: "ProtectionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationJobs_ScheduledAt",
                table: "NotificationJobs",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationJobs_Status",
                table: "NotificationJobs",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_ProtectionPlans_ProtectionPlanId",
                table: "Bookings",
                column: "ProtectionPlanId",
                principalTable: "ProtectionPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_ProtectionPlans_ProtectionPlanId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "NotificationJobs");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ProtectionPlanId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "NotificationPreferencesJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotificationPreferencesJson",
                table: "OwnerProfiles");
        }
    }
}
