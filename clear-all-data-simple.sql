-- ============================================================================
-- CLEAR ALL DATA - KEEP ONLY cyriladmin@ryverental.com
-- ============================================================================

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

-- Delete vehicles
DELETE FROM "Vehicles";

-- Delete all documents
DELETE FROM "Documents";

-- Delete payouts
DELETE FROM "Payouts";

-- Delete notifications (keep admin's)
DELETE FROM "Notifications" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete referrals (keep admin's)
DELETE FROM "Referrals" 
WHERE "ReferrerId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
)
AND "ReferredUserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete OTP codes (keep admin's)
DELETE FROM "OtpCodes" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete profile change audits (keep admin's)
DELETE FROM "ProfileChangeAudits" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete driver profiles (keep admin's)
DELETE FROM "DriverProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete renter profiles (keep admin's)
DELETE FROM "RenterProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete owner profiles (keep admin's)
DELETE FROM "OwnerProfiles" 
WHERE "UserId" NOT IN (
    SELECT "Id" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com'
);

-- Delete all users except cyriladmin@ryverental.com
DELETE FROM "Users" 
WHERE "Email" != 'cyriladmin@ryverental.com';

-- Delete reports
DELETE FROM "Reports";

-- Delete notification jobs
DELETE FROM "NotificationJobs";

-- Delete partner clicks
DELETE FROM "PartnerClicks";

-- Verification queries
SELECT 'Users' as "Table", COUNT(*) as "Count" FROM "Users";
SELECT 'Vehicles' as "Table", COUNT(*) as "Count" FROM "Vehicles";
SELECT 'Bookings' as "Table", COUNT(*) as "Count" FROM "Bookings";
SELECT 'PaymentTransactions' as "Table", COUNT(*) as "Count" FROM "PaymentTransactions";
SELECT 'Payouts' as "Table", COUNT(*) as "Count" FROM "Payouts";
SELECT 'Admin User' as "Info", "Email", "Role" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
