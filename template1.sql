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
