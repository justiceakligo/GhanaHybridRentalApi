# Partner Booking Email Templates

## Required Email Templates for Partner Bookings

Create these email templates via the admin endpoint `/api/v1/admin/email-templates`:

### 1. booking_confirmed_partner

**Subject:** 
```
Booking Confirmed via {{partner_name}} - {{booking_reference}}
```

**Template Body:**
```html
<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
    <h2>✅ Booking Confirmed!</h2>
    
    <p>Hi {{customer_name}},</p>
    
    <p>Your car rental booking is confirmed!</p>
    
    <div style="background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;">
        <h3>Booking Details</h3>
        <p><strong>Reference:</strong> {{booking_reference}}</p>
        <p><strong>Booked via:</strong> {{partner_name}}</p>
        <p><strong>Vehicle:</strong> {{vehicle_make}} {{vehicle_model}}</p>
        <p><strong>Pickup:</strong> {{pickup_date}} at {{pickup_time}}</p>
        <p><strong>Return:</strong> {{return_date}} at {{return_time}}</p>
        <p><strong>Duration:</strong> {{trip_duration}} days</p>
    </div>
    
    <div style="background: #e8f5e9; padding: 15px; border-left: 4px solid #4caf50; margin: 20px 0;">
        <p><strong>💳 Payment:</strong> {{payment_note}}</p>
    </div>
    
    <h3>Owner Contact Information</h3>
    <p><strong>Name:</strong> {{owner_name}}</p>
    <p><strong>Phone:</strong> {{owner_phone}}</p>
    <p><strong>Address:</strong> {{owner_address}}</p>
    
    <h3>Pickup Instructions</h3>
    <p>{{pickup_instructions}}</p>
    
    <div style="margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;">
        <p><strong>Questions about your booking?</strong></p>
        <p>Contact {{partner_name}} for booking-related inquiries, or email us at {{support_email}}</p>
    </div>
    
    <p style="margin-top: 30px;">
        Safe travels!<br>
        The Ryve Rental Team
    </p>
</div>
```

### 2. owner_new_booking_partner

**Subject:**
```
New Partner Booking - {{booking_reference}}
```

**Template Body:**
```html
<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
    <h2>🎉 New Booking from {{partner_name}}!</h2>
    
    <p>Hi {{owner_name}},</p>
    
    <p>You have a new booking from our partner {{partner_name}}.</p>
    
    <div style="background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;">
        <h3>Booking Details</h3>
        <p><strong>Reference:</strong> {{booking_reference}}</p>
        <p><strong>Customer:</strong> {{customer_name}}</p>
        <p><strong>Vehicle:</strong> {{vehicle_make}} {{vehicle_model}}</p>
        <p><strong>Pickup:</strong> {{pickup_date}} at {{pickup_time}}</p>
        <p><strong>Return:</strong> {{return_date}} at {{return_time}}</p>
    </div>
    
    <div style="background: #fff3e0; padding: 15px; border-left: 4px solid #ff9800; margin: 20px 0;">
        <p><strong>💰 Your Earnings:</strong> {{currency}} {{owner_earnings}}</p>
        <p><small>Settlement pending from {{partner_name}}</small></p>
    </div>
    
    <div style="background: #e3f2fd; padding: 15px; border-left: 4px solid #2196f3; margin: 20px 0;">
        <p><strong>📝 Note:</strong> This is a partner booking. Payment has been processed via {{partner_name}}. Your payout will be processed after partner settlement.</p>
    </div>
    
    <a href="https://dashboard.ryverental.com/owner/bookings/{{booking_id}}" 
       style="display: inline-block; background: #4caf50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0;">
        View in Dashboard
    </a>
    
    <p style="margin-top: 30px;">
        Best regards,<br>
        The Ryve Rental Team
    </p>
</div>
```

## How to Create Templates

Use the admin API to create these templates:

```bash
POST /api/v1/admin/email-templates
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "templateName": "booking_confirmed_partner",
  "subject": "Booking Confirmed via {{partner_name}} - {{booking_reference}}",
  "htmlContent": "...", 
  "isActive": true
}
```

## Template Placeholders Available

For partner bookings, these additional placeholders are available:
- `{{partner_name}}` - Name of the integration partner
- `{{payment_note}}` - Payment processing note (e.g., "Payment processed via Hotel ABC")
- `{{owner_earnings}}` - Owner's earnings from this booking

All standard booking placeholders also work:
- `{{customer_name}}`, `{{booking_reference}}`, `{{vehicle_make}}`, etc.
