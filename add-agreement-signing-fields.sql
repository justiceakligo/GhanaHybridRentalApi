-- Add agreement signing fields to Bookings table
-- This allows tracking when renters sign rental agreements before payment

ALTER TABLE "Bookings"
ADD COLUMN IF NOT EXISTS "AgreementSigned" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS "AgreementSignedAt" timestamp with time zone,
ADD COLUMN IF NOT EXISTS "AgreementSignedBy" character varying(256),
ADD COLUMN IF NOT EXISTS "AgreementIpAddress" character varying(64),
ADD COLUMN IF NOT EXISTS "AgreementSignatureData" text;

-- Create an index on AgreementSigned for faster queries
CREATE INDEX IF NOT EXISTS "IX_Bookings_AgreementSigned" ON "Bookings" ("AgreementSigned");

-- Add a comment to the table explaining the new fields
COMMENT ON COLUMN "Bookings"."AgreementSigned" IS 'Whether the rental agreement has been digitally signed';
COMMENT ON COLUMN "Bookings"."AgreementSignedAt" IS 'Timestamp when the agreement was signed';
COMMENT ON COLUMN "Bookings"."AgreementSignedBy" IS 'Name of the person who signed the agreement';
COMMENT ON COLUMN "Bookings"."AgreementIpAddress" IS 'IP address from which the agreement was signed';
COMMENT ON COLUMN "Bookings"."AgreementSignatureData" IS 'Base64 signature image or digital acceptance token';
