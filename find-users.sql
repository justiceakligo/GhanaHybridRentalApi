-- Check for users in this database
SELECT "Email", "Role", "Status", "CreatedAt" 
FROM "Users" 
WHERE "Email" IN ('cyriladmin@ryverental.com', 'owner@test.com')
ORDER BY "Email";
