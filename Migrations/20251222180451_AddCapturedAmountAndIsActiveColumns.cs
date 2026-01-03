using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCapturedAmountAndIsActiveColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                table: "PaymentTransactions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CarCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PayoutAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutAuditLogs_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAuditLogs_PayoutId",
                table: "PayoutAuditLogs",
                column: "PayoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutAuditLogs");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CarCategories");
        }
    }
}
