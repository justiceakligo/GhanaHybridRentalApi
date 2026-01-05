-- Add DeletedAt column to Vehicles table for soft delete functionality
-- This preserves all bookings and revenue history when vehicles are "deleted"

ALTER TABLE "Vehicles" 
ADD COLUMN "DeletedAt" timestamp without time zone NULL;

-- Create index for better query performance on active vehicles
CREATE INDEX "IX_Vehicles_DeletedAt" ON "Vehicles" ("DeletedAt") 
WHERE "DeletedAt" IS NULL;

COMMENT ON COLUMN "Vehicles"."DeletedAt" IS 'Soft delete timestamp. NULL = active, NOT NULL = deleted but data preserved for audit/history';
