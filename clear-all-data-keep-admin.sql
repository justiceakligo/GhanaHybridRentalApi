-- ============================================================================
-- CLEAR ALL DATA - KEEP ONLY cyriladmin@ryverental.com
-- ============================================================================
-- WARNING: This will DELETE ALL DATA except the admin user!
-- Execute this script with extreme caution in production environments
-- ============================================================================

BEGIN;

-- Get the admin user ID for reference
DO $$
DECLARE
    admin_user_id UUID;
BEGIN
    SELECT "Id" INTO admin_user_id FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
    RAISE NOTICE 'Admin User ID: %', admin_user_id;
END $$;

-- ============================================================================
-- STEP 1: Delete booking-related data
-- ============================================================================
RAISE NOTICE 'Deleting booking-related data...';

-- Delete booking charges
DELETE FROM "BookingCharges";

-- Delete rental agreement acceptances
DELETE FROM "RentalAgreementAcceptances";

-- Delete reviews
DELETE FROM "Reviews";

-- Delete inspections
DELETE FROM "Inspections";

-- Delete payment transactions
DELETE FROM "PaymentTransactions";

-- Delete bookings
DELETE FROM "Bookings";

RAISE NOTICE 'Booking-related data deleted.';

-- ============================================================================
-- STEP 2: Delete vehicle-related data
-- ============================================================================
RAISE NOTICE 'Deleting vehicle-related data...';

-- Delete documents related to vehicles
DELETE FROM "Documents" WHERE "EntityType" = 'vehicle';

-- Delete vehicles
DELETE FROM "Vehicles";

RAISE NOTICE 'Vehicle-related data deleted.';

-- ============================================================================
-- STEP 3: Delete earnings and payout data
-- ============================================================================
RAISE NOTICE 'Deleting earnings and payout data...';

-- Delete payouts
DELETE FROM "Payouts";

RAISE NOTICE 'Earnings and payout data deleted.';

-- ============================================================================
-- STEP 4: Delete user-related data (except admin)
-- ============================================================================
RAISE NOTICE 'Deleting user-related data (keeping admin)...';

-- Delete notifications
DELETE FROM "Notifications" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete referrals
DELETE FROM "Referrals" 
WHERE "ReferrerId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
)
AND "ReferredUserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete OTP codes
DELETE FROM "OtpCodes" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete documents related to users
DELETE FROM "Documents" 
WHERE "EntityType" = 'user' 
AND "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete profile change audits
DELETE FROM "ProfileChangeAudits" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete driver profiles
DELETE FROM "DriverProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete renter profiles
DELETE FROM "RenterProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete owner profiles
DELETE FROM "OwnerProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete all users except cyriladmin@ryverental.com
DELETE FROM "Users" 
WHERE "Email" != 'cyriladmin@ryverental.com';

RAISE NOTICE 'User-related data deleted (admin preserved).';

-- ============================================================================
-- STEP 5: Delete miscellaneous data
-- ============================================================================
RAISE NOTICE 'Deleting miscellaneous data...';

-- Delete reports
DELETE FROM "Reports";

-- Delete notification jobs
DELETE FROM "NotificationJobs";

-- Delete partner clicks
DELETE FROM "PartnerClicks";

RAISE NOTICE 'Miscellaneous data deleted.';

-- ============================================================================
-- COMMIT TRANSACTION
-- ============================================================================
COMMIT;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
RAISE NOTICE '============================================';
RAISE NOTICE 'DATABASE CLEANUP COMPLETED';
RAISE NOTICE '============================================';

SELECT 'Users remaining:' as "Info", COUNT(*) as "Count" FROM "Users";
SELECT 'Admin user:' as "Info", "Email", "Role", "FirstName", "LastName" 
FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';

SELECT 'Vehicles remaining:' as "Info", COUNT(*) as "Count" FROM "Vehicles";
SELECT 'Bookings remaining:' as "Info", COUNT(*) as "Count" FROM "Bookings";
SELECT 'Payments remaining:' as "Info", COUNT(*) as "Count" FROM "PaymentTransactions";
SELECT 'Payouts remaining:' as "Info", COUNT(*) as "Count" FROM "Payouts";
SELECT 'Reviews remaining:' as "Info", COUNT(*) as "Count" FROM "Reviews";
SELECT 'Owner Profiles remaining:' as "Info", COUNT(*) as "Count" FROM "OwnerProfiles";
SELECT 'Renter Profiles remaining:' as "Info", COUNT(*) as "Count" FROM "RenterProfiles";
SELECT 'Driver Profiles remaining:' as "Info", COUNT(*) as "Count" FROM "DriverProfiles";

RAISE NOTICE '============================================';
RAISE NOTICE 'Database is now clean and ready for fresh data';
RAISE NOTICE 'Only cyriladmin@ryverental.com user remains';
RAISE NOTICE '============================================';
