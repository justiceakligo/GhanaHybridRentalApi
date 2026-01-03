-- Create Promo Code System Tables (Phase 1-3: Renters, Owners, Referrals)
-- Run this against ghanarentaldb database

-- PromoCodes Table
CREATE TABLE IF NOT EXISTS "PromoCodes" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Description" TEXT,
    "PromoType" VARCHAR(50) NOT NULL, -- percentage, fixed_amount, free_addon, commission_reduction, owner_vehicle_discount
    "DiscountValue" DECIMAL(18, 2) NOT NULL, -- 10 for 10%, 50 for GHS 50
    "TargetUserType" VARCHAR(20) NOT NULL, -- renter, owner, both
    "AppliesTo" VARCHAR(50) NOT NULL, -- total_amount, platform_fee, protection_plan, rental_amount, commission
    "MinimumBookingAmount" DECIMAL(18, 2) NULL, -- Minimum booking value to use code
    "MaximumDiscountAmount" DECIMAL(18, 2) NULL, -- Cap on discount (e.g., max GHS 500 off)
    "ValidFrom" TIMESTAMP NOT NULL,
    "ValidUntil" TIMESTAMP NOT NULL,
    "MaxTotalUses" INT NULL, -- Total times code can be used across all users
    "MaxUsesPerUser" INT NOT NULL DEFAULT 1, -- Times each user can use this code
    "CurrentTotalUses" INT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedBy" VARCHAR(50) NOT NULL, -- admin, owner, system
    "CreatedByUserId" UUID NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
    "CategoryId" UUID NULL REFERENCES "CarCategories"("Id") ON DELETE SET NULL, -- If applies to specific category
    "CityId" UUID NULL REFERENCES "Cities"("Id") ON DELETE SET NULL, -- If applies to specific city
    "VehicleId" UUID NULL REFERENCES "Vehicles"("Id") ON DELETE SET NULL, -- If owner-created for specific vehicle
    "FirstTimeUsersOnly" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsReferralCode" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReferrerUserId" UUID NULL REFERENCES "Users"("Id") ON DELETE SET NULL, -- For referral codes
    "ReferralRewardType" VARCHAR(50) NULL, -- credit, commission_reduction, cash
    "ReferralRewardValue" DECIMAL(18, 2) NULL, -- Reward for referrer when code is used
    "MetadataJson" TEXT NULL, -- Additional flexible data
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- PromoCodeUsage Table
CREATE TABLE IF NOT EXISTS "PromoCodeUsage" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "PromoCodeId" UUID NOT NULL REFERENCES "PromoCodes"("Id") ON DELETE CASCADE,
    "Code" VARCHAR(50) NOT NULL, -- Denormalized for quick lookup
    "UsedByUserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "UserType" VARCHAR(20) NOT NULL, -- renter, owner
    "BookingId" UUID NULL REFERENCES "Bookings"("Id") ON DELETE SET NULL,
    "OriginalAmount" DECIMAL(18, 2) NOT NULL,
    "DiscountAmount" DECIMAL(18, 2) NOT NULL,
    "FinalAmount" DECIMAL(18, 2) NOT NULL,
    "AppliedTo" VARCHAR(50) NOT NULL, -- total_amount, platform_fee, insurance, commission
    "ReferrerUserId" UUID NULL REFERENCES "Users"("Id") ON DELETE SET NULL, -- If referral code
    "ReferrerRewardAmount" DECIMAL(18, 2) NULL,
    "ReferrerRewardApplied" BOOLEAN NOT NULL DEFAULT FALSE,
    "UsedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- UserReferrals Table (Track referral relationships)
CREATE TABLE IF NOT EXISTS "UserReferrals" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ReferrerUserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ReferredUserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ReferralCode" VARCHAR(50) NOT NULL,
    "ReferralType" VARCHAR(20) NOT NULL, -- renter, owner
    "TotalRewardEarned" DECIMAL(18, 2) NOT NULL DEFAULT 0,
    "TotalBookingsFromReferred" INT NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'active', -- active, inactive, completed
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE("ReferrerUserId", "ReferredUserId")
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_Code" ON "PromoCodes"("Code");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_ValidFrom_ValidUntil" ON "PromoCodes"("ValidFrom", "ValidUntil");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_TargetUserType" ON "PromoCodes"("TargetUserType");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_IsActive" ON "PromoCodes"("IsActive");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_ReferrerUserId" ON "PromoCodes"("ReferrerUserId");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_VehicleId" ON "PromoCodes"("VehicleId");

CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsage_PromoCodeId" ON "PromoCodeUsage"("PromoCodeId");
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsage_UsedByUserId" ON "PromoCodeUsage"("UsedByUserId");
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsage_BookingId" ON "PromoCodeUsage"("BookingId");
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsage_Code" ON "PromoCodeUsage"("Code");
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsage_ReferrerUserId" ON "PromoCodeUsage"("ReferrerUserId");

CREATE INDEX IF NOT EXISTS "IX_UserReferrals_ReferrerUserId" ON "UserReferrals"("ReferrerUserId");
CREATE INDEX IF NOT EXISTS "IX_UserReferrals_ReferredUserId" ON "UserReferrals"("ReferredUserId");
CREATE INDEX IF NOT EXISTS "IX_UserReferrals_ReferralCode" ON "UserReferrals"("ReferralCode");

-- Add PromoCodeId to Bookings table for tracking
ALTER TABLE "Bookings" ADD COLUMN IF NOT EXISTS "PromoCodeId" UUID NULL REFERENCES "PromoCodes"("Id") ON DELETE SET NULL;
ALTER TABLE "Bookings" ADD COLUMN IF NOT EXISTS "PromoDiscountAmount" DECIMAL(18, 2) NOT NULL DEFAULT 0;

-- Sample promo codes for testing (insert only if they don't exist)
INSERT INTO "PromoCodes" ("Code", "Description", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "ValidFrom", "ValidUntil", "MaxTotalUses", "MaxUsesPerUser", "CreatedBy", "FirstTimeUsersOnly")
SELECT 'NEWYEAR2026', 'New Year 2026 - 20% off for new renters', 'percentage', 20, 'renter', 'total_amount', '2026-01-01', '2026-01-31', 1000, 1, 'admin', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "PromoCodes" WHERE "Code" = 'NEWYEAR2026');

INSERT INTO "PromoCodes" ("Code", "Description", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "ValidFrom", "ValidUntil", "MaxTotalUses", "MaxUsesPerUser", "CreatedBy", "FirstTimeUsersOnly")
SELECT 'WELCOME50', 'Welcome bonus - GHS 50 off first booking', 'fixed_amount', 50, 'renter', 'total_amount', '2026-01-01', '2026-12-31', NULL, 1, 'admin', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "PromoCodes" WHERE "Code" = 'WELCOME50');

INSERT INTO "PromoCodes" ("Code", "Description", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "ValidFrom", "ValidUntil", "MaxTotalUses", "MaxUsesPerUser", "CreatedBy", "FirstTimeUsersOnly")
SELECT 'FREEPROTECTION', 'Free protection plan for 3 days+', 'free_addon', 100, 'renter', 'protection_plan', '2026-01-01', '2026-03-31', 500, 3, 'admin', FALSE
WHERE NOT EXISTS (SELECT 1 FROM "PromoCodes" WHERE "Code" = 'FREEPROTECTION');

INSERT INTO "PromoCodes" ("Code", "Description", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "ValidFrom", "ValidUntil", "MaxTotalUses", "MaxUsesPerUser", "CreatedBy", "FirstTimeUsersOnly")
SELECT 'OWNER50OFF', 'Owner onboarding - 50% commission reduction for 10 bookings', 'commission_reduction', 50, 'owner', 'commission', '2026-01-01', '2026-06-30', 100, 10, 'admin', FALSE
WHERE NOT EXISTS (SELECT 1 FROM "PromoCodes" WHERE "Code" = 'OWNER50OFF');

INSERT INTO "PromoCodes" ("Code", "Description", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "ValidFrom", "ValidUntil", "MaxTotalUses", "MaxUsesPerUser", "CreatedBy", "FirstTimeUsersOnly")
SELECT 'LISTFREE', 'Free listing - 100% commission discount for first 3 bookings', 'commission_reduction', 100, 'owner', 'commission', '2026-01-01', '2026-12-31', NULL, 3, 'admin', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "PromoCodes" WHERE "Code" = 'LISTFREE');

COMMIT;
