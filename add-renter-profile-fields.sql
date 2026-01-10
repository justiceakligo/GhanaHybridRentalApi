-- Add new fields to RenterProfiles table for v1.230
-- Address and Emergency Contact information

-- Add StreetAddress column
ALTER TABLE "RenterProfiles" 
ADD COLUMN IF NOT EXISTS "StreetAddress" character varying(512);

-- Add City column
ALTER TABLE "RenterProfiles" 
ADD COLUMN IF NOT EXISTS "City" character varying(128);

-- Add EmergencyContactName column
ALTER TABLE "RenterProfiles" 
ADD COLUMN IF NOT EXISTS "EmergencyContactName" character varying(256);

-- Add EmergencyContactPhone column
ALTER TABLE "RenterProfiles" 
ADD COLUMN IF NOT EXISTS "EmergencyContactPhone" character varying(32);

-- Add comments for documentation
COMMENT ON COLUMN "RenterProfiles"."StreetAddress" IS 'Renter street address';
COMMENT ON COLUMN "RenterProfiles"."City" IS 'Renter city';
COMMENT ON COLUMN "RenterProfiles"."EmergencyContactName" IS 'Emergency contact full name';
COMMENT ON COLUMN "RenterProfiles"."EmergencyContactPhone" IS 'Emergency contact phone number';
