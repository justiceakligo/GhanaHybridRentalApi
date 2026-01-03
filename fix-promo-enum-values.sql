-- Fix enum values to match C# PascalCase

-- Update PromoType values
UPDATE "PromoCodes" SET "PromoType" = 'Percentage' WHERE "PromoType" = 'percentage';
UPDATE "PromoCodes" SET "PromoType" = 'FixedAmount' WHERE "PromoType" = 'fixed_amount';
UPDATE "PromoCodes" SET "PromoType" = 'FreeAddon' WHERE "PromoType" = 'free_addon';
UPDATE "PromoCodes" SET "PromoType" = 'CommissionReduction' WHERE "PromoType" = 'commission_reduction';

-- Update TargetUserType values
UPDATE "PromoCodes" SET "TargetUserType" = 'Renter' WHERE "TargetUserType" = 'renter';
UPDATE "PromoCodes" SET "TargetUserType" = 'Owner' WHERE "TargetUserType" = 'owner';
UPDATE "PromoCodes" SET "TargetUserType" = 'Both' WHERE "TargetUserType" = 'both';

-- Update AppliesTo values
UPDATE "PromoCodes" SET "AppliesTo" = 'TotalAmount' WHERE "AppliesTo" = 'total_amount';
UPDATE "PromoCodes" SET "AppliesTo" = 'PlatformFee' WHERE "AppliesTo" = 'platform_fee';
UPDATE "PromoCodes" SET "AppliesTo" = 'ProtectionPlan' WHERE "AppliesTo" = 'protection_plan';
UPDATE "PromoCodes" SET "AppliesTo" = 'RentalAmount' WHERE "AppliesTo" = 'rental_amount';
UPDATE "PromoCodes" SET "AppliesTo" = 'Commission' WHERE "AppliesTo" = 'commission';

-- Update ReferralRewardType values (if any exist)
UPDATE "PromoCodes" SET "ReferralRewardType" = 'Credit' WHERE "ReferralRewardType" = 'credit';
UPDATE "PromoCodes" SET "ReferralRewardType" = 'CommissionReduction' WHERE "ReferralRewardType" = 'commission_reduction';
UPDATE "PromoCodes" SET "ReferralRewardType" = 'Cash' WHERE "ReferralRewardType" = 'cash';

-- Verify the changes
SELECT "Code", "PromoType", "TargetUserType", "AppliesTo" FROM "PromoCodes" ORDER BY "Code";
