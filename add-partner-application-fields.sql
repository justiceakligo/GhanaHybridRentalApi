-- Add API key expiry and application fields to IntegrationPartners table

-- Add API key expiry
ALTER TABLE "IntegrationPartners" 
ADD COLUMN "ApiKeyExpiresAt" timestamp with time zone NULL;

-- Add application/contact information fields
ALTER TABLE "IntegrationPartners" 
ADD COLUMN "ContactPerson" varchar(512) NULL,
ADD COLUMN "Email" varchar(256) NULL,
ADD COLUMN "Phone" varchar(32) NULL,
ADD COLUMN "Website" varchar(512) NULL,
ADD COLUMN "RegistrationNumber" varchar(128) NULL,
ADD COLUMN "Description" varchar(2000) NULL,
ADD COLUMN "ApplicationReference" varchar(64) NULL,
ADD COLUMN "AdminNotes" text NULL;

-- Create index on ApplicationReference for quick lookups
CREATE INDEX "IX_IntegrationPartners_ApplicationReference" 
ON "IntegrationPartners" ("ApplicationReference");

-- Create index on Email for duplicate checking
CREATE INDEX "IX_IntegrationPartners_Email" 
ON "IntegrationPartners" ("Email");

-- Create index on ApiKeyExpiresAt for expiry checking
CREATE INDEX "IX_IntegrationPartners_ApiKeyExpiresAt" 
ON "IntegrationPartners" ("ApiKeyExpiresAt");

COMMENT ON COLUMN "IntegrationPartners"."ApiKeyExpiresAt" IS 'API key expiry date (null = no expiry)';
COMMENT ON COLUMN "IntegrationPartners"."ApplicationReference" IS 'Application reference number (e.g., PA-2026-001234)';
COMMENT ON COLUMN "IntegrationPartners"."AdminNotes" IS 'Admin notes about partner application/account';
