-- Update booking_reserved and booking_confirmed email templates with complete pricing breakdown
-- Includes: Vehicle rental, Driver, Protection Plan, Platform Fee, Security Deposit, Promo Discount

-- Update booking_reserved template (sent when booking is created, before payment)
UPDATE "EmailTemplates"
SET "BodyTemplate" = '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 20px 0;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;">
                            <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;">ðŸ”– Booking Reserved!</h1>
                            <p style="margin: 10px 0 0 0; color: #f0f0f0; font-size: 16px;">Complete your payment to confirm</p>
                        </td>
                    </tr>
                    
                    <!-- Booking Reference -->
                    <tr>
                        <td style="padding: 30px; background-color: #fef9e7; border-bottom: 3px solid #f39c12;">
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td>
                                        <p style="margin: 0; color: #666; font-size: 14px;">Booking Reference</p>
                                        <h2 style="margin: 5px 0 0 0; color: #f39c12; font-size: 24px; font-weight: bold;">{{booking_reference}}</h2>
                                    </td>
                                    <td align="right">
                                        <div style="background-color: #e74c3c; color: #ffffff; padding: 8px 16px; border-radius: 20px; font-weight: bold; font-size: 14px;">
                                            âš ï¸ PENDING PAYMENT
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Customer Info -->
                    <tr>
                        <td style="padding: 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #667eea; padding-bottom: 10px;">ðŸ‘¤ Customer Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Name:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{customer_name}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Email:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{customer_email}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Phone:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{customer_phone}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Vehicle Info -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #667eea; padding-bottom: 10px;">ðŸš— Vehicle Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Vehicle:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_make}} {{vehicle_model}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Plate Number:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_plate}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Type:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_type}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Trip Details -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #667eea; padding-bottom: 10px;">ðŸ“… Trip Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Pickup:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{pickup_date}} at {{pickup_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Return:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{return_date}} at {{return_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Duration:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{trip_duration}} days</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Pickup Location:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{pickup_location}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Return Location:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{return_location}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Pricing Breakdown -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #667eea; padding-bottom: 10px;">ðŸ’° Pricing Breakdown</h3>
                            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f8f9fa; border-radius: 8px; overflow: hidden;">
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #e0e0e0;">Vehicle ({{trip_duration}} days)</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #e0e0e0;">{{currency}} {{rental_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #e0e0e0;">Driver Service</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #e0e0e0;">{{currency}} {{driver_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #e0e0e0;">ðŸ›¡ï¸ Protection Plan</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #e0e0e0;">{{currency}} {{protection_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #e0e0e0;">ðŸ“Š Platform Fee</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #e0e0e0;">{{currency}} {{platform_fee}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 2px solid #667eea;">ðŸ”’ Security Deposit (Refundable)</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #2196F3; border-bottom: 2px solid #667eea;">{{currency}} {{deposit_amount}}</td>
                                </tr>
                                <tr style="display: {{promo_display}};">
                                    <td style="padding: 12px 20px; color: #27ae60; border-bottom: 2px solid #667eea;">ðŸŽ Promo Discount</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #27ae60; border-bottom: 2px solid #667eea;">-{{currency}} {{promo_discount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 20px; background-color: #667eea; color: #ffffff; font-size: 18px; font-weight: bold;">TOTAL</td>
                                    <td style="padding: 20px; background-color: #667eea; color: #ffffff; font-size: 20px; text-align: right; font-weight: bold;">{{currency}} {{total_amount}}</td>
                                </tr>
                            </table>
                            <p style="margin: 10px 0 0 0; color: #999; font-size: 13px; font-style: italic;">âœ“ Includes {{currency}} {{deposit_amount}} refundable security deposit</p>
                        </td>
                    </tr>
                    
                    <!-- Next Steps -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <div style="background: #e7f3ff; border-left: 5px solid #2196F3; padding: 20px; border-radius: 8px;">
                                <h3 style="margin: 0 0 15px 0; color: #1976D2; font-size: 18px;">ðŸ’³ Next Steps</h3>
                                <ol style="margin: 0; padding-left: 20px; color: #333;">
                                    <li style="margin-bottom: 10px;">Complete your payment through the booking portal</li>
                                    <li style="margin-bottom: 10px;">You will receive a confirmation email once payment is processed</li>
                                    <li style="margin-bottom: 10px;">Ensure you arrive on time for pickup</li>
                                    <li style="margin-bottom: 10px;">Bring a valid ID and driver''s license</li>
                                </ol>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Support -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <div style="background: #f8f9fa; padding: 20px; border-radius: 8px;">
                                <h3 style="margin: 0 0 15px 0; color: #333; font-size: 18px;">ðŸ“ž Need Help?</h3>
                                <p style="margin: 5px 0; color: #666;">Email: <a href="mailto:{{support_email}}" style="color: #667eea; text-decoration: none;">{{support_email}}</a></p>
                                <p style="margin: 5px 0; color: #666;">Phone: <a href="tel:{{support_phone}}" style="color: #667eea; text-decoration: none;">{{support_phone}}</a></p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style="text-align: center; padding: 30px; background-color: #f8f9fa; border-top: 1px solid #e0e0e0;">
                            <p style="color: #999; font-size: 14px; margin: 5px 0;">Thank you for choosing Ryve Rental</p>
                            <p style="color: #ccc; font-size: 12px; margin: 5px 0;">&copy; 2026 Ryve Rental. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>'
