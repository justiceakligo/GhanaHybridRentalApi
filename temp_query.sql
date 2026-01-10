-- Add QR code to booking_confirmed email template
-- This updates the template to include a QR code for easy check-in access

UPDATE "EmailTemplates"
SET 
    "HtmlBody" = '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Booking Confirmed</title>
</head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                    
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #2d7d5d 0%, #1e5a42 100%); padding: 40px 30px; text-align: center;">
                            <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;">ðŸŽ‰ Booking Confirmed!</h1>
                            <p style="margin: 10px 0 0 0; color: #e0f2e9; font-size: 16px;">Your rental is ready to go</p>
                        </td>
                    </tr>
                    
                    <!-- Booking Reference -->
                    <tr>
                        <td style="padding: 30px; text-align: center; background-color: #f8fffe;">
                            <p style="margin: 0 0 10px 0; color: #666; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;">Booking Reference</p>
                            <h2 style="margin: 0; color: #2d7d5d; font-size: 32px; font-weight: bold; letter-spacing: 2px;">{{booking_reference}}</h2>
                        </td>
                    </tr>
                    
                    <!-- Main Content -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <p style="margin: 0 0 20px 0; color: #333; font-size: 16px; line-height: 1.6;">
                                Hi <strong>{{customer_name}}</strong>,
                            </p>
                            <p style="margin: 0 0 20px 0; color: #333; font-size: 16px; line-height: 1.6;">
                                Great news! Your booking has been confirmed and payment received. Your vehicle is reserved and ready for your trip.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- QR Code Section -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px; text-align: center;">
                            <div style="background-color: #f8fffe; border: 2px solid #2d7d5d; border-radius: 12px; padding: 30px; display: inline-block;">
                                <h3 style="margin: 0 0 15px 0; color: #2d7d5d; font-size: 20px; font-weight: bold;">ðŸ“± Quick Check-In QR Code</h3>
                                <p style="margin: 0 0 20px 0; color: #666; font-size: 14px;">Scan this code when you arrive for pickup</p>
                                <div style="background: white; padding: 15px; border-radius: 8px; display: inline-block; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                                    <img src="{{qr_code}}" alt="Check-in QR Code" style="width: 200px; height: 200px; display: block; border: 3px solid #2d7d5d; border-radius: 8px;" />
                                </div>
                                <p style="margin: 20px 0 0 0; color: #666; font-size: 13px; font-style: italic;">
                                    Save this email or take a screenshot for easy access at pickup
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Trip Details -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f8fffe; border-radius: 8px; overflow: hidden;">
                                <tr>
                                    <td colspan="2" style="padding: 20px; background-color: #2d7d5d;">
                                        <h3 style="margin: 0; color: #ffffff; font-size: 18px;">ðŸ“‹ Trip Details</h3>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px; width: 40%;">Vehicle</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{vehicle_make}} {{vehicle_model}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Plate Number</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{vehicle_plate}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Rental Type</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{vehicle_type}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Pickup</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{pickup_date}} at {{pickup_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Return</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{return_date}} at {{return_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Duration</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; font-weight: bold;">{{trip_duration}} days</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; color: #666; font-size: 14px;">Pickup Location</td>
                                    <td style="padding: 15px 20px; color: #333; font-size: 14px; font-weight: bold;">{{pickup_location}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Owner Contact -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #fffef8; border-radius: 8px; overflow: hidden; border: 1px solid #f0ead6;">
                                <tr>
                                    <td colspan="2" style="padding: 20px; background-color: #f0ead6;">
                                        <h3 style="margin: 0; color: #333; font-size: 18px;">ðŸ‘¤ Owner Contact</h3>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #f0ead6; color: #666; font-size: 14px; width: 40%;">Name</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #f0ead6; color: #333; font-size: 14px; font-weight: bold;">{{owner_name}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #f0ead6; color: #666; font-size: 14px;">Phone</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #f0ead6; color: #333; font-size: 14px; font-weight: bold;">{{owner_phone}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; color: #666; font-size: 14px;">Pickup Address</td>
                                    <td style="padding: 15px 20px; color: #333; font-size: 14px; font-weight: bold;">{{owner_address}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Pickup Instructions -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <div style="background-color: #e8f5e9; border-left: 4px solid #2d7d5d; padding: 20px; border-radius: 4px;">
                                <h4 style="margin: 0 0 10px 0; color: #2d7d5d; font-size: 16px;">ðŸ“ Pickup Instructions</h4>
                                <p style="margin: 0; color: #333; font-size: 14px; line-height: 1.6;">{{pickup_instructions}}</p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Payment Summary -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f8fffe; border-radius: 8px; overflow: hidden;">
                                <tr>
                                    <td colspan="2" style="padding: 20px; background-color: #2d7d5d;">
                                        <h3 style="margin: 0; color: #ffffff; font-size: 18px;">ðŸ’³ Payment Summary</h3>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Rental Amount</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; text-align: right; font-weight: bold;">{{currency}} {{rental_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #666; font-size: 14px;">Driver Service</td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e0e0e0; color: #333; font-size: 14px; text-align: right; font-weight: bold;">{{currency}} {{driver_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; background-color: #2d7d5d; color: #ffffff; font-size: 16px; font-weight: bold;">Total Paid</td>
                                    <td style="padding: 15px 20px; background-color: #2d7d5d; color: #ffffff; font-size: 16px; text-align: right; font-weight: bold;">{{currency}} {{total_amount}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Check-in Button -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px; text-align: center;">
                            <a href="{{inspection_link}}" style="display: inline-block; padding: 16px 40px; background-color: #2d7d5d; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold; box-shadow: 0 4px 6px rgba(45, 125, 93, 0.3);">
                                Start Check-In Process
                            </a>
                            <p style="margin: 15px 0 0 0; color: #666; font-size: 13px;">
                                Or scan the QR code above when you arrive
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Important Notes -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <div style="background-color: #fff3e0; border-left: 4px solid #ff9800; padding: 20px; border-radius: 4px;">
                                <h4 style="margin: 0 0 10px 0; color: #e65100; font-size: 16px;">âš ï¸ Important Reminders</h4>
                                <ul style="margin: 0; padding-left: 20px; color: #333; font-size: 14px; line-height: 1.8;">
                                    <li>Bring your driver''s license and ID</li>
                                    <li>Arrive 15 minutes before pickup time</li>
                                    <li>Complete the vehicle inspection with the owner</li>
                                    <li>Use the QR code or inspection link for quick check-in</li>
                                    <li>Contact the owner if you''ll be late</li>
                                </ul>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style="padding: 30px; text-align: center; background-color: #f8f8f8; border-top: 1px solid #e0e0e0;">
                            <p style="margin: 0 0 10px 0; color: #666; font-size: 14px;">
                                Need help? Contact us at <a href="mailto:{{support_email}}" style="color: #2d7d5d; text-decoration: none; font-weight: bold;">{{support_email}}</a>
                            </p>
                            <p style="margin: 0; color: #999; font-size: 12px;">
                                Â© 2026 RyvePool. All rights reserved.
                            </p>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    "UpdatedAt" = NOW()
WHERE "TemplateCode" = 'booking_confirmed';

-- Verify the update
SELECT "TemplateCode", "Subject", "UpdatedAt" 
FROM "EmailTemplates" 
WHERE "TemplateCode" = 'booking_confirmed';

