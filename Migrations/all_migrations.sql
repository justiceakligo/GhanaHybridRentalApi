CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "AppConfigs" (
        "ConfigKey" character varying(128) NOT NULL,
        "ConfigValue" text NOT NULL,
        "IsSensitive" boolean NOT NULL,
        "Scope" character varying(32) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AppConfigs" PRIMARY KEY ("ConfigKey")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "CarCategories" (
        "Id" uuid NOT NULL,
        "Name" character varying(128) NOT NULL,
        "Description" character varying(512),
        "DefaultDailyRate" numeric(18,2) NOT NULL,
        "MinDailyRate" numeric(18,2) NOT NULL,
        "MaxDailyRate" numeric(18,2) NOT NULL,
        "DefaultDepositAmount" numeric NOT NULL,
        "RequiresDriver" boolean NOT NULL,
        CONSTRAINT "PK_CarCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Cities" (
        "Id" uuid NOT NULL,
        "Name" character varying(128) NOT NULL,
        "Region" character varying(128),
        "CountryCode" character varying(8),
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "Description" character varying(512),
        "DefaultDeliveryFee" numeric,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Cities" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "GlobalSettings" (
        "Key" character varying(128) NOT NULL,
        "ValueJson" text NOT NULL,
        CONSTRAINT "PK_GlobalSettings" PRIMARY KEY ("Key")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "InsurancePlans" (
        "Id" uuid NOT NULL,
        "Name" character varying(128) NOT NULL,
        "Description" character varying(512),
        "DailyPrice" numeric(18,2) NOT NULL,
        "CoverageSummary" character varying(1024),
        "IsMandatory" boolean NOT NULL,
        "IsDefault" boolean NOT NULL,
        "Active" boolean NOT NULL,
        CONSTRAINT "PK_InsurancePlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "IntegrationPartners" (
        "Id" uuid NOT NULL,
        "Name" character varying(256) NOT NULL,
        "Type" character varying(32) NOT NULL,
        "ApiKey" character varying(256) NOT NULL,
        "ReferralCode" character varying(64),
        "WebhookUrl" character varying(512),
        "Active" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastUsedAt" timestamp with time zone,
        CONSTRAINT "PK_IntegrationPartners" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "OtpCodes" (
        "Id" uuid NOT NULL,
        "Phone" character varying(32) NOT NULL,
        "Code" character varying(16) NOT NULL,
        "Purpose" character varying(32) NOT NULL,
        "Channel" character varying(32) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "Used" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OtpCodes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(256),
        "Phone" character varying(32),
        "FirstName" character varying(128),
        "LastName" character varying(128),
        "PasswordHash" character varying(256) NOT NULL,
        "Role" character varying(32) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "PhoneVerified" boolean NOT NULL,
        "PasswordResetToken" character varying(256),
        "PasswordResetTokenExpiry" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "RefundPolicies" (
        "Id" uuid NOT NULL,
        "PolicyName" character varying(200) NOT NULL,
        "Description" character varying(500),
        "HoursBeforePickup" integer NOT NULL,
        "RefundPercentage" numeric(5,2) NOT NULL,
        "RefundDeposit" boolean NOT NULL,
        "CategoryId" uuid,
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RefundPolicies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefundPolicies_CarCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "CarCategories" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "RegionalPricings" (
        "Id" uuid NOT NULL,
        "Region" character varying(100) NOT NULL,
        "City" character varying(100),
        "CategoryId" uuid,
        "PriceMultiplier" numeric(5,2) NOT NULL,
        "ExtraHoldAmount" numeric(18,2) NOT NULL,
        "MinDailyRate" numeric(18,2),
        "MaxDailyRate" numeric(18,2),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RegionalPricings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RegionalPricings_CarCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "CarCategories" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Airports" (
        "Id" uuid NOT NULL,
        "Name" character varying(128) NOT NULL,
        "Code" character varying(10) NOT NULL,
        "CityId" uuid NOT NULL,
        "Address" character varying(512),
        "Latitude" numeric,
        "Longitude" numeric,
        "IsActive" boolean NOT NULL,
        "PickupFee" numeric,
        "DropoffFee" numeric,
        "DisplayOrder" integer NOT NULL,
        "Instructions" character varying(1024),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Airports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Airports_Cities_CityId" FOREIGN KEY ("CityId") REFERENCES "Cities" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "DriverProfiles" (
        "UserId" uuid NOT NULL,
        "FullName" character varying(256),
        "LicenseNumber" character varying(64),
        "LicenseExpiryDate" timestamp with time zone,
        "VerificationStatus" character varying(32) NOT NULL,
        "OwnerEmployerId" uuid,
        "DriverType" character varying(32) NOT NULL,
        "Available" boolean NOT NULL,
        "PhotoUrl" character varying(512),
        "Bio" character varying(1000),
        "DailyRate" numeric,
        "YearsOfExperience" integer,
        "AverageRating" numeric,
        "TotalTrips" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "DocumentsJson" text,
        CONSTRAINT "PK_DriverProfiles" PRIMARY KEY ("UserId"),
        CONSTRAINT "FK_DriverProfiles_Users_OwnerEmployerId" FOREIGN KEY ("OwnerEmployerId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_DriverProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "OwnerProfiles" (
        "UserId" uuid NOT NULL,
        "OwnerType" character varying(32) NOT NULL,
        "DisplayName" character varying(256),
        "CompanyName" character varying(256),
        "BusinessRegistrationNumber" character varying(128),
        "PayoutPreference" character varying(32) NOT NULL,
        "PayoutDetailsJson" text,
        CONSTRAINT "PK_OwnerProfiles" PRIMARY KEY ("UserId"),
        CONSTRAINT "FK_OwnerProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Payouts" (
        "Id" uuid NOT NULL,
        "OwnerId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "Method" character varying(32) NOT NULL,
        "ExternalPayoutId" character varying(256),
        "Reference" character varying(256),
        "PayoutDetailsJson" text,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ProcessedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "ErrorMessage" text,
        "BookingIdsJson" text,
        CONSTRAINT "PK_Payouts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Payouts_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Referrals" (
        "Id" uuid NOT NULL,
        "ReferralCode" character varying(64) NOT NULL,
        "ReferrerUserId" uuid,
        "ReferredUserId" uuid,
        "IntegrationPartnerId" uuid,
        "Status" character varying(32) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "RewardAmount" numeric(18,2),
        "RewardCurrency" character varying(8),
        CONSTRAINT "PK_Referrals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Referrals_IntegrationPartners_IntegrationPartnerId" FOREIGN KEY ("IntegrationPartnerId") REFERENCES "IntegrationPartners" ("Id"),
        CONSTRAINT "FK_Referrals_Users_ReferredUserId" FOREIGN KEY ("ReferredUserId") REFERENCES "Users" ("Id"),
        CONSTRAINT "FK_Referrals_Users_ReferrerUserId" FOREIGN KEY ("ReferrerUserId") REFERENCES "Users" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "RenterProfiles" (
        "UserId" uuid NOT NULL,
        "FullName" character varying(256),
        "Nationality" character varying(64),
        "Dob" timestamp with time zone,
        "VerificationStatus" character varying(32) NOT NULL,
        "DocumentsJson" text,
        "DriverLicenseNumber" character varying(64),
        "DriverLicenseExpiryDate" timestamp with time zone,
        "DriverLicensePhotoUrl" character varying(256),
        "NationalIdNumber" character varying(64),
        "NationalIdPhotoUrl" character varying(256),
        "PassportNumber" character varying(64),
        "PassportExpiryDate" timestamp with time zone,
        "PassportPhotoUrl" character varying(256),
        CONSTRAINT "PK_RenterProfiles" PRIMARY KEY ("UserId"),
        CONSTRAINT "FK_RenterProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Reports" (
        "Id" uuid NOT NULL,
        "ReporterUserId" uuid NOT NULL,
        "TargetType" character varying(50) NOT NULL,
        "TargetId" uuid NOT NULL,
        "Reason" character varying(100) NOT NULL,
        "Description" character varying(2000),
        "Status" character varying(50) NOT NULL,
        "ActionTaken" character varying(50),
        "ReviewedByUserId" uuid,
        "AdminNotes" character varying(1000),
        "ReviewedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Reports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Reports_Users_ReporterUserId" FOREIGN KEY ("ReporterUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Reports_Users_ReviewedByUserId" FOREIGN KEY ("ReviewedByUserId") REFERENCES "Users" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Vehicles" (
        "Id" uuid NOT NULL,
        "OwnerId" uuid NOT NULL,
        "PlateNumber" character varying(32) NOT NULL,
        "Make" character varying(64) NOT NULL,
        "Model" character varying(64) NOT NULL,
        "Year" integer NOT NULL,
        "CategoryId" uuid,
        "CityId" uuid,
        "Transmission" character varying(16) NOT NULL,
        "FuelType" character varying(32) NOT NULL,
        "SeatingCapacity" integer NOT NULL,
        "HasAC" boolean NOT NULL,
        "Status" character varying(32) NOT NULL,
        "PhotosJson" text,
        CONSTRAINT "PK_Vehicles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Vehicles_CarCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "CarCategories" ("Id"),
        CONSTRAINT "FK_Vehicles_Cities_CityId" FOREIGN KEY ("CityId") REFERENCES "Cities" ("Id"),
        CONSTRAINT "FK_Vehicles_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Bookings" (
        "Id" uuid NOT NULL,
        "RenterId" uuid NOT NULL,
        "VehicleId" uuid NOT NULL,
        "OwnerId" uuid NOT NULL,
        "Status" character varying(32) NOT NULL,
        "PickupDateTime" timestamp with time zone NOT NULL,
        "ReturnDateTime" timestamp with time zone NOT NULL,
        "PickupLocationJson" text,
        "ReturnLocationJson" text,
        "WithDriver" boolean NOT NULL,
        "DriverId" uuid,
        "DriverAmount" numeric,
        "BookingReference" character varying(32) NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "RentalAmount" numeric(18,2) NOT NULL,
        "DepositAmount" numeric(18,2) NOT NULL,
        "PlatformFee" numeric,
        "FeesJson" text,
        "TotalAmount" numeric(18,2) NOT NULL,
        "PaymentStatus" character varying(32) NOT NULL,
        "PaymentMethod" character varying(16) NOT NULL,
        "InsurancePlanId" uuid,
        "InsuranceAmount" numeric(18,2),
        "InsuranceAccepted" boolean NOT NULL,
        "PickupInspectionId" uuid,
        "ReturnInspectionId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Bookings_InsurancePlans_InsurancePlanId" FOREIGN KEY ("InsurancePlanId") REFERENCES "InsurancePlans" ("Id"),
        CONSTRAINT "FK_Bookings_Users_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Users" ("Id"),
        CONSTRAINT "FK_Bookings_Users_RenterId" FOREIGN KEY ("RenterId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Bookings_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Inspections" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Type" character varying(16) NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "Notes" text,
        "MagicLinkToken" character varying(256),
        "ExpiresAt" timestamp with time zone,
        "PhotosJson" text,
        "Mileage" integer,
        "FuelLevel" character varying(32),
        "DamageNotesJson" text,
        CONSTRAINT "PK_Inspections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Inspections_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "PaymentTransactions" (
        "Id" uuid NOT NULL,
        "BookingId" uuid,
        "UserId" uuid NOT NULL,
        "Type" character varying(32) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "Method" character varying(32) NOT NULL,
        "ExternalTransactionId" character varying(256),
        "Reference" character varying(256),
        "MetadataJson" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "ErrorMessage" text,
        CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentTransactions_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id"),
        CONSTRAINT "FK_PaymentTransactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE TABLE "Reviews" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "ReviewerUserId" uuid NOT NULL,
        "TargetType" character varying(50) NOT NULL,
        "TargetId" uuid,
        "Rating" integer NOT NULL,
        "Comment" character varying(2000),
        "ModerationStatus" character varying(50) NOT NULL,
        "ModeratedByUserId" uuid,
        "ModerationNotes" character varying(500),
        "ModeratedAt" timestamp with time zone,
        "IsVisible" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Reviews_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Reviews_Users_ModeratedByUserId" FOREIGN KEY ("ModeratedByUserId") REFERENCES "Users" ("Id"),
        CONSTRAINT "FK_Reviews_Users_ReviewerUserId" FOREIGN KEY ("ReviewerUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Airports_CityId" ON "Airports" ("CityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_DriverId" ON "Bookings" ("DriverId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_InsurancePlanId" ON "Bookings" ("InsurancePlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_PickupInspectionId" ON "Bookings" ("PickupInspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_RenterId" ON "Bookings" ("RenterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_ReturnInspectionId" ON "Bookings" ("ReturnInspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Bookings_VehicleId" ON "Bookings" ("VehicleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_DriverProfiles_OwnerEmployerId" ON "DriverProfiles" ("OwnerEmployerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Inspections_BookingId" ON "Inspections" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE UNIQUE INDEX "IX_IntegrationPartners_ApiKey" ON "IntegrationPartners" ("ApiKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE UNIQUE INDEX "IX_IntegrationPartners_ReferralCode" ON "IntegrationPartners" ("ReferralCode") WHERE "ReferralCode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_PaymentTransactions_BookingId" ON "PaymentTransactions" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_PaymentTransactions_UserId" ON "PaymentTransactions" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Payouts_OwnerId" ON "Payouts" ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Referrals_IntegrationPartnerId" ON "Referrals" ("IntegrationPartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Referrals_ReferredUserId" ON "Referrals" ("ReferredUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Referrals_ReferrerUserId" ON "Referrals" ("ReferrerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_RefundPolicies_CategoryId" ON "RefundPolicies" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_RegionalPricings_CategoryId" ON "RegionalPricings" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Reports_ReporterUserId" ON "Reports" ("ReporterUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Reports_ReviewedByUserId" ON "Reports" ("ReviewedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Reviews_BookingId" ON "Reviews" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Reviews_ModeratedByUserId" ON "Reviews" ("ModeratedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Reviews_ReviewerUserId" ON "Reviews" ("ReviewerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE UNIQUE INDEX "IX_Users_Phone" ON "Users" ("Phone") WHERE "Phone" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Vehicles_CategoryId" ON "Vehicles" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Vehicles_CityId" ON "Vehicles" ("CityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    CREATE INDEX "IX_Vehicles_OwnerId" ON "Vehicles" ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_Inspections_PickupInspectionId" FOREIGN KEY ("PickupInspectionId") REFERENCES "Inspections" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_Inspections_ReturnInspectionId" FOREIGN KEY ("ReturnInspectionId") REFERENCES "Inspections" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208061442_AddPasswordResetFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208061442_AddPasswordResetFields', '8.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211040202_RentalAgreementSystem') THEN
    CREATE TABLE "RentalAgreementAcceptances" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "RenterId" uuid NOT NULL,
        "TemplateVersion" character varying(20) NOT NULL,
        "TemplateCode" character varying(100) NOT NULL,
        "AcceptedNoSmoking" boolean NOT NULL,
        "AcceptedFinesAndTickets" boolean NOT NULL,
        "AcceptedAccidentProcedure" boolean NOT NULL,
        "AgreementSnapshot" text,
        "AcceptedAt" timestamp with time zone NOT NULL,
        "IpAddress" character varying(100),
        "UserAgent" character varying(200),
        CONSTRAINT "PK_RentalAgreementAcceptances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RentalAgreementAcceptances_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RentalAgreementAcceptances_Users_RenterId" FOREIGN KEY ("RenterId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211040202_RentalAgreementSystem') THEN
    CREATE TABLE "RentalAgreementTemplates" (
        "Id" uuid NOT NULL,
        "Code" character varying(100) NOT NULL,
        "Version" character varying(20) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "BodyText" text NOT NULL,
        "RequireNoSmokingConfirmation" boolean NOT NULL,
        "RequireFinesAndTicketsConfirmation" boolean NOT NULL,
        "RequireAccidentProcedureConfirmation" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RentalAgreementTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211040202_RentalAgreementSystem') THEN
    CREATE INDEX "IX_RentalAgreementAcceptances_BookingId" ON "RentalAgreementAcceptances" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211040202_RentalAgreementSystem') THEN
    CREATE INDEX "IX_RentalAgreementAcceptances_RenterId" ON "RentalAgreementAcceptances" ("RenterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211040202_RentalAgreementSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251211040202_RentalAgreementSystem', '8.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    DROP INDEX "IX_Users_Email";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    DROP INDEX "IX_Users_Phone";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    DROP INDEX "IX_IntegrationPartners_ReferralCode";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE TABLE "Partners" (
        "Id" uuid NOT NULL,
        "Name" character varying(128) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "LogoUrl" character varying(512),
        "WebsiteUrl" character varying(512),
        "PhoneNumber" character varying(32),
        "City" character varying(64) NOT NULL,
        "Country" character varying(8) NOT NULL,
        "Latitude" numeric,
        "Longitude" numeric,
        "TargetRoles" character varying(64) NOT NULL,
        "Categories" character varying(256) NOT NULL,
        "PriorityScore" integer NOT NULL,
        "IsFeatured" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "ReferralCode" character varying(64),
        "Metadata" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Partners" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE TABLE "PostRentalChargeTypes" (
        "Id" uuid NOT NULL,
        "Code" character varying(64) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500),
        "DefaultAmount" numeric NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "RecipientType" character varying(32) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PostRentalChargeTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE TABLE "PartnerClicks" (
        "Id" uuid NOT NULL,
        "PartnerId" uuid NOT NULL,
        "UserId" uuid,
        "BookingId" uuid,
        "Role" character varying(32),
        "City" character varying(64),
        "EventType" character varying(32) NOT NULL,
        "ConversionAmount" numeric,
        "ExternalReference" character varying(128),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PartnerClicks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PartnerClicks_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id"),
        CONSTRAINT "FK_PartnerClicks_Partners_PartnerId" FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PartnerClicks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE TABLE "BookingCharges" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "ChargeTypeId" uuid NOT NULL,
        "Amount" numeric NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "Label" character varying(128),
        "Notes" text,
        "EvidencePhotoUrlsJson" text NOT NULL,
        "Status" character varying(32) NOT NULL,
        "CreatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "SettledAt" timestamp with time zone,
        "PaymentTransactionId" uuid,
        CONSTRAINT "PK_BookingCharges" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BookingCharges_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_BookingCharges_PaymentTransactions_PaymentTransactionId" FOREIGN KEY ("PaymentTransactionId") REFERENCES "PaymentTransactions" ("Id"),
        CONSTRAINT "FK_BookingCharges_PostRentalChargeTypes_ChargeTypeId" FOREIGN KEY ("ChargeTypeId") REFERENCES "PostRentalChargeTypes" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_BookingCharges_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE UNIQUE INDEX "IX_Users_Phone" ON "Users" ("Phone") WHERE "Phone" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE UNIQUE INDEX "IX_IntegrationPartners_ReferralCode" ON "IntegrationPartners" ("ReferralCode") WHERE "ReferralCode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_BookingCharges_BookingId" ON "BookingCharges" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_BookingCharges_ChargeTypeId" ON "BookingCharges" ("ChargeTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_BookingCharges_CreatedByUserId" ON "BookingCharges" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_BookingCharges_PaymentTransactionId" ON "BookingCharges" ("PaymentTransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_PartnerClicks_BookingId" ON "PartnerClicks" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_PartnerClicks_PartnerId" ON "PartnerClicks" ("PartnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    CREATE INDEX "IX_PartnerClicks_UserId" ON "PartnerClicks" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213030142_PartnerSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251213030142_PartnerSystem', '8.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Users" ALTER COLUMN "PasswordResetTokenExpiry" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Users" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reviews" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reviews" ALTER COLUMN "ModeratedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reviews" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reports" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reports" ALTER COLUMN "ReviewedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Reports" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RenterProfiles" ALTER COLUMN "PassportExpiryDate" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RenterProfiles" ALTER COLUMN "DriverLicenseExpiryDate" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RenterProfiles" ALTER COLUMN "Dob" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RentalAgreementTemplates" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RentalAgreementTemplates" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RentalAgreementAcceptances" ALTER COLUMN "AcceptedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RegionalPricings" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RegionalPricings" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RefundPolicies" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "RefundPolicies" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Referrals" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Referrals" ALTER COLUMN "CompletedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "PostRentalChargeTypes" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "PostRentalChargeTypes" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Payouts" ALTER COLUMN "ProcessedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Payouts" ALTER COLUMN "PeriodStart" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Payouts" ALTER COLUMN "PeriodEnd" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Payouts" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Payouts" ALTER COLUMN "CompletedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "PaymentTransactions" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "PaymentTransactions" ALTER COLUMN "CompletedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Partners" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Partners" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "PartnerClicks" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "OtpCodes" ALTER COLUMN "ExpiresAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "OtpCodes" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "IntegrationPartners" ALTER COLUMN "LastUsedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "IntegrationPartners" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Inspections" ALTER COLUMN "ExpiresAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Inspections" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Inspections" ALTER COLUMN "CompletedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "DriverProfiles" ALTER COLUMN "LicenseExpiryDate" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "DriverProfiles" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Cities" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Cities" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Bookings" ALTER COLUMN "ReturnDateTime" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Bookings" ALTER COLUMN "PickupDateTime" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Bookings" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "BookingCharges" ALTER COLUMN "SettledAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "BookingCharges" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "AppConfigs" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "AppConfigs" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Airports" ALTER COLUMN "UpdatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    ALTER TABLE "Airports" ALTER COLUMN "CreatedAt" TYPE timestamp without time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213051214_FixDateTimeColumns') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251213051214_FixDateTimeColumns', '8.0.0');
    END IF;
END $EF$;
COMMIT;

