-- Migration: Add Partner Booking Tracking
-- Date: 2026-01-11
-- Purpose: Track partner bookings and settlements separately from direct bookings

-- Add partner tracking columns to Bookings table
ALTER TABLE "Bookings" 
ADD COLUMN IF NOT EXISTS "PaymentChannel" varchar(32) DEFAULT 'direct',
ADD COLUMN IF NOT EXISTS "PartnerSettlementStatus" varchar(32),
ADD COLUMN IF NOT EXISTS "IntegrationPartnerId" uuid;

-- Add foreign key constraint
ALTER TABLE "Bookings"
ADD CONSTRAINT "FK_Bookings_IntegrationPartners_IntegrationPartnerId"
FOREIGN KEY ("IntegrationPartnerId") REFERENCES "IntegrationPartners" ("Id")
ON DELETE SET NULL;

-- Create index for partner bookings
CREATE INDEX IF NOT EXISTS "IX_Bookings_IntegrationPartnerId" ON "Bookings" ("IntegrationPartnerId");
CREATE INDEX IF NOT EXISTS "IX_Bookings_PaymentChannel" ON "Bookings" ("PaymentChannel");
CREATE INDEX IF NOT EXISTS "IX_Bookings_PartnerSettlementStatus" ON "Bookings" ("PartnerSettlementStatus");

-- Create PartnerSettlements table
CREATE TABLE IF NOT EXISTS "PartnerSettlements" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "IntegrationPartnerId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "SettlementPeriodStart" timestamp with time zone NOT NULL,
    "SettlementPeriodEnd" timestamp with time zone NOT NULL,
    "BookingReference" varchar(64),
    "TotalAmount" numeric(18,2) NOT NULL,
    "CommissionPercent" numeric(5,2) NOT NULL,
    "CommissionAmount" numeric(18,2) NOT NULL,
    "SettlementAmount" numeric(18,2) NOT NULL,
    "Status" varchar(32) NOT NULL DEFAULT 'pending',
    "DueDate" timestamp with time zone,
    "PaidDate" timestamp with time zone,
    "PaymentReference" varchar(256),
    "PaymentMethod" varchar(64),
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "FK_PartnerSettlements_IntegrationPartners_IntegrationPartnerId" 
        FOREIGN KEY ("IntegrationPartnerId") REFERENCES "IntegrationPartners" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PartnerSettlements_Bookings_BookingId" 
        FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
);

-- Create indexes for PartnerSettlements
CREATE INDEX "IX_PartnerSettlements_IntegrationPartnerId" ON "PartnerSettlements" ("IntegrationPartnerId");
CREATE INDEX "IX_PartnerSettlements_BookingId" ON "PartnerSettlements" ("BookingId");
CREATE INDEX "IX_PartnerSettlements_Status" ON "PartnerSettlements" ("Status");
CREATE INDEX "IX_PartnerSettlements_DueDate" ON "PartnerSettlements" ("DueDate");

-- Update existing bookings to have 'direct' payment channel
UPDATE "Bookings" 
SET "PaymentChannel" = 'direct' 
WHERE "PaymentChannel" IS NULL;

-- Add commission configuration to IntegrationPartners if not exists
ALTER TABLE "IntegrationPartners"
ADD COLUMN IF NOT EXISTS "CommissionPercent" numeric(5,2) DEFAULT 15.00,
ADD COLUMN IF NOT EXISTS "SettlementTermDays" integer DEFAULT 30,
ADD COLUMN IF NOT EXISTS "AutoConfirmBookings" boolean DEFAULT true;

COMMENT ON COLUMN "Bookings"."PaymentChannel" IS 'Payment channel: direct (customer pays us) or partner (customer pays partner)';
COMMENT ON COLUMN "Bookings"."PartnerSettlementStatus" IS 'Partner settlement status: null (direct booking), pending, paid, overdue';
COMMENT ON COLUMN "Bookings"."IntegrationPartnerId" IS 'Integration partner if booking came through partner API';

COMMENT ON TABLE "PartnerSettlements" IS 'Tracks financial settlements with integration partners';
COMMENT ON COLUMN "PartnerSettlements"."SettlementAmount" IS 'Amount partner owes us (TotalAmount - CommissionAmount)';
COMMENT ON COLUMN "PartnerSettlements"."Status" IS 'Settlement status: pending, paid, overdue, cancelled';
