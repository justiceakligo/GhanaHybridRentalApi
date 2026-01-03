-- Professional Email Templates for Ryve Rental
-- Run this script to insert all email templates into the database

-- Delete existing templates to avoid conflicts
DELETE FROM "EmailTemplates" WHERE "TemplateName" IN (
    'booking_confirmation_customer',
    'booking_confirmation_owner',
    'booking_completed_customer',
    'booking_completed_owner',
    'vehicle_returned_customer',
    'vehicle_returned_owner',
    'trip_pickup_reminder',
    'trip_return_reminder',
    'payment_received',
    'welcome_email'
);

-- 1. BOOKING CONFIRMATION - CUSTOMER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'booking_confirmation_customer',
    'Booking Confirmed - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">🎉 Booking Confirmed!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Your rental is ready</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #667eea;">{{customer_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Great news! Your booking has been confirmed and we''re excited to serve you.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #667eea; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #667eea; font-size: 22px;">📋 Booking Details</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #667eea; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Status:</td>
                    <td style="padding: 10px 0;"><span style="background: #28a745; color: white; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">CONFIRMED</span></td>
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
                <tr style="background: #f0f7ff;">
                    <td style="padding: 15px 10px; font-weight: bold; color: #667eea; font-size: 18px;">💰 Total Amount:</td>
                    <td style="padding: 15px 10px; text-align: right; color: #667eea; font-size: 22px; font-weight: bold;">{{currency}} {{total_amount}}</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #856404; font-size: 18px;">📱 Next Steps</h3>
            <ol style="margin: 0; padding-left: 20px; color: #856404;">
                <li style="margin: 8px 0; line-height: 1.6;">Save this confirmation email</li>
                <li style="margin: 8px 0; line-height: 1.6;">Arrive at pickup location on time</li>
                <li style="margin: 8px 0; line-height: 1.6;">Complete vehicle inspection with owner</li>
                <li style="margin: 8px 0; line-height: 1.6;">Enjoy your ride! 🚗</li>
            </ol>
        </div>
        
        <div style="background: #e7f3ff; padding: 20px; border-radius: 8px; border-left: 4px solid #2196F3; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #1976D2; font-size: 18px;">💡 Important Reminders</h3>
            <ul style="margin: 0; padding-left: 20px; color: #1565C0;">
                <li style="margin: 8px 0; line-height: 1.6;">Bring a valid driver''s license and ID</li>
                <li style="margin: 8px 0; line-height: 1.6;">Vehicle inspection is required before departure</li>
                <li style="margin: 8px 0; line-height: 1.6;">Late returns may incur additional charges</li>
                <li style="margin: 8px 0; line-height: 1.6;">Return vehicle with same fuel level</li>
            </ul>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Need Help?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #667eea; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thank you for choosing Ryve Rental! 🚙</p>
            <p style="margin: 10px 0 0 0; font-size: 14px; color: #f0f0f0;">Drive safe, drive smart.</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'booking',
    'Customer booking confirmation email',
    true,
    true,
    '["customer_name","booking_reference","vehicle_make","vehicle_model","vehicle_plate","vehicle_type","pickup_date","pickup_time","pickup_location","return_date","return_time","trip_duration","currency","rental_amount","driver_amount","total_amount","support_email"]',
    NOW(),
    NOW()
);

