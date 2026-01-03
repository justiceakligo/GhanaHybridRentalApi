-- Update email templates to be more user-friendly
-- 1. Update subjects to friendly names
-- 2. Update placeholder labels to be human-readable (no underscores, proper capitalization)

-- Update subjects for all existing templates
UPDATE "EmailTemplates"
SET 
    "Subject" = 'Booking Confirmed - {{Booking Reference}}',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_customer';

UPDATE "EmailTemplates"
SET 
    "Subject" = 'New Booking Received - {{Booking Reference}}',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_owner';

UPDATE "EmailTemplates"
SET 
    "Subject" = 'Trip Completed - {{Booking Reference}}',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_completed_customer';

UPDATE "EmailTemplates"
SET 
    "Subject" = 'Rental Completed - {{Booking Reference}}',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_completed_owner';

UPDATE "EmailTemplates"
SET 
    "Subject" = 'Pickup Reminder - Tomorrow',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'pickup_reminder';

UPDATE "EmailTemplates"
SET 
    "Subject" = 'Return Reminder - Tomorrow',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'return_reminder';

-- Insert configurable support phone into AppConfig
INSERT INTO "AppConfig" ("ConfigKey", "ConfigValue", "IsSensitive", "Scope", "CreatedAt", "UpdatedAt")
VALUES 
    ('support_phone_email', '+233 53 594 4564', false, 'support', NOW(), NOW()),
    ('support_phone_whatsapp', '+233535944564', false, 'support', NOW(), NOW()),
    ('support_email', 'support@ryverental.com', false, 'support', NOW(), NOW())
ON CONFLICT ("ConfigKey") 
DO UPDATE SET 
    "ConfigValue" = EXCLUDED."ConfigValue",
    "UpdatedAt" = NOW();
