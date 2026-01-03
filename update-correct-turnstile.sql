-- Replace YOUR_CORRECT_SECRET_KEY with the actual secret key from Cloudflare
-- Example: 0x4AAAAAACIPSC_TH2Gf07f9_YOUR_FULL_KEY_HERE

INSERT INTO "GlobalSettings" ("Key", "ValueJson")
VALUES ('Turnstile:SecretKey', '"YOUR_CORRECT_SECRET_KEY"')
ON CONFLICT ("Key") DO UPDATE 
SET "ValueJson" = '"YOUR_CORRECT_SECRET_KEY"';

-- Verify
SELECT "Key", "ValueJson" FROM "GlobalSettings" WHERE "Key" = 'Turnstile:SecretKey';
