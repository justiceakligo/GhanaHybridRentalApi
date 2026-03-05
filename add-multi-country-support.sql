-- Migration: Add Countries table for multi-country support
-- Date: 2026-03-05

-- Create Countries table
CREATE TABLE IF NOT EXISTS "Countries" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" varchar(2) NOT NULL UNIQUE,
    "Name" varchar(128) NOT NULL,
    "CurrencyCode" varchar(3) NOT NULL,
    "CurrencySymbol" varchar(10) NOT NULL DEFAULT '',
    "PhoneCode" varchar(10) NOT NULL DEFAULT '',
    "Timezone" varchar(64) NOT NULL DEFAULT 'UTC',
    "DefaultLanguage" varchar(10) NOT NULL DEFAULT 'en',
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "PaymentProvidersJson" text NULL,
    "ConfigJson" text NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT now()
);

-- Create index on country code for fast lookups
CREATE INDEX IF NOT EXISTS "IX_Countries_Code" ON "Countries" ("Code");
CREATE INDEX IF NOT EXISTS "IX_Countries_IsActive" ON "Countries" ("IsActive");

-- Update Cities table to have proper foreign key relationship to Countries
ALTER TABLE "Cities" ADD COLUMN IF NOT EXISTS "CountryId" uuid NULL;

-- Create foreign key relationship (if Cities exist without country, they'll be NULL)
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Cities_Countries_CountryId'
    ) THEN
        ALTER TABLE "Cities" 
        ADD CONSTRAINT "FK_Cities_Countries_CountryId" 
        FOREIGN KEY ("CountryId") REFERENCES "Countries" ("Id") ON DELETE SET NULL;
    END IF;
END $$;

-- Create index on Cities.CountryId
CREATE INDEX IF NOT EXISTS "IX_Cities_CountryId" ON "Cities" ("CountryId");

-- Seed Ghana as the default country
INSERT INTO "Countries" ("Id", "Code", "Name", "CurrencyCode", "CurrencySymbol", "PhoneCode", "Timezone", "DefaultLanguage", "IsActive", "IsDefault", "PaymentProvidersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'GH',
    'Ghana',
    'GHS',
    '₵',
    '+233',
    'Africa/Accra',
    'en-GH',
    true,
    true,
    '["paystack", "stripe"]',
    now(),
    now()
)
ON CONFLICT ("Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "IsDefault" = EXCLUDED."IsDefault",
    "UpdatedAt" = now();

-- Update existing Cities to link to Ghana
UPDATE "Cities" 
SET "CountryId" = (SELECT "Id" FROM "Countries" WHERE "Code" = 'GH')
WHERE "CountryCode" = 'GH' AND "CountryId" IS NULL;

-- Seed additional countries (inactive by default, ready for expansion)
INSERT INTO "Countries" ("Id", "Code", "Name", "CurrencyCode", "CurrencySymbol", "PhoneCode", "Timezone", "DefaultLanguage", "IsActive", "IsDefault", "PaymentProvidersJson", "CreatedAt", "UpdatedAt")
VALUES 
    (gen_random_uuid(), 'NG', 'Nigeria', 'NGN', '₦', '+234', 'Africa/Lagos', 'en-NG', false, false, '["paystack", "flutterwave"]', now(), now()),
    (gen_random_uuid(), 'KE', 'Kenya', 'KES', 'KSh', '+254', 'Africa/Nairobi', 'en-KE', false, false, '["mpesa", "flutterwave"]', now(), now()),
    (gen_random_uuid(), 'ZA', 'South Africa', 'ZAR', 'R', '+27', 'Africa/Johannesburg', 'en-ZA', false, false, '["paystack", "stripe"]', now(), now()),
    (gen_random_uuid(), 'TZ', 'Tanzania', 'TZS', 'TSh', '+255', 'Africa/Dar_es_Salaam', 'sw-TZ', false, false, '["flutterwave"]', now(), now())
ON CONFLICT ("Code") DO NOTHING;

-- Create AppConfig entries for country-specific settings
-- These can be used to override global settings per country

COMMENT ON TABLE "Countries" IS 'Stores countries where the rental service operates';
COMMENT ON COLUMN "Countries"."Code" IS 'ISO 3166-1 alpha-2 country code';
COMMENT ON COLUMN "Countries"."CurrencyCode" IS 'ISO 4217 currency code';
COMMENT ON COLUMN "Countries"."IsDefault" IS 'Whether this is the default country for routes without country prefix';
COMMENT ON COLUMN "Countries"."PaymentProvidersJson" IS 'Enabled payment providers for this country (JSON array)';
COMMENT ON COLUMN "Countries"."ConfigJson" IS 'Country-specific configuration (JSON object)';
