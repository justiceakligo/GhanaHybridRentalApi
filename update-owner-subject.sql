-- Update owner template subject
UPDATE "EmailTemplates" 
SET "Subject" = 'New Booking Reserved - {{booking_reference}}', 
    "UpdatedAt" = NOW() 
WHERE "TemplateName" = 'booking_confirmation_owner';
