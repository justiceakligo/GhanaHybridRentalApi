-- Create email template for deposit refund notifications
INSERT INTO "EmailTemplates" ("Id", "TemplateName", "Subject", "BodyTemplate", "Description", "Category", "IsActive", "IsHtml", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'deposit_refund_processed',
    'Deposit Refund Processed - Ryve Rental',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .refund-box { background: white; border-left: 4px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 5px; }
        .amount { font-size: 32px; font-weight: bold; color: #10b981; margin: 10px 0; }
        .info-row { padding: 10px 0; border-bottom: 1px solid #eee; }
        .label { font-weight: bold; color: #666; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>💰 Deposit Refund Processed</h1>
        </div>
        <div class="content">
            <p>Hello {{customer_name}},</p>
            
            <p>Great news! Your deposit refund has been successfully processed.</p>
            
            <div class="refund-box">
                <div class="amount">{{currency}} {{amount}}</div>
                <div class="info-row">
                    <span class="label">Booking Reference:</span> {{booking_reference}}
                </div>
                <div class="info-row">
                    <span class="label">Vehicle:</span> {{vehicle_name}}
                </div>
                <div class="info-row">
                    <span class="label">Status:</span> <span style="color: #10b981;">✓ Processed</span>
                </div>
            </div>
            
            <p>The refund has been initiated to your mobile money account. Depending on your mobile network provider, it may take a few minutes to reflect in your account.</p>
            
            <p><strong>What happens next?</strong></p>
            <ul>
                <li>You will receive an SMS confirmation from your mobile money provider</li>
                <li>The funds should appear in your account within 5-10 minutes</li>
                <li>If you don''t receive it within 1 hour, please contact us</li>
            </ul>
            
            <center>
                <a href="https://dashboard.ryverental.com/bookings/{{booking_id}}" class="button">View Booking Details</a>
            </center>
            
            <p>Thank you for choosing Ryve Rental. We look forward to serving you again!</p>
            
            <p>Best regards,<br>
            The Ryve Rental Team</p>
        </div>
        <div class="footer">
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>&copy; 2026 Ryve Rental. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
    'Deposit refund processed notification for customers',
    'Refunds',
    true,
    true,
    NOW(),
    NOW()
)
ON CONFLICT ("TemplateName") DO UPDATE SET
    "Subject" = EXCLUDED."Subject",
    "BodyTemplate" = EXCLUDED."BodyTemplate",
    "Description" = EXCLUDED."Description",
    "Category" = EXCLUDED."Category",
    "IsActive" = EXCLUDED."IsActive",
    "IsHtml" = EXCLUDED."IsHtml",
    "UpdatedAt" = NOW();
