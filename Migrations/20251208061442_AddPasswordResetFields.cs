using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhanaHybridRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppConfigs",
                columns: table => new
                {
                    ConfigKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigs", x => x.ConfigKey);
                });

            migrationBuilder.CreateTable(
                name: "CarCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DefaultDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DefaultDepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RequiresDriver = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DefaultDeliveryFee = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ValueJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DailyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CoverageSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WebhookUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HoursBeforePickup = table.Column<int>(type: "integer", nullable: false),
                    RefundPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    RefundDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundPolicies_CarCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CarCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RegionalPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriceMultiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ExtraHoldAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionalPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionalPricings_CarCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CarCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PickupFee = table.Column<decimal>(type: "numeric", nullable: true),
                    DropoffFee = table.Column<decimal>(type: "numeric", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Airports_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LicenseExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerEmployerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Available = table.Column<bool>(type: "boolean", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DailyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalTrips = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_OwnerEmployerId",
                        column: x => x.OwnerEmployerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwnerProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BusinessRegistrationNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayoutPreference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayoutDetailsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_OwnerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalPayoutId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PayoutDetailsJson = table.Column<string>(type: "text", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    BookingIdsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferredUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IntegrationPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RewardAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    RewardCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_IntegrationPartners_IntegrationPartnerId",
                        column: x => x.IntegrationPartnerId,
                        principalTable: "IntegrationPartners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RenterProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Nationality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Dob = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DocumentsJson = table.Column<string>(type: "text", nullable: true),
                    DriverLicenseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DriverLicenseExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverLicensePhotoUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NationalIdNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NationalIdPhotoUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PassportNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PassportExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassportPhotoUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenterProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_RenterProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionTaken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlateNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Make = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Transmission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FuelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SeatingCapacity = table.Column<int>(type: "integer", nullable: false),
                    HasAC = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PhotosJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_CarCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CarCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vehicles_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PickupDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PickupLocationJson = table.Column<string>(type: "text", nullable: true),
                    ReturnLocationJson = table.Column<string>(type: "text", nullable: true),
                    WithDriver = table.Column<bool>(type: "boolean", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    BookingReference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    RentalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "numeric", nullable: true),
                    FeesJson = table.Column<string>(type: "text", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InsurancePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsuranceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    InsuranceAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    PickupInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_InsurancePlans_InsurancePlanId",
                        column: x => x.InsurancePlanId,
                        principalTable: "InsurancePlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_RenterId",
                        column: x => x.RenterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bookings_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MagicLinkToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotosJson = table.Column<string>(type: "text", nullable: true),
                    Mileage = table.Column<int>(type: "integer", nullable: true),
                    FuelLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DamageNotesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ModerationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModeratedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModerationNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ModeratedByUserId",
                        column: x => x.ModeratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_CityId",
                table: "Airports",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DriverId",
                table: "Bookings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_InsurancePlanId",
                table: "Bookings",
                column: "InsurancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PickupInspectionId",
                table: "Bookings",
                column: "PickupInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RenterId",
                table: "Bookings",
                column: "RenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReturnInspectionId",
                table: "Bookings",
                column: "ReturnInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleId",
                table: "Bookings",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_OwnerEmployerId",
                table: "DriverProfiles",
                column: "OwnerEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_BookingId",
                table: "Inspections",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationPartners_ApiKey",
                table: "IntegrationPartners",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationPartners_ReferralCode",
                table: "IntegrationPartners",
                column: "ReferralCode",
                unique: true,
                filter: "\"ReferralCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BookingId",
                table: "PaymentTransactions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_OwnerId",
                table: "Payouts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_IntegrationPartnerId",
                table: "Referrals",
                column: "IntegrationPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredUserId",
                table: "Referrals",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerUserId",
                table: "Referrals",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_CategoryId",
                table: "RefundPolicies",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionalPricings_CategoryId",
                table: "RegionalPricings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReviewedByUserId",
                table: "Reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ModeratedByUserId",
                table: "Reviews",
                column: "ModeratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerUserId",
                table: "Reviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CategoryId",
                table: "Vehicles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CityId",
                table: "Vehicles",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId",
                table: "Vehicles",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Inspections_PickupInspectionId",
                table: "Bookings",
                column: "PickupInspectionId",
                principalTable: "Inspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Inspections_ReturnInspectionId",
                table: "Bookings",
                column: "ReturnInspectionId",
                principalTable: "Inspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Cities_CityId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Inspections_PickupInspectionId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Inspections_ReturnInspectionId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Airports");

            migrationBuilder.DropTable(
                name: "AppConfigs");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "OwnerProfiles");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RefundPolicies");

            migrationBuilder.DropTable(
                name: "RegionalPricings");

            migrationBuilder.DropTable(
                name: "RenterProfiles");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "IntegrationPartners");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "InsurancePlans");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "CarCategories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
