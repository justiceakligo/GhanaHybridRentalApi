-- Migration: Add Insurance System and Canada Support
-- Date: 2026-03-05

-- ============================================
-- PHASE 1 & 2: Add Insurance Infrastructure
-- ============================================

-- Add certificate URL to existing Bookings table
ALTER TABLE "Bookings" 
ADD COLUMN IF NOT EXISTS "InsuranceCertificateUrl" varchar(500) NULL;

-- Create Country Insurance Configuration table
CREATE TABLE IF NOT EXISTS "CountryInsuranceConfig" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "CountryId" uuid NOT NULL,
    "RequiresRealInsurance" boolean NOT NULL DEFAULT false,
    "InsuranceProviderName" varchar(255) NULL,
    "InsuranceProviderApiUrl" varchar(500) NULL,
    "AutoIssuePolicy" boolean NOT NULL DEFAULT false,
    "MinimumLiabilityAmount" decimal NULL,
    "ConfigJson" text NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT now(),
    FOREIGN KEY ("CountryId") REFERENCES "Countries" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_CountryInsuranceConfig_CountryId" 
ON "CountryInsuranceConfig" ("CountryId");

-- Create Insurance Policies table
CREATE TABLE IF NOT EXISTS "InsurancePolicies" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "BookingId" uuid NOT NULL,
    "ProtectionPlanId" uuid NULL,
    "CountryCode" varchar(2) NOT NULL,
    "PolicyNumber" varchar(255) NOT NULL,
    "InsuranceProviderName" varchar(255) NOT NULL,
    "CoverageStartDate" timestamp without time zone NOT NULL,
    "CoverageEndDate" timestamp without time zone NOT NULL,
    "PremiumAmount" decimal NOT NULL DEFAULT 0,
    "LiabilityCoverage" decimal NULL,
    "CertificateUrl" varchar(500) NULL,
    "ProviderPolicyJson" text NULL,
    "Status" varchar(50) NOT NULL DEFAULT 'issued',
    "IssuedAt" timestamp without time zone NOT NULL DEFAULT now(),
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT now(),
    FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_InsurancePolicies_BookingId" 
ON "InsurancePolicies" ("BookingId");

CREATE INDEX IF NOT EXISTS "IX_InsurancePolicies_PolicyNumber" 
ON "InsurancePolicies" ("PolicyNumber");

CREATE INDEX IF NOT EXISTS "IX_InsurancePolicies_Status" 
ON "InsurancePolicies" ("Status");

-- ============================================
-- PHASE 3: Add Canada
-- ============================================

