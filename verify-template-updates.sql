-- Verify template updates
SELECT "TemplateName", "Subject", 
    CASE 
        WHEN "BodyTemplate" LIKE '%RESERVED%' THEN 'Contains RESERVED'
        WHEN "BodyTemplate" LIKE '%CONFIRMED%' THEN 'Contains CONFIRMED'
        ELSE 'Unknown'
    END as body_status
FROM "EmailTemplates" 
WHERE "TemplateName" IN ('booking_confirmation_customer', 'booking_confirmation_owner')
ORDER BY "TemplateName";
