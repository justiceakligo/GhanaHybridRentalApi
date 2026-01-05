-- Create owner_account_approved email template
INSERT INTO "EmailTemplates" 
    ("Id", "Name", "Subject", "HtmlBody", "PlainTextBody", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
    (
        gen_random_uuid(),
        'owner_account_approved',
        'Your Owner Account Has Been Approved! 🎉',
        '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }
        .info-box { background: white; padding: 20px; border-left: 4px solid #667eea; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .success-icon { font-size: 48px; margin-bottom: 20px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div class="success-icon">✅</div>
            <h1>Congratulations, {{ownerName}}!</h1>
            <p>Your owner account has been approved</p>
        </div>
        <div class="content">
            <p>Great news! Your Ryve Rental owner account has been successfully approved and activated.</p>
            
            <div class="info-box">
                <h3>🎯 What''s Next?</h3>
                <ul>
                    <li>Log in to your owner dashboard</li>
                    <li>Add your vehicles to start earning</li>
                    <li>Set your pricing and availability</li>
                    <li>Manage bookings and payments</li>
                </ul>
            </div>

            <div style="text-align: center; margin: 30px 0;">
                <a href="{{loginUrl}}" class="button">Login to Dashboard</a>
            </div>

            <div class="info-box">
                <h3>📋 Your Account Details</h3>
                <p><strong>Email:</strong> {{email}}</p>
                <p><strong>Account Type:</strong> Owner</p>
                <p><strong>Status:</strong> Active ✅</p>
            </div>

            <h3>💡 Quick Start Guide</h3>
            <ol>
                <li><strong>Complete Your Profile:</strong> Add business details and payout information</li>
                <li><strong>List Your First Vehicle:</strong> Upload photos and set pricing</li>
                <li><strong>Get Verified:</strong> Upload required documents for vehicle verification</li>
                <li><strong>Start Earning:</strong> Accept bookings and receive payments</li>
            </ol>

            <div class="info-box">
                <h3>💰 Payment Information</h3>
                <p>Don''t forget to set up your payout details in your profile to receive payments seamlessly.</p>
            </div>

            <p>If you have any questions or need assistance getting started, our support team is here to help!</p>
        </div>
        <div class="footer">
            <p>© 2026 Ryve Rental. All rights reserved.</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>',
        'Congratulations {{ownerName}}!

Your Ryve Rental owner account has been successfully approved and activated.

What''s Next:
- Log in to your owner dashboard
- Add your vehicles to start earning
- Set your pricing and availability
- Manage bookings and payments

Login here: {{loginUrl}}

Your Account Details:
Email: {{email}}
Account Type: Owner
Status: Active

Quick Start Guide:
1. Complete Your Profile - Add business details and payout information
2. List Your First Vehicle - Upload photos and set pricing
3. Get Verified - Upload required documents
4. Start Earning - Accept bookings and receive payments

Payment Information:
Don''t forget to set up your payout details in your profile to receive payments.

If you have any questions, our support team is here to help!

© 2026 Ryve Rental. All rights reserved.',
        'transactional',
        true,
        NOW(),
        NOW()
    );

-- Verify template was created
SELECT "Id", "Name", "Subject", "Category", "IsActive" 
FROM "EmailTemplates" 
WHERE "Name" = 'owner_account_approved';
