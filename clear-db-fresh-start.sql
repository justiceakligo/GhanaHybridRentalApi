-- Clear Database for Fresh Start
-- Keeps only cyriladmin@ryverental.com
-- Date: 2026-01-10

BEGIN;

-- Step 1: Clear all booking-related data
DELETE FROM "Inspections";
DELETE FROM "PaymentTransactions";
DELETE FROM "Bookings";
DELETE FROM "Notifications";

-- Step 2: Clear all vehicle-related data
DELETE FROM "Vehicles";

-- Step 3: Clear all user-related data except admin
-- Delete related data first
DELETE FROM "Documents";
DELETE FROM "RenterProfiles";
DELETE FROM "OwnerProfiles";

-- Delete all users except admin
DELETE FROM "Users" WHERE "Email" != 'cyriladmin@ryverental.com';

COMMIT;

-- Verification query
SELECT 
    (SELECT COUNT(*) FROM "Users") as "TotalUsers",
    (SELECT COUNT(*) FROM "Bookings") as "TotalBookings",
    (SELECT COUNT(*) FROM "Vehicles") as "TotalVehicles",
    (SELECT COUNT(*) FROM "PaymentTransactions") as "TotalPayments",
    (SELECT "Email" FROM "Users" LIMIT 1) as "RemainingUser";
