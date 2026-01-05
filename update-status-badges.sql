-- Update status badges in templates
UPDATE "EmailTemplates" 
SET "BodyTemplate" = REPLACE("BodyTemplate", 
    '<span style="background: #28a745; color: white; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">CONFIRMED</span>',
    '<span style="background: #ffc107; color: #333; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">RESERVED - PENDING PAYMENT</span>'
),
"UpdatedAt" = NOW()
WHERE "TemplateName" IN ('booking_confirmation_customer', 'booking_confirmation_owner');
