-- Fix emoji encoding in email templates
-- Replace corrupted UTF-8 emoji characters with proper ones

UPDATE "EmailTemplates"
SET "BodyTemplate" = 
    -- Fix checkmark
    REPLACE(
    -- Fix car
    REPLACE(
    -- Fix calendar  
    REPLACE(
    -- Fix credit card
    REPLACE(
    -- Fix shield
    REPLACE(
    -- Fix chart
    REPLACE(
    -- Fix lock
    REPLACE(
    -- Fix gift
    REPLACE(
    -- Fix phone
    REPLACE(
    -- Fix home
    REPLACE(
    -- Fix warning
    REPLACE(
    -- Fix location
    REPLACE("BodyTemplate", 
        E'\u00C3\u00A2\u009C\u0085', E'\u2705'),  -- ✅
        E'\u00C3\u00B0\u009F\u009A\u0097', E'\uD83D\uDE97'),  -- 🚗
        E'\u00C3\u00B0\u009F\u0093\u0085', E'\uD83D\uDCC5'),  -- 📅
        E'\u00C3\u00B0\u009F\u0092\u00B3', E'\uD83D\uDCB3'),  -- 💳
        E'\u00C3\u00B0\u009F\u009B\u00A1\u00C3\u00AF\u00C2\u00B8\u008F', E'\uD83D\uDEE1\uFE0F'),  -- 🛡️
        E'\u00C3\u00B0\u009F\u0093\u008A', E'\uD83D\uDCCA'),  -- 📊
        E'\u00C3\u00B0\u009F\u0094\u0092', E'\uD83D\uDD12'),  -- 🔒
        E'\u00C3\u00B0\u009F\u008E\u0081', E'\uD83C\uDF81'),  -- 🎁
        E'\u00C3\u00B0\u009F\u0093\u009E', E'\uD83D\uDCDE'),  -- 📞
        E'\u00C3\u00B0\u009F\u008F\u00A0', E'\uD83C\uDFE0'),  -- 🏠
        E'\u00C3\u00A2\u009A\u00A0\u00C3\u00AF\u00C2\u00B8\u008F', E'\u26A0\uFE0F'),  -- ⚠️
        E'\u00C3\u00B0\u009F\u0093\u008D', E'\uD83D\uDCCD'),  -- 📍
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "TemplateName" IN ('booking_confirmed', 'booking_confirmation_owner')
AND "IsActive" = true;
