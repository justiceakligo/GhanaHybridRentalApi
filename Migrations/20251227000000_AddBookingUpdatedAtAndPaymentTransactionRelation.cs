using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingUpdatedAtAndPaymentTransactionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UpdatedAt column to Bookings table
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "timestamp without time zone",
                nullable: true);

            // Note: PaymentTransaction navigation property doesn't require a column
            // It uses the existing BookingId foreign key in PaymentTransactions table
            // The relationship is already configured through EF Core conventions
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");
        }
    }
}