-- 2. BOOKING CONFIRMATION - OWNER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'booking_confirmation_owner',
    'New Booking Received - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0;">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">💼 New Booking!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">You have a new rental</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #28a745;">{{owner_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Great news! You have received a new booking for your vehicle.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #28a745; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #28a745; font-size: 22px;">📋 Booking Details</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #28a745; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Status:</td>
                    <td style="padding: 10px 0;"><span style="background: #28a745; color: white; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: bold;">CONFIRMED</span></td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <h3 style="margin: 0 0 15px 0; color: #28a745; font-size: 18px;">👤 Customer Information</h3>
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Name:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{customer_name}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Phone:</td>
                    <td style="padding: 10px 0;">{{customer_phone}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Email:</td>
                    <td style="padding: 10px 0;">{{customer_email}}</td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <h3 style="margin: 0 0 15px 0; color: #28a745; font-size: 18px;">🚗 Vehicle Details</h3>
            <table style="width: 100%; border-collapse: collapse;">
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
                <tr style="background: #e8f5e9;">
                    <td style="padding: 15px 10px; font-weight: bold; color: #28a745; font-size: 18px;">💰 Your Earnings:</td>
                    <td style="padding: 15px 10px; text-align: right; color: #28a745; font-size: 22px; font-weight: bold;">{{currency}} {{rental_amount}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px; font-size: 13px; color: #666;" colspan="2">Platform fee and payment processing will be deducted as per your agreement</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #856404; font-size: 18px;">✅ Action Required</h3>
            <ol style="margin: 0; padding-left: 20px; color: #856404;">
                <li style="margin: 8px 0; line-height: 1.6;">Prepare vehicle for pickup</li>
                <li style="margin: 8px 0; line-height: 1.6;">Be available at pickup location on time</li>
                <li style="margin: 8px 0; line-height: 1.6;">Conduct thorough vehicle inspection</li>
                <li style="margin: 8px 0; line-height: 1.6;">Complete pickup checklist</li>
            </ol>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Questions?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #28a745; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thanks for being a Ryve Rental partner! 🤝</p>
            <p style="margin: 10px 0 0 0; font-size: 14px; color: #f0f0f0;">Together, we''re revolutionizing car sharing.</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'booking',
    'Owner notification for new booking',
    true,
    true,
    '["owner_name","booking_reference","customer_name","customer_phone","customer_email","vehicle_make","vehicle_model","vehicle_plate","vehicle_type","pickup_date","pickup_time","pickup_location","return_date","return_time","trip_duration","currency","rental_amount","support_email"]',
    NOW(),
    NOW()
);

-- 3. BOOKING COMPLETED - CUSTOMER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'booking_completed_customer',
    'Trip Completed - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">✅ Trip Completed!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Thank you for your rental</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #17a2b8;">{{customer_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Your rental has been successfully completed! We hope you had a great experience.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #17a2b8; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #17a2b8; font-size: 22px;">📊 Trip Summary</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #17a2b8; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Vehicle:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{vehicle_make}} {{vehicle_model}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Duration:</td>
                    <td style="padding: 10px 0;">{{trip_duration}} days</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Distance Traveled:</td>
                    <td style="padding: 10px 0;">{{distance_traveled}} km</td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <h3 style="margin: 0 0 15px 0; color: #17a2b8; font-size: 18px;">💰 Payment Summary</h3>
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Total Paid:</td>
                    <td style="padding: 10px 0; text-align: right; font-size: 16px;">{{currency}} {{total_amount}}</td>
                </tr>
                <tr style="background: #d1ecf1;">
                    <td style="padding: 15px 10px; font-weight: bold; color: #0c5460; font-size: 16px;">🔄 Deposit Refund:</td>
                    <td style="padding: 15px 10px; text-align: right; color: #0c5460; font-size: 18px; font-weight: bold;">{{currency}} {{deposit_amount}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px; font-size: 13px; color: #666;" colspan="2">Your deposit will be processed within 2-3 business days</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #d4edda; padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #155724; font-size: 18px;">⭐ How Was Your Experience?</h3>
            <p style="margin: 0 0 15px 0; color: #155724; line-height: 1.6;">We''d love to hear about your rental experience! Your feedback helps us improve our service.</p>
            <div style="text-align: center;">
                <a href="https://ryverental.com/reviews" style="display: inline-block; background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; font-size: 15px;">Leave a Review</a>
            </div>
        </div>
        
        <div style="background: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #856404; font-size: 18px;">📄 Next Steps</h3>
            <ul style="margin: 0; padding-left: 20px; color: #856404;">
                <li style="margin: 8px 0; line-height: 1.6;">Check your email for the deposit refund confirmation</li>
                <li style="margin: 8px 0; line-height: 1.6;">Share your experience with friends</li>
                <li style="margin: 8px 0; line-height: 1.6;">Book again for your next adventure!</li>
            </ul>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Need Support?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #17a2b8; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thank you for choosing Ryve Rental! 🙏</p>
            <p style="margin: 10px 0 0 0; font-size: 14px; color: #f0f0f0;">We hope to serve you again soon.</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'rental',
    'Customer notification when rental is completed',
    true,
    true,
    '["customer_name","booking_reference","vehicle_make","vehicle_model","trip_duration","distance_traveled","currency","total_amount","deposit_amount","support_email"]',
    NOW(),
    NOW()
);

-- 4. BOOKING COMPLETED - OWNER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'booking_completed_owner',
    'Rental Completed - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #6f42c1 0%, #563d7c 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">✅ Rental Completed!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Vehicle returned successfully</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #6f42c1;">{{owner_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Good news! The rental for your vehicle has been successfully completed and your vehicle has been returned.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #6f42c1; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #6f42c1; font-size: 22px;">📊 Rental Summary</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #6f42c1; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Customer:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{customer_name}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Vehicle:</td>
                    <td style="padding: 10px 0;">{{vehicle_make}} {{vehicle_model}} ({{vehicle_plate}})</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Duration:</td>
                    <td style="padding: 10px 0;">{{trip_duration}} days</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Distance:</td>
                    <td style="padding: 10px 0;">{{distance_traveled}} km</td>
                </tr>
            </table>
            
            <hr style="border: none; border-top: 2px solid #e0e0e0; margin: 20px 0;">
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr style="background: #e7e3f5;">
                    <td style="padding: 15px 10px; font-weight: bold; color: #6f42c1; font-size: 18px;">💰 Your Earnings:</td>
                    <td style="padding: 15px 10px; text-align: right; color: #6f42c1; font-size: 22px; font-weight: bold;">{{currency}} {{rental_amount}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px; font-size: 13px; color: #666;" colspan="2">Payout will be processed according to your payment schedule</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #856404; font-size: 18px;">✅ Next Steps</h3>
            <ul style="margin: 0; padding-left: 20px; color: #856404;">
                <li style="margin: 8px 0; line-height: 1.6;">Inspect your vehicle for any damages</li>
                <li style="margin: 8px 0; line-height: 1.6;">Clean and prepare for the next rental</li>
                <li style="margin: 8px 0; line-height: 1.6;">Check your payout schedule</li>
                <li style="margin: 8px 0; line-height: 1.6;">Review customer if needed</li>
            </ul>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Questions?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #6f42c1; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #6f42c1 0%, #563d7c 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thank you for being a valued partner! 🤝</p>
            <p style="margin: 10px 0 0 0; font-size: 14px; color: #f0f0f0;">Keep earning with Ryve Rental.</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'rental',
    'Owner notification when rental is completed',
    true,
    true,
    '["owner_name","booking_reference","customer_name","vehicle_make","vehicle_model","vehicle_plate","trip_duration","distance_traveled","currency","rental_amount","support_email"]',
    NOW(),
    NOW()
);

-- 5. VEHICLE RETURNED - CUSTOMER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'vehicle_returned_customer',
    'Vehicle Return Confirmed - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #fd7e14 0%, #e8590c 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">🔄 Vehicle Returned!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Return confirmed successfully</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #fd7e14;">{{customer_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Thank you for returning the vehicle on time! Your return has been confirmed.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #fd7e14; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #fd7e14; font-size: 22px;">📋 Return Details</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #fd7e14; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Vehicle:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{vehicle_make}} {{vehicle_model}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Return Date:</td>
                    <td style="padding: 10px 0;">{{return_date}} at {{return_time}}</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #d1ecf1; padding: 20px; border-radius: 8px; border-left: 4px solid #17a2b8; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #0c5460; font-size: 18px;">⏳ What''s Next?</h3>
            <ul style="margin: 0; padding-left: 20px; color: #0c5460;">
                <li style="margin: 8px 0; line-height: 1.6;">Final inspection is being conducted</li>
                <li style="margin: 8px 0; line-height: 1.6;">Trip summary will be sent shortly</li>
                <li style="margin: 8px 0; line-height: 1.6;">Deposit refund will be processed</li>
            </ul>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Questions?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #fd7e14; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #fd7e14 0%, #e8590c 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thank you for choosing Ryve Rental! 🚗</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'rental',
    'Customer notification when vehicle is returned',
    true,
    true,
    '["customer_name","booking_reference","vehicle_make","vehicle_model","return_date","return_time","support_email"]',
    NOW(),
    NOW()
);

