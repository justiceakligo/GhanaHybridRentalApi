-- Fix bookings with invalid CreatedAt dates (0001-01-01)
-- Set them to a reasonable date based on their booking reference year
UPDATE "Bookings"
SET "CreatedAt" = 
    CASE 
        WHEN "BookingReference" LIKE 'RV-2026-%' THEN '2026-01-01 00:00:00'::timestamp
        WHEN "BookingReference" LIKE 'RV-2025-%' THEN '2025-12-01 00:00:00'::timestamp
        ELSE NOW() - INTERVAL '30 days'
    END,
    "UpdatedAt" = NOW()
WHERE "CreatedAt" < '2020-01-01'::timestamp;

-- Show updated bookings
SELECT "BookingReference", "Status", "CreatedAt", "PaymentStatus" 
FROM "Bookings" 
ORDER BY "CreatedAt" DESC;
