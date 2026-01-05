-- Update customer template subject only
UPDATE "EmailTemplates" 
SET "Subject" = 'Booking Reserved - {{booking_reference}}', 
    "UpdatedAt" = NOW() 
WHERE "TemplateName" = 'booking_confirmation_customer';