-- 6. VEHICLE RETURNED - OWNER
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Category", "Description", "IsActive", "IsHtml", "AvailablePlaceholdersJson", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'vehicle_returned_owner',
    'Vehicle Returned - {{booking_reference}}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 0; background-color: #f4f4f4;">
    
    <div style="background: linear-gradient(135deg, #6610f2 0%, #520dc2 100%); padding: 40px 20px; text-align: center;">
        <h1 style="color: white; margin: 0; font-size: 32px; font-weight: bold;">🔄 Vehicle Returned!</h1>
        <p style="color: #f0f0f0; margin: 10px 0 0 0; font-size: 16px;">Your vehicle is back</p>
    </div>
    
    <div style="background: white; padding: 30px 20px;">
        
        <p style="font-size: 18px; color: #333; margin-bottom: 20px;">Hi <strong style="color: #6610f2;">{{owner_name}}</strong>,</p>
        
        <p style="font-size: 16px; line-height: 1.8;">Good news! Your vehicle has been returned by the customer.</p>
        
        <div style="background: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 5px solid #6610f2; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
            <h2 style="margin: 0 0 20px 0; color: #6610f2; font-size: 22px;">📋 Return Details</h2>
            
            <table style="width: 100%; border-collapse: collapse;">
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Booking Reference:</td>
                    <td style="padding: 10px 0; color: #6610f2; font-size: 18px; font-weight: bold;">{{booking_reference}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Customer:</td>
                    <td style="padding: 10px 0; font-size: 16px;">{{customer_name}}</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Vehicle:</td>
                    <td style="padding: 10px 0;">{{vehicle_make}} {{vehicle_model}} ({{vehicle_plate}})</td>
                </tr>
                <tr>
                    <td style="padding: 10px 0; font-weight: bold; color: #666;">Return Date:</td>
                    <td style="padding: 10px 0;">{{return_date}} at {{return_time}}</td>
                </tr>
            </table>
        </div>
        
        <div style="background: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;">
            <h3 style="margin: 0 0 15px 0; color: #856404; font-size: 18px;">✅ Next Steps</h3>
            <ul style="margin: 0; padding-left: 20px; color: #856404;">
                <li style="margin: 8px 0; line-height: 1.6;">Conduct final vehicle inspection</li>
                <li style="margin: 8px 0; line-height: 1.6;">Document any damages or issues</li>
                <li style="margin: 8px 0; line-height: 1.6;">Complete the return checklist</li>
                <li style="margin: 8px 0; line-height: 1.6;">Prepare vehicle for next rental</li>
            </ul>
        </div>
        
        <div style="text-align: center; margin: 35px 0; padding: 25px; background: #f8f9fa; border-radius: 8px;">
            <p style="color: #666; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">Need Assistance?</p>
            <p style="margin: 8px 0;"><strong>📧 Email:</strong> <a href="mailto:{{support_email}}" style="color: #6610f2; text-decoration: none;">{{support_email}}</a></p>
        </div>
        
        <div style="background: linear-gradient(135deg, #6610f2 0%, #520dc2 100%); padding: 25px; border-radius: 8px; text-align: center; margin-top: 35px;">
            <p style="margin: 0; font-size: 18px; color: white; font-weight: bold;">Thanks for partnering with us! 🤝</p>
        </div>
        
    </div>
    
    <div style="text-align: center; padding: 20px; background-color: #f4f4f4;">
        <p style="margin: 5px 0; font-size: 12px; color: #999;">This is an automated message. Please do not reply to this email.</p>
        <p style="margin: 5px 0; font-size: 12px; color: #999;">© 2025 Ryve Rental. All rights reserved.</p>
    </div>
    
</body>
</html>',
    'rental',
    'Owner notification when vehicle is returned',
    true,
    true,
    '["owner_name","booking_reference","customer_name","vehicle_make","vehicle_model","vehicle_plate","return_date","return_time","support_email"]',
    NOW(),
    NOW()
);

-- Success message
SELECT 'Email templates created successfully! Total: 6 templates' AS result;
