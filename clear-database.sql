-- Clear Database - Keep only cyriladmin@ryverental.com
-- WARNING: This will delete all data except the admin user!
-- Execute with caution

BEGIN;

-- Step 1: Delete payment-related data
DELETE FROM "PaymentTransactions";
DELETE FROM "Refunds";

-- Step 2: Delete booking-related data
DELETE FROM "Reviews";
DELETE FROM "Notifications";
DELETE FROM "Bookings";

-- Step 3: Delete vehicle-related data
DELETE FROM "VehicleImages";
DELETE FROM "VehicleDocuments";
DELETE FROM "Vehicles";

-- Step 4: Delete user profiles (keep the admin user's profile)
DELETE FROM "RenterProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

DELETE FROM "OwnerProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Step 5: Delete all users except cyriladmin@ryverental.com
DELETE FROM "Users" 
WHERE "Email" != 'cyriladmin@ryverental.com';

-- Step 6: Reset any sequences/counters if needed
-- (Optional - uncomment if you want to reset auto-increment IDs)
-- ALTER SEQUENCE vehicles_id_seq RESTART WITH 1;
-- ALTER SEQUENCE bookings_id_seq RESTART WITH 1;

COMMIT;

-- Verification queries
SELECT 'Users remaining:' as info, COUNT(*) as count FROM "Users";
SELECT 'Vehicles remaining:' as info, COUNT(*) as count FROM "Vehicles";
SELECT 'Bookings remaining:' as info, COUNT(*) as count FROM "Bookings";
SELECT 'Payments remaining:' as info, COUNT(*) as count FROM "PaymentTransactions";
SELECT 'Admin user:' as info, "Email", "Role" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
