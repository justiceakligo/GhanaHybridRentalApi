-- Update booking_confirmation_customer template to include owner contact information
UPDATE "EmailTemplates"
SET "BodyTemplate" = REPLACE(
    "BodyTemplate",
    '</table>
        </div>
        
        <div style="background: #e7f3ff;',
    '</table>
        </div>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #28a745; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #28a745; font-size: 22px;">📞 Owner Contact Information</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Owner:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{owner_name}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Phone:</td>
                    <td style="padding: 10px 0;"><a href="tel:{{owner_phone}}" style="color: #28a745; text-decoration: none; font-weight: bold;">{{owner_phone}}</a></td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Address:</td>
                    <td style="padding: 10px 0;">{{owner_address}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">GPS Address:</td>
                    <td style="padding: 10px 0; font-family: monospace; background: #e8f5e9; padding: 5px 10px; border-radius: 4px;">{{owner_gps_address}}</td>
                </tr>
            </table>
            
            <div style="background: #fff3cd; padding: 15px; margin-top: 15px; border-radius: 8px; border-left: 4px solid #ffc107;">
                <p style="margin: 0; font-size: 14px; color: #856404;">
                    <strong>📍 Pickup Instructions:</strong><br>
                    {{pickup_instructions}}
                </p>
            </div>
        </div>
        
        <div style="background: #e7f3ff;'
),
"UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmation_customer' 
  AND "BodyTemplate" LIKE '%Next Steps%';

-- Update booking_confirmed template to include owner contact and inspection link
UPDATE "EmailTemplates"
SET "BodyTemplate" = REPLACE(
    "BodyTemplate",
    '<div style="text-align: center;',
    '<div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #28a745; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #28a745; font-size: 22px;">📞 Owner Contact Information</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Owner:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{owner_name}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Phone:</td>
                    <td style="padding: 10px 0;"><a href="tel:{{owner_phone}}" style="color: #28a745; text-decoration: none; font-weight: bold;">{{owner_phone}}</a></td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Address:</td>
                    <td style="padding: 10px 0;">{{owner_address}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">GPS Address:</td>
                    <td style="padding: 10px 0; font-family: monospace; background: #e8f5e9; padding: 5px 10px; border-radius: 4px;">{{owner_gps_address}}</td>
                </tr>
            </table>
            
            <div style="background: #fff3cd; padding: 15px; margin-top: 15px; border-radius: 8px; border-left: 4px solid #ffc107;">
                <p style="margin: 0; font-size: 14px; color: #856404;">
                    <strong>📍 Pickup Instructions:</strong><br>
                    {{pickup_instructions}}
                </p>
            </div>
        </div>
        
        <div style="background: #e3f2fd; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #2196F3; text-align: center;">
            <h3 style="margin: 0 0 15px 0; color: #1976D2; font-size: 18px;">🔍 Vehicle Inspection</h3>
            <p style="margin: 0 0 15px 0; color: #333;">Complete the vehicle inspection before pickup</p>
            <a href="{{inspection_link}}" style="display: inline-block; background: #2196F3; color: white; padding: 12px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; font-size: 16px;">Start Inspection</a>
        </div>
        
        <div style="text-align: center;'
),
"UpdatedAt" = NOW()
WHERE "TemplateName" = 'booking_confirmed';

-- Verify updates
SELECT "TemplateName", "Subject", 
    CASE 
        WHEN "BodyTemplate" LIKE '%owner_phone%' THEN 'Has owner contact'
        ELSE 'Missing owner contact'
    END as owner_contact_status,
    CASE
        WHEN "BodyTemplate" LIKE '%inspection_link%' THEN 'Has inspection link'
        ELSE 'Missing inspection link'
    END as inspection_status
FROM "EmailTemplates"
WHERE "TemplateName" IN ('booking_confirmation_customer', 'booking_confirmed');
