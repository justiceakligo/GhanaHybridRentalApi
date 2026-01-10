-- Migration: Add ReceiptTemplates table for customizable receipts
-- Created: 2026-01-06

-- Create ReceiptTemplates table
CREATE TABLE IF NOT EXISTS "ReceiptTemplates" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "TemplateName" varchar(128) NOT NULL,
    "LogoUrl" varchar(512) NOT NULL DEFAULT 'https://i.imgur.com/ryvepool-logo.png',
    "CompanyName" varchar(256) NOT NULL DEFAULT 'RyvePool',
    "CompanyAddress" varchar(512) NOT NULL DEFAULT 'Accra, Ghana',
    "CompanyPhone" varchar(128) NOT NULL DEFAULT '+233 XX XXX XXXX',
    "CompanyEmail" varchar(256) NOT NULL DEFAULT 'support@ryvepool.com',
    "CompanyWebsite" varchar(512) NOT NULL DEFAULT 'www.ryvepool.com',
    "HeaderTemplate" text NOT NULL DEFAULT '',
    "FooterTemplate" text NOT NULL DEFAULT '',
    "TermsAndConditions" text,
    "CustomCss" text,
    "IsActive" boolean NOT NULL DEFAULT true,
    "ShowLogo" boolean NOT NULL DEFAULT true,
    "ShowQrCode" boolean NOT NULL DEFAULT false,
    "ReceiptNumberPrefix" varchar(50) NOT NULL DEFAULT 'RCT',
    "AvailablePlaceholdersJson" text,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    "CreatedByUserId" uuid
);

-- Add index on IsActive for faster queries
CREATE INDEX IF NOT EXISTS "IX_ReceiptTemplates_IsActive" 
ON "ReceiptTemplates" ("IsActive");

-- Insert default RyvePool template with logo
INSERT INTO "ReceiptTemplates" (
    "Id",
    "TemplateName",
    "LogoUrl",
    "CompanyName",
    "CompanyAddress",
    "CompanyPhone",
    "CompanyEmail",
    "CompanyWebsite",
    "HeaderTemplate",
    "FooterTemplate",
    "TermsAndConditions",
    "CustomCss",
    "IsActive",
    "ShowLogo",
    "ShowQrCode",
    "ReceiptNumberPrefix",
    "AvailablePlaceholdersJson",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'RyvePool Default',
    'https://i.imgur.com/ryvepool-logo.png',
    'RyvePool',
    'Accra, Ghana',
    '+233 XX XXX XXXX',
    'support@ryvepool.com',
    'www.ryvepool.com',
    '',
    '',
    'All rentals are subject to our terms and conditions. Prices include applicable taxes. For support, contact us at support@ryvepool.com',
    NULL,
    true,
    true,
    false,
    'RCT',
    '["{{receiptNumber}}", "{{receiptDate}}", "{{bookingReference}}", "{{customerName}}", "{{customerEmail}}", "{{customerPhone}}", "{{pickupDateTime}}", "{{returnDateTime}}", "{{totalDays}}", "{{vehicleName}}", "{{plateNumber}}", "{{currency}}", "{{vehicleAmount}}", "{{driverAmount}}", "{{insuranceAmount}}", "{{platformFee}}", "{{totalAmount}}", "{{paymentStatus}}", "{{paymentMethod}}"]',
    (now() at time zone 'utc'),
    (now() at time zone 'utc')
)
ON CONFLICT DO NOTHING;

-- Add comment
COMMENT ON TABLE "ReceiptTemplates" IS 'Customizable receipt templates for professional-looking rental receipts';
COMMENT ON COLUMN "ReceiptTemplates"."LogoUrl" IS 'URL to company logo displayed on receipt';
COMMENT ON COLUMN "ReceiptTemplates"."CompanyName" IS 'Company name displayed on receipt header';
COMMENT ON COLUMN "ReceiptTemplates"."TermsAndConditions" IS 'Terms displayed in receipt footer';
COMMENT ON COLUMN "ReceiptTemplates"."CustomCss" IS 'Custom CSS styling for HTML receipts';
COMMENT ON COLUMN "ReceiptTemplates"."IsActive" IS 'Only one template can be active at a time';
COMMENT ON COLUMN "ReceiptTemplates"."ShowLogo" IS 'Whether to display logo on receipt';
COMMENT ON COLUMN "ReceiptTemplates"."ShowQrCode" IS 'Future feature: show QR code for receipt verification';
