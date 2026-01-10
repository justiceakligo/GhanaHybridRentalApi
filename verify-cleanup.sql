SELECT 'Users' as "Category", COUNT(*) as "Count" FROM "Users"
UNION ALL
SELECT 'Vehicles', COUNT(*) FROM "Vehicles"
UNION ALL
SELECT 'Bookings', COUNT(*) FROM "Bookings"
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM "PaymentTransactions"
UNION ALL
SELECT 'Payouts', COUNT(*) FROM "Payouts"
UNION ALL
SELECT 'Reviews', COUNT(*) FROM "Reviews"
UNION ALL
SELECT 'OwnerProfiles', COUNT(*) FROM "OwnerProfiles"
UNION ALL
SELECT 'RenterProfiles', COUNT(*) FROM "RenterProfiles"
UNION ALL
SELECT 'DriverProfiles', COUNT(*) FROM "DriverProfiles";

SELECT '=== Admin User ===' as "Status", "Email", "Role", "FirstName", "LastName" 
FROM "Users" WHERE "Email" = 'cyriladmin@ryverental.com';
