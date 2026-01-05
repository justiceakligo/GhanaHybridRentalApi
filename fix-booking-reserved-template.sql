-- Fix booking notification templates to say "Booking Reserved" instead of "Booking Confirmed"
-- These templates are sent when a booking is created (BEFORE payment)
-- The booking_confirmed template is sent AFTER successful payment

-- 1. Update customer booking confirmation template
UPDATE "EmailTemplates" 
SET 
    "Subject" = 'Booking Reserved - {{booking_reference}}',
    "BodyTemplate" = '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">🔖 Booking Reserved!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Complete payment to confirm your rental</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #667eea;">{{customer_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Thank you for choosing Ryve Rental! Your booking has been reserved.</p>
        
        <div style="background: #fff3cd; border-left: 5px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 8px;">
            <p style="margin: 0; font-size: 15px; color: #856404;">
                <strong>⚠️ Payment Pending:</strong> Please complete your payment to confirm this booking. Your reservation will be held for a limited time.
            </p>
        </div>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #667eea; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #667eea; font-size: 22px;">📋 Booking Details</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #667eea; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Status:</td>
                    <td style="padding: 10px 0;"><span style="background: #ffc107; color: #333; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">RESERVED - PENDING PAYMENT</span></td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Vehicle:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{vehicle_make}} {{vehicle_model}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Plate Number:</td>
                    <td style="padding: 10px 0;">{{vehicle_plate}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Type:</td>
                    <td style="padding: 10px 0;">{{vehicle_type}}</td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">📅 Pickup:</td>
                    <td style="padding: 10px 0; font-size: 15px;">{{pickup_date}} at {{pickup_time}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">📍 Location:</td>
                    <td style="padding: 10px 0;">{{pickup_location}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">🔙 Return:</td>
                    <td style="padding: 10px 0; font-size: 15px;">{{return_date}} at {{return_time}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">⏱️ Duration:</td>
                    <td style="padding: 10px 0;">{{trip_duration}} days</td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Rental Amount:</td>
                    <td style="padding: 10px 0; text-align: right;">{{currency}} {{rental_amount}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Driver Fee:</td>
                    <td style="padding: 10px 0; text-align: right;">{{currency}} {{driver_amount}}</td>
                </tr>
                <tr>
                    <td style="padding: 15px 0 5px 0; font-weight: bold; color: #333; font-size: 18px; border-top: 2px solid #667eea;">Total Amount:</td>
                    <td style="padding: 15px 0 5px 0; text-align: right; font-size: 20px; color: #667eea; font-weight: bold; border-top: 2px solid #667eea;">{{currency}} {{total_amount}}</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #e7f3ff; border-left: 5px solid #2196F3; padding: 20px; margin: 25px 0; border-radius: 8px;">
            <h3 style="margin: 0 0 15px 0; color: #1976D2; font-size: 18px;">💳 Next Steps</h3>
            <ol style="margin: 0; padding-left: 20px; color: #333;">
                <li style="margin-bottom: 10px;">Complete your payment through the booking portal</li>
                <li style="margin-bottom: 10px;">You will receive a confirmation email once payment is processed</li>
                <li style="margin-bottom: 10px;">Ensure you arrive on time for pickup</li>
                <li style="margin-bottom: 10px;">Bring a valid ID and driver''s license</li>
            </ol>
        </div>
        
        <div style="background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #333; font-size: 18px;">📞 Need Help?</h3>
            <p style="margin: 5px 0; color: #666;">
                Email: <a href="mailto:{{support_email}}" style="color: #667eea; text-decoration: none;">{{support_email}}</a>
            </p>
            <p style="margin: 5px 0; color: #666;">
                Phone: <a href="tel:{{support_phone}}" style="color: #667eea; text-decoration: none;">{{support_phone}}</a>
            </p>
        </div>
        
        <div style="text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0;">
            <p style="color: #999; font-size: 14px; margin: 5px 0;">Thank you for choosing Ryve Rental</p>
            <p style="color: #999; font-size: 14px; margin: 5px 0;">Drive Safe, Drive Happy! 🚗✨</p>
        </div>
        
    </div>
    
    <div style="background: #333; color: #fff; text-align: center; padding: 20px; font-size: 12px;">
        <p style="margin: 5px 0;">© 2024 Ryve Rental. All rights reserved.</p>
        <p style="margin: 5px 0; color: #aaa;">This is an automated message. Please do not reply to this email.</p>
    </div>
    
</body>
</html>',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_customer';

-- 2. Update owner booking notification template to show "RESERVED - PENDING PAYMENT"
UPDATE "EmailTemplates" 
SET 
    "Subject" = 'New Booking Reserved - {{booking_reference}}',
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_owner';

-- Update the status badge in the owner template body
UPDATE "EmailTemplates" 
SET 
    "BodyTemplate" = REPLACE(
        "BodyTemplate",
        '<span style="background: #28a745; color: white; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">CONFIRMED</span>',
        '<span style="background: #ffc107; color: #333; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">RESERVED - PENDING PAYMENT</span>'
    ),
    "UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_owner' 
  AND "BodyTemplate" LIKE '%CONFIRMED%';

-- Verify the updates
SELECT 
    "TemplateName", 
    "Subject", 
    CASE 
        WHEN "BodyTemplate" LIKE '%RESERVED%' THEN 'Updated with RESERVED status'
        ELSE 'Not updated'
    END as status_check,
    "UpdatedAt"
FROM "EmailTemplates" 
WHERE "TemplateName" IN ('booking_confirmation_customer', 'booking_confirmation_owner')
ORDER BY "TemplateName";
