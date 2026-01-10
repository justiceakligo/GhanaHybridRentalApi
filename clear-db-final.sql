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

-- Delete all notifications
DELETE FROM "Notifications";

-- Delete all referrals
DELETE FROM "Referrals";

-- Delete all OTP codes
DELETE FROM "OtpCodes";

-- Delete all profile change audits
DELETE FROM "ProfileChangeAudits";

-- Delete all driver profiles
DELETE FROM "DriverProfiles";

-- Delete all renter profiles
DELETE FROM "RenterProfiles";

-- Delete all owner profiles
DELETE FROM "OwnerProfiles";

-- Delete all users except cyriladmin@ryverental.com
DELETE FROM "Users" 
WHERE "Email" != 'cyriladmin@ryverental.com';

-- Delete all reports
DELETE FROM "Reports";

-- Delete all notification jobs
DELETE FROM "NotificationJobs";

-- Delete all partner clicks
DELETE FROM "PartnerClicks";

-- Verification queries
SELECT 'Users' as "Table", COUNT(*) as "Count" FROM "Users";
SELECT 'Vehicles' as "Table", COUNT(*) as "Count" FROM "Vehicles";
SELECT 'Bookings' as "Table", COUNT(*) as "Count" FROM "Bookings";
SELECT 'PaymentTransactions' as "Table", COUNT(*) as "Count" FROM "PaymentTransactions";
SELECT 'Payouts' as "Table", COUNT(*) as "Count" FROM "Payouts";
SELECT 'Admin User Remaining:' as "Info", "Email", "Role" FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
