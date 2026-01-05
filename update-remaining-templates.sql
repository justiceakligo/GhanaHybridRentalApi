UPDATE "EmailTemplates" SET "Subject" = 'Return Reminder - Tomorrow', "UpdatedAt" = NOW() WHERE "TemplateName" = 'return_reminder';
UPDATE "EmailTemplates" SET "Subject" = 'Booking Confirmed', "UpdatedAt" = NOW() WHERE "TemplateName" = 'booking_confirmation_customer';
UPDATE "EmailTemplates" SET "Subject" = 'New Booking Received', "UpdatedAt" = NOW() WHERE "TemplateName" = 'booking_confirmation_owner';
UPDATE "EmailTemplates" SET "Subject" = 'Trip Completed', "UpdatedAt" = NOW() WHERE "TemplateName" = 'booking_completed_customer';
UPDATE "EmailTemplates" SET "Subject" = 'Rental Completed', "UpdatedAt" = NOW() WHERE "TemplateName" = 'booking_completed_owner';
