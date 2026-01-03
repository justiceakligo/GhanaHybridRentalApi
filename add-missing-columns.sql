-- Add missing NotificationPreferencesJson column to Users table
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "NotificationPreferencesJson" text NULL;

-- Verify the column was added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name = 'NotificationPreferencesJson';
