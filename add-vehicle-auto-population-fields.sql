-- Add auto-population fields to Vehicles table for features, specifications, and inclusions

-- Add JSON columns for features, specifications, and inclusions
ALTER TABLE "Vehicles"
ADD COLUMN IF NOT EXISTS "FeaturesJson" TEXT NULL,
ADD COLUMN IF NOT EXISTS "SpecificationsJson" TEXT NULL,
ADD COLUMN IF NOT EXISTS "InclusionsJson" TEXT NULL;

-- Add vehicle specification fields
ALTER TABLE "Vehicles"
ADD COLUMN IF NOT EXISTS "FuelType" VARCHAR(50) NULL,
ADD COLUMN IF NOT EXISTS "TransmissionType" VARCHAR(50) NULL,
ADD COLUMN IF NOT EXISTS "SeatingCapacity" INTEGER NULL;

-- Add mileage allowance fields (nullable - defaults to global settings)
ALTER TABLE "Vehicles"
ADD COLUMN IF NOT EXISTS "MileageAllowancePerDay" INTEGER NULL,
ADD COLUMN IF NOT EXISTS "ExtraKmRate" DECIMAL(10,2) NULL;

-- Add comments for documentation
COMMENT ON COLUMN "Vehicles"."FeaturesJson" IS 'JSON array of vehicle features (e.g., ["Air Conditioning", "Bluetooth Audio", "USB Charging"])';
COMMENT ON COLUMN "Vehicles"."SpecificationsJson" IS 'JSON object of vehicle specifications (e.g., {"engineSize": "1.5L", "fuelEfficiency": "15-17 km/L"})';
COMMENT ON COLUMN "Vehicles"."InclusionsJson" IS 'JSON object of rental inclusions specific to this vehicle (overrides can be set here)';
COMMENT ON COLUMN "Vehicles"."FuelType" IS 'Fuel type: Petrol, Diesel, Electric, Hybrid, etc.';
COMMENT ON COLUMN "Vehicles"."TransmissionType" IS 'Transmission type: Manual, Automatic, CVT, etc.';
COMMENT ON COLUMN "Vehicles"."SeatingCapacity" IS 'Number of passenger seats';
COMMENT ON COLUMN "Vehicles"."MileageAllowancePerDay" IS 'Daily mileage allowance in km (NULL uses global default)';
COMMENT ON COLUMN "Vehicles"."ExtraKmRate" IS 'Rate per extra kilometer in GHS (NULL uses global default)';

-- Insert global default settings for mileage allowance
INSERT INTO "GlobalSettings" ("Key", "ValueJson", "Description")
VALUES 
    ('Vehicle:DefaultMileageAllowancePerDay', '600', 'Default daily mileage allowance in kilometers for all vehicles'),
    ('Vehicle:DefaultExtraKmRate', '0.30', 'Default rate per extra kilometer in GHS')
ON CONFLICT ("Key") DO UPDATE SET
    "ValueJson" = EXCLUDED."ValueJson",
    "Description" = EXCLUDED."Description";