WHERE "TemplateName" = 'booking_reserved' OR "TemplateName" = 'booking_confirmation_customer';

-- Update booking_confirmed template (sent after successful payment)
UPDATE "EmailTemplates"
SET "BodyTemplate" = '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 20px 0;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #2d7d5d 0%, #1a5a3d 100%); padding: 40px 30px; text-align: center;">
                            <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;">âœ… Booking Confirmed!</h1>
                            <p style="margin: 10px 0 0 0; color: #b8e6d5; font-size: 16px;">Payment received successfully</p>
                        </td>
                    </tr>
                    
                    <!-- Booking Reference & QR Code -->
                    <tr>
                        <td style="padding: 30px; background-color: #f0fdf4; border-bottom: 3px solid #2d7d5d;">
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td width="60%">
                                        <p style="margin: 0; color: #666; font-size: 14px;">Booking Reference</p>
                                        <h2 style="margin: 5px 0 0 0; color: #2d7d5d; font-size: 24px; font-weight: bold;">{{booking_reference}}</h2>
                                    </td>
                                    <td width="40%" align="right">
                                        <img src="{{qr_code}}" alt="QR Code" style="width: 120px; height: 120px; border: 2px solid #2d7d5d; border-radius: 8px;" />
                                    </td>
                                </tr>
                            </table>
                            <div style="margin-top: 15px; background-color: #2d7d5d; color: #ffffff; padding: 10px 16px; border-radius: 6px; text-align: center; font-weight: bold; font-size: 14px;">
                                âœ“ CONFIRMED & PAID
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Vehicle Info -->
                    <tr>
                        <td style="padding: 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #2d7d5d; padding-bottom: 10px;">ðŸš— Vehicle Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Vehicle:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_make}} {{vehicle_model}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Plate Number:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_plate}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Type:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{vehicle_type}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Trip Details -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #2d7d5d; padding-bottom: 10px;">ðŸ“… Trip Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Pickup:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{pickup_date}} at {{pickup_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Return:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{return_date}} at {{return_time}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Duration:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{trip_duration}} days</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Pickup Location:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{pickup_location}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Payment Summary -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #2d7d5d; padding-bottom: 10px;">ðŸ’³ Payment Summary</h3>
                            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f8fffe; border-radius: 8px; overflow: hidden;">
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #d1f2e8;">Vehicle ({{trip_duration}} days)</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #d1f2e8;">{{currency}} {{rental_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #d1f2e8;">Driver Service</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #d1f2e8;">{{currency}} {{driver_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #d1f2e8;">ðŸ›¡ï¸ Protection Plan</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #d1f2e8;">{{currency}} {{protection_amount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 1px solid #d1f2e8;">ðŸ“Š Platform Fee</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #333; border-bottom: 1px solid #d1f2e8;">{{currency}} {{platform_fee}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 12px 20px; color: #666; border-bottom: 2px solid #2d7d5d;">ðŸ”’ Security Deposit (Refundable)</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #2196F3; border-bottom: 2px solid #2d7d5d;">{{currency}} {{deposit_amount}}</td>
                                </tr>
                                <tr style="display: {{promo_display}};">
                                    <td style="padding: 12px 20px; color: #27ae60; border-bottom: 2px solid #2d7d5d;">ðŸŽ Promo Discount</td>
                                    <td style="padding: 12px 20px; text-align: right; font-weight: bold; color: #27ae60; border-bottom: 2px solid #2d7d5d;">-{{currency}} {{promo_discount}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 20px; background-color: #2d7d5d; color: #ffffff; font-size: 18px; font-weight: bold;">TOTAL PAID</td>
                                    <td style="padding: 20px; background-color: #2d7d5d; color: #ffffff; font-size: 20px; text-align: right; font-weight: bold;">{{currency}} {{total_amount}}</td>
                                </tr>
                            </table>
                            <p style="margin: 10px 0 0 0; color: #999; font-size: 13px; font-style: italic;">âœ“ Includes {{currency}} {{deposit_amount}} refundable security deposit</p>
                        </td>
                    </tr>
                    
                    <!-- Owner Info -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <h3 style="margin: 0 0 20px 0; color: #333; font-size: 18px; border-bottom: 2px solid #2d7d5d; padding-bottom: 10px;">ðŸ  Owner Details</h3>
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Name:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{owner_name}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Phone:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{owner_phone}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">Address:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{owner_address}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; color: #666;">GPS Address:</td>
                                    <td style="padding: 8px 0; color: #333; font-weight: bold; text-align: right;">{{owner_gps_address}}</td>
                                </tr>
                            </table>
                            <div style="margin-top: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; border-radius: 4px;">
                                <p style="margin: 0; color: #856404; font-size: 14px;"><strong>ðŸ“ Pickup Instructions:</strong></p>
                                <p style="margin: 5px 0 0 0; color: #856404; font-size: 14px;">{{pickup_instructions}}</p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Check-in Button -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px; text-align: center;">
                            <a href="{{inspection_link}}" style="display: inline-block; padding: 16px 40px; background-color: #2d7d5d; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold; box-shadow: 0 4px 6px rgba(45, 125, 93, 0.3);">
                                Start Check-In Process
                            </a>
                            <p style="margin: 15px 0 0 0; color: #666; font-size: 13px;">Scan the QR code or click the button above to begin vehicle inspection at pickup</p>
                        </td>
                    </tr>
                    
                    <!-- Support -->
                    <tr>
                        <td style="padding: 0 30px 30px 30px;">
                            <div style="background: #f8f9fa; padding: 20px; border-radius: 8px;">
                                <h3 style="margin: 0 0 15px 0; color: #333; font-size: 18px;">ðŸ“ž Need Help?</h3>
                                <p style="margin: 5px 0; color: #666;">Email: <a href="mailto:{{support_email}}" style="color: #2d7d5d; text-decoration: none;">{{support_email}}</a></p>
                                <p style="margin: 5px 0; color: #666;">Phone: <a href="tel:{{support_phone}}" style="color: #2d7d5d; text-decoration: none;">{{support_phone}}</a></p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style="text-align: center; padding: 30px; background-color: #f8f9fa; border-top: 1px solid #e0e0e0;">
                            <p style="color: #999; font-size: 14px; margin: 5px 0;">Thank you for choosing Ryve Rental</p>
                            <p style="color: #ccc; font-size: 12px; margin: 5px 0;">&copy; 2026 Ryve Rental. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>'
WHERE "TemplateName" = 'booking_confirmed';

