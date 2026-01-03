-- Verify the current Turnstile configuration
SELECT "Key", "ValueJson", LENGTH("ValueJson") as length
FROM "GlobalSettings" 
WHERE "Key" = 'Turnstile:SecretKey';
