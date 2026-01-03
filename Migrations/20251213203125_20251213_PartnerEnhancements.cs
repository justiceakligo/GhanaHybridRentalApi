using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class _20251213_PartnerEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessHoursJson",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactJson",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrlsJson",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationJson",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Partners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RatingAvg",
                table: "Partners",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "Partners",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationBadge",
                table: "Partners",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessHoursJson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactJson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ImageUrlsJson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IntegrationJson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RatingAvg",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "VerificationBadge",
                table: "Partners");
        }
    }
}
