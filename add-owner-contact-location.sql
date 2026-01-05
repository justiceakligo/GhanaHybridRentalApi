-- Add owner contact and location fields to OwnerProfile
-- Critical for renters to contact owners and know pickup/return locations

ALTER TABLE "OwnerProfiles"
ADD COLUMN IF NOT EXISTS "BusinessPhone" VARCHAR(32),
ADD COLUMN IF NOT EXISTS "BusinessAddress" TEXT,
ADD COLUMN IF NOT EXISTS "GpsAddress" VARCHAR(128),
ADD COLUMN IF NOT EXISTS "PickupInstructions" TEXT,
ADD COLUMN IF NOT EXISTS "City" VARCHAR(128),
ADD COLUMN IF NOT EXISTS "Region" VARCHAR(128);

COMMENT ON COLUMN "OwnerProfiles"."BusinessPhone" IS 'Owner/business phone for renter contact';
COMMENT ON COLUMN "OwnerProfiles"."BusinessAddress" IS 'Physical address for vehicle pickup/return';
COMMENT ON COLUMN "OwnerProfiles"."GpsAddress" IS 'Ghana GPS address (e.g., GA-123-4567)';
COMMENT ON COLUMN "OwnerProfiles"."PickupInstructions" IS 'Special instructions for vehicle pickup';
COMMENT ON COLUMN "OwnerProfiles"."City" IS 'City where owner operates';
COMMENT ON COLUMN "OwnerProfiles"."Region" IS 'Region/State where owner operates';

-- Verify the additions
SELECT column_name, data_type, character_maximum_length 
FROM information_schema.columns 
WHERE table_name = 'OwnerProfiles' 
  AND column_name IN ('BusinessPhone', 'BusinessAddress', 'GpsAddress', 'PickupInstructions', 'City', 'Region')
ORDER BY column_name;
