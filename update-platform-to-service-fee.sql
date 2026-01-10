-- Update email templates to use "Service Fee" instead of "Platform Fee"

-- Update all templates that contain "Platform Fee" text
UPDATE "EmailTemplates"
SET "BodyTemplate" = REPLACE("BodyTemplate", 'Platform Fee', 'Service Fee')
WHERE "BodyTemplate" LIKE '%Platform Fee%';

UPDATE "EmailTemplates"
SET "BodyTemplate" = REPLACE("BodyTemplate", 'platform fee', 'service fee')
WHERE "BodyTemplate" LIKE '%platform fee%';

UPDATE "EmailTemplates"
SET "Subject" = REPLACE("Subject", 'Platform Fee', 'Service Fee')
WHERE "Subject" LIKE '%Platform Fee%';

UPDATE "EmailTemplates"
SET "Subject" = REPLACE("Subject", 'platform fee', 'service fee')
WHERE "Subject" LIKE '%platform fee%';

-- Verification
SELECT "TemplateName", "Subject", 
       CASE 
           WHEN "BodyTemplate" LIKE '%Service Fee%' THEN 'Contains Service Fee ✓'
           WHEN "BodyTemplate" LIKE '%Platform Fee%' THEN 'Still has Platform Fee ✗'
           ELSE 'No fee reference'
       END as "Status"
FROM "EmailTemplates"
WHERE "BodyTemplate" LIKE '%Fee%'
ORDER BY "TemplateName";
