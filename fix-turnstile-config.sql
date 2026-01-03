-- Check current Turnstile configuration in GlobalSettings table
SELECT "Key", "ValueJson" 
FROM "GlobalSettings" 
WHERE "Key" LIKE '%Turnstile%' OR "Key" LIKE '%turnstile%';

-- OPTION 1: Use Cloudflare's ALWAYS-PASS test key (for testing only)
-- This will allow all logins to pass Turnstile verification
-- Site key (use in frontend): 1x00000000000000000000AA
-- Secret key (use in backend): 1x0000000000000000000000000000000AA

INSERT INTO "GlobalSettings" ("Key", "ValueJson")
VALUES ('Turnstile:SecretKey', '"1x0000000000000000000000000000000AA"')
ON CONFLICT ("Key") DO UPDATE 
SET "ValueJson" = '"1x0000000000000000000000000000000AA"';

-- OPTION 2: Use your production Turnstile secret key
-- Uncomment and replace YOUR_PRODUCTION_SECRET_KEY below:

-- INSERT INTO "GlobalSettings" ("Key", "ValueJson")
-- VALUES ('Turnstile:SecretKey', '"YOUR_PRODUCTION_SECRET_KEY"')
-- ON CONFLICT ("Key") DO UPDATE 
-- SET "ValueJson" = '"YOUR_PRODUCTION_SECRET_KEY"';

-- After updating, verify:
SELECT "Key", "ValueJson" 
FROM "GlobalSettings" 
WHERE "Key" = 'Turnstile:SecretKey';
