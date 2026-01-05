-- Find the owner with email owner@test.com and their vehicles
-- First, find the user
SELECT 
    u.id as user_id,
    u.email,
    u.fullname,
    u.role,
    u.createdat
FROM users u
WHERE u.email = 'owner@test.com';

-- Find vehicles associated with this owner
SELECT 
    v.id as vehicle_id,
    v.make,
    v.model,
    v.year,
    v.registrationnumber,
    v.dailyrate,
    v.status,
    v.createdat,
    u.id as owner_id,
    u.email as owner_email,
    u.fullname as owner_name
FROM vehicles v
INNER JOIN users u ON v.ownerid = u.id
WHERE u.email = 'owner@test.com'
ORDER BY v.createdat DESC;

-- Count vehicles for this owner
SELECT 
    u.email,
    u.fullname,
    COUNT(v.id) as total_vehicles,
    COUNT(CASE WHEN v.status = 'active' THEN 1 END) as active_vehicles,
    COUNT(CASE WHEN v.status = 'inactive' THEN 1 END) as inactive_vehicles
FROM users u
LEFT JOIN vehicles v ON v.ownerid = u.id
WHERE u.email = 'owner@test.com'
GROUP BY u.email, u.fullname;
