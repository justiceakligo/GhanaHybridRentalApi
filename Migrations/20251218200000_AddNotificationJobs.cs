using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    public partial class AddNotificationJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    TargetPhone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    ChannelsJson = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SendImmediately = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
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
                name: "IX_NotificationJobs_Status",
                table: "NotificationJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationJobs_ScheduledAt",
                table: "NotificationJobs",
                column: "ScheduledAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationJobs");
        }
    }
}