-- Add Canada as a country
INSERT INTO "Countries" 
("Id", "Code", "Name", "CurrencyCode", "CurrencySymbol", "PhoneCode", 
 "Timezone", "DefaultLanguage", "IsActive", "IsDefault", "PaymentProvidersJson", 
 "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'CA',
    'Canada',
    'CAD',
    '$',
    '+1',
    'America/Toronto',
    'en-CA',
    false,  -- Start inactive
    false,
    '["stripe"]',
    now(),
    now()
)
ON CONFLICT ("Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "UpdatedAt" = now();

-- Add Canadian cities (Ottawa, Toronto, Calgary)
INSERT INTO "Cities" ("Id", "Name", "Region", "CountryCode", "CountryId", "IsActive", "DisplayOrder", "DefaultDeliveryFee", "CreatedAt")
SELECT 
    gen_random_uuid(),
    'Toronto',
    'Ontario',
    'CA',
    (SELECT "Id" FROM "Countries" WHERE "Code" = 'CA'),
    true,
    1,
    15.00,
    now()
WHERE NOT EXISTS (SELECT 1 FROM "Cities" WHERE "Name" = 'Toronto' AND "CountryCode" = 'CA')
UNION ALL
SELECT 
    gen_random_uuid(),
    'Ottawa',
    'Ontario',
    'CA',
    (SELECT "Id" FROM "Countries" WHERE "Code" = 'CA'),
    true,
    2,
    18.00,
    now()
WHERE NOT EXISTS (SELECT 1 FROM "Cities" WHERE "Name" = 'Ottawa' AND "CountryCode" = 'CA')
UNION ALL
SELECT 
    gen_random_uuid(),
    'Calgary',
    'Alberta',
    'CA',
    (SELECT "Id" FROM "Countries" WHERE "Code" = 'CA'),
    true,
    3,
    20.00,
    now()
WHERE NOT EXISTS (SELECT 1 FROM "Cities" WHERE "Name" = 'Calgary' AND "CountryCode" = 'CA');

-- ============================================
-- Configure Insurance Requirements
-- ============================================

-- Ghana: No real insurance needed (current system)
INSERT INTO "CountryInsuranceConfig" 
("CountryId", "RequiresRealInsurance", "InsuranceProviderName", "AutoIssuePolicy", "ConfigJson", "CreatedAt")
SELECT 
    "Id",
    false,  -- Ghana doesn't need real insurance API calls
    'RyveRental Protection',
    false,
    '{"certificateTemplate": "ghana", "insuranceNote": "Vehicle owner maintains comprehensive insurance coverage as required by RyveRental listing policy."}'::text,
    now()
FROM "Countries" 
WHERE "Code" = 'GH'
ON CONFLICT DO NOTHING;

-- Nigeria: Basic insurance (ready for future provider)
INSERT INTO "CountryInsuranceConfig" 
("CountryId", "RequiresRealInsurance", "InsuranceProviderName", "AutoIssuePolicy", "MinimumLiabilityAmount", "ConfigJson", "CreatedAt")
SELECT 
    "Id",
    false,  -- Start with mock, ready for real provider later
    'RyveRental Protection',
    false,
    500000,  -- NGN 500k minimum
    '{"certificateTemplate": "nigeria", "readyForRealInsurance": true}'::text,
    now()
FROM "Countries" 
WHERE "Code" = 'NG'
ON CONFLICT DO NOTHING;

-- Kenya: Basic insurance (ready for future provider)
INSERT INTO "CountryInsuranceConfig" 
("CountryId", "RequiresRealInsurance", "InsuranceProviderName", "AutoIssuePolicy", "MinimumLiabilityAmount", "ConfigJson", "CreatedAt")
SELECT 
    "Id",
    false,  -- Start with mock, ready for real provider later
    'RyveRental Protection',
    false,
    2000000,  -- KES 2M minimum
    '{"certificateTemplate": "kenya", "readyForRealInsurance": true}'::text,
    now()
FROM "Countries" 
WHERE "Code" = 'KE'
ON CONFLICT DO NOTHING;

-- Canada: Real insurance REQUIRED (Intact Insurance)
INSERT INTO "CountryInsuranceConfig" 
("CountryId", "RequiresRealInsurance", "InsuranceProviderName", "AutoIssuePolicy", "MinimumLiabilityAmount", "ConfigJson", "CreatedAt")
SELECT 
    "Id",
    true,  -- Canada requires real insurance policy
    'Intact Insurance',
    true,
    2000000,  -- $2M CAD minimum (Ontario requirement)
    '{"certificateTemplate": "canada-regulated", "regulatoryCompliance": true, "fsraRequired": true}'::text,
    now()
FROM "Countries" 
WHERE "Code" = 'CA'
ON CONFLICT DO NOTHING;

-- ============================================
-- Set Canada-specific configuration
-- ============================================

-- Configure Canada country settings
UPDATE "Countries" 
SET "ConfigJson" = '{
    "taxRate": 0.13,
    "platformFeePercentage": 15,
    "supportEmail": "support@ryverental.ca",
    "supportPhone": "+1-800-RYVE-RENT",
    "minBookingAmount": 50,
    "businessHours": "Mon-Fri 9AM-5PM EST",
    "insuranceRequired": true,
    "p2pRentalEndorsementRequired": true
}'::text
WHERE "Code" = 'CA';

-- ============================================
-- Comments and Documentation
-- ============================================

COMMENT ON TABLE "CountryInsuranceConfig" IS 'Stores insurance requirements and configuration per country';
COMMENT ON COLUMN "CountryInsuranceConfig"."RequiresRealInsurance" IS 'If true, real insurance policy must be issued via API';
COMMENT ON COLUMN "CountryInsuranceConfig"."AutoIssuePolicy" IS 'If true, automatically issue policy on booking creation';

COMMENT ON TABLE "InsurancePolicies" IS 'Stores actual insurance policies issued for bookings';
COMMENT ON COLUMN "InsurancePolicies"."PolicyNumber" IS 'Policy number from insurance provider';
COMMENT ON COLUMN "InsurancePolicies"."Status" IS 'Policy status: issued, active, expired, cancelled';
COMMENT ON COLUMN "InsurancePolicies"."ProviderPolicyJson" IS 'Full policy details from insurance provider API';

-- ============================================
-- Verification Queries
-- ============================================

-- Verify Canada was created
SELECT * FROM "Countries" WHERE "Code" = 'CA';

-- Verify Canadian cities
SELECT * FROM "Cities" WHERE "CountryCode" = 'CA';

-- Verify insurance configurations
SELECT 
    c."Code",
    c."Name",
    ic."RequiresRealInsurance",
    ic."InsuranceProviderName",
    ic."MinimumLiabilityAmount"
FROM "Countries" c
LEFT JOIN "CountryInsuranceConfig" ic ON c."Id" = ic."CountryId"
ORDER BY c."Code";
