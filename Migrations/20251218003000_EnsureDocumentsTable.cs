using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    public partial class EnsureDocumentsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent create: use raw SQL with IF NOT EXISTS to avoid errors if table already exists
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS ""Documents"" (
                ""Id"" uuid PRIMARY KEY,
                ""FileName"" text NOT NULL,
                ""Url"" text NOT NULL,
                ""ContentType"" text NULL,
                ""Size"" bigint NULL,
                ""UploadedAt"" timestamp without time zone NOT NULL,
                ""UploadedByUserId"" uuid NULL
            );");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Documents\";");
        }
    }
}
