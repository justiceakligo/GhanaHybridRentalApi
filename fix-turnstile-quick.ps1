# Quick fix for Turnstile configuration
# This connects to your PostgreSQL database and adds the test Turnstile key

$dbHost = "ryve-postgres-new.postgres.database.azure.com"
$dbName = "ghanarentaldb_new"
$dbUser = "ryveadmin"
$dbPassword = "RyveDb@2025!Secure#123"

# SQL to insert/update Turnstile production key
$sql = @"
INSERT INTO "GlobalSettings" ("Key", "ValueJson")
VALUES ('Turnstile:SecretKey', '""0x4AAAAAACIPSC_TH2Gf07f9""')
ON CONFLICT ("Key") DO UPDATE 
SET "ValueJson" = '""0x4AAAAAACIPSC_TH2Gf07f9""';

SELECT "Key", "ValueJson" FROM "GlobalSettings" WHERE "Key" = 'Turnstile:SecretKey';
"@

Write-Host "Updating Turnstile configuration..." -ForegroundColor Cyan

# Using psql (requires PostgreSQL client installed)
$env:PGPASSWORD = $dbPassword
psql -h $dbHost -U $dbUser -d $dbName -c $sql

Write-Host "`nTurnstile production key configured. Login should now work!" -ForegroundColor Green
