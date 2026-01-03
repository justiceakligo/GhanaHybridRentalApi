-- Check promo codes
SELECT "Code", "PromoType", "DiscountValue", "TargetUserType", "AppliesTo", "IsActive" 
FROM "PromoCodes" 
ORDER BY "CreatedAt" 
LIMIT 10;
