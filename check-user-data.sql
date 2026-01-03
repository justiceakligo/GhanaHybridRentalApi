-- Check if Turnstile key exists (to verify this is the right database)
SELECT "Key", "ValueJson" FROM "GlobalSettings" WHERE "Key" = 'Turnstile:SecretKey';

-- Check if users exist
SELECT "Email", "Role", "Status", "CreatedAt" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
