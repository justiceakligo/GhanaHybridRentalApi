using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateDocumentsTableFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent add: use raw SQL with IF NOT EXISTS to avoid collisions when optional columns are ensured elsewhere
            migrationBuilder.Sql("ALTER TABLE \"OwnerProfiles\" ADD COLUMN IF NOT EXISTS \"PayoutVerificationStatus\" character varying(32) NOT NULL DEFAULT '';");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.Sql("ALTER TABLE \"OwnerProfiles\" DROP COLUMN IF EXISTS \"PayoutVerificationStatus\";");
        }
    }
}
