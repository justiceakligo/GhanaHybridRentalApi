-- Add all potentially missing columns to ensure schema is up to date

-- Users table missing columns
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "NotificationPreferencesJson" text NULL;

-- Bookings table - check for ActualPickupDateTime (from earlier error)
ALTER TABLE "Bookings" ADD COLUMN IF NOT EXISTS "ActualPickupDateTime" timestamp with time zone NULL;
ALTER TABLE "Bookings" ADD COLUMN IF NOT EXISTS "ActualReturnDateTime" timestamp with time zone NULL;

-- Verify all columns were added
SELECT 'Users.NotificationPreferencesJson' as column_check, 
       COUNT(*) as exists 
FROM information_schema.columns 
WHERE table_name = 'Users' AND column_name = 'NotificationPreferencesJson'

UNION ALL

SELECT 'Bookings.ActualPickupDateTime' as column_check, 
       COUNT(*) as exists 
FROM information_schema.columns 
WHERE table_name = 'Bookings' AND column_name = 'ActualPickupDateTime'

UNION ALL

SELECT 'Bookings.ActualReturnDateTime' as column_check, 
       COUNT(*) as exists 
FROM information_schema.columns 
WHERE table_name = 'Bookings' AND column_name = 'ActualReturnDateTime';
