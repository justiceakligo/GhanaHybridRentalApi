# Email Attachment System Guide
**Automated PDF Attachments for Rental Agreements and Receipts**

## 🎯 Quick Overview

The system now automatically attaches PDF documents to booking notification emails:

1. **Rental Agreement PDF** → Attached to booking confirmation email after payment
2. **Receipt PDF** → Attached to trip completion email after vehicle return

---

## 📧 Email Flow

### When a Booking is Confirmed

**Trigger:** Payment received or owner manually confirms booking

**Email Template:** `booking_confirmed`

**Recipients:** 
- Renter (with attachment)
- Owner (notification only)

**Attachment:** `rental-agreement-{BookingReference}.pdf`

**Code Flow:**
```
Payment Confirmed
    ↓
NotificationService.SendBookingConfirmedNotificationAsync()
    ↓
Query RentalAgreementAcceptances table
    ↓
If agreement exists:
    GenerateRentalAgreementDocument()
    Create EmailAttachment(filename, bytes, "application/pdf")
    SendEmailWithAttachmentsAsync(email, subject, body, [attachment])
Else:
    SendEmailAsync(email, subject, body) // No attachment
```

**Required Data:**
- Booking must have `RentalAgreementAcceptances` record
- Renter email must be valid

---

### When a Booking is Completed

**Trigger:** Return inspection completed, vehicle marked as returned

**Email Template:** `booking_completed_customer`

**Recipients:**
- Renter (with receipt attachment)
- Owner (payment notification)

**Attachment:** `receipt-{BookingReference}.pdf`

**Code Flow:**
```
Return Inspection Completed
    ↓
NotificationService.SendBookingCompletedNotificationAsync()
    ↓
ReceiptTemplateService.GenerateReceiptHtmlAsync(booking)
    ↓
Convert HTML to UTF-8 bytes
    ↓
Create EmailAttachment(filename, bytes, "application/pdf")
    ↓
SendEmailWithAttachmentsAsync(email, subject, body, [attachment])
    ↓
(Error handling: send email without attachment if generation fails)
```

**Required Data:**
- Active receipt template in `ReceiptTemplates` table
- Booking status: `Completed`
- Valid renter email

---

## 🔧 Technical Implementation

### Email Service Interface

```csharp
public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    
    Task SendEmailWithAttachmentsAsync(
        string email, 
        string subject, 
        string htmlMessage, 
        List<EmailAttachment> attachments);
}

public record EmailAttachment(
    string FileName,
    byte[] Content,
    string ContentType
);
```

### Resend API Integration

**Attachment Format:**
```json
{
  "from": "RyvePool <noreply@ryverental.com>",
  "to": ["renter@example.com"],
  "subject": "Booking Confirmed - BK123456",
  "html": "<html>...</html>",
  "attachments": [
    {
      "filename": "rental-agreement-BK123456.pdf",
      "content": "base64EncodedPdfContent..."
    }
  ]
}
```

**Base64 Encoding:**
```csharp
var base64Content = Convert.ToBase64String(attachment.Content);
```

---

## 📄 Rental Agreement PDF Structure

### Content Sections

1. **Header**
   ```
   ═══════════════════════════════════════════════
                   VEHICLE RENTAL AGREEMENT
                        RYVEPOOL
   ═══════════════════════════════════════════════
   ```

2. **Agreement Metadata**
   - Agreement Date (UTC timestamp)
   - Booking Reference
   - Template Code and Version

3. **Renter Information**
   - Full Name
   - Email
   - Phone Number

4. **Vehicle Information**
   - Make, Model, Year
   - Plate Number

5. **Rental Period**
   - Pickup Date/Time
   - Return Date/Time

6. **Agreement Terms**
   - Full text from `AgreementSnapshot` field
   - Terms and conditions as accepted

7. **Policy Acceptances**
   - ✓ No Smoking Policy: ACCEPTED
   - ✓ Fines & Tickets Responsibility: ACCEPTED
   - ✓ Accident Procedure: ACCEPTED

8. **Digital Signature**
   - Signed By (Renter Name)
   - Date & Time (UTC)
   - IP Address (for audit trail)

9. **Footer**
   ```
   This is a legally binding digital agreement.
   Acceptance was recorded electronically with audit trail.
   ```

### File Naming
```
rental-agreement-{BookingReference}.pdf
Example: rental-agreement-BK123456.pdf
```

---

## 🧾 Receipt PDF Structure

### Content Sections

1. **Header with Logo**
   - RyvePool logo (from template `LogoUrl`)
   - Company name and contact information

2. **Receipt Number**
   - Format: `{ReceiptNumberPrefix}-{BookingId}`
   - Example: `RVP-000123`

3. **Customer Information**
   - Name, Email, Phone
   - Populated from `{{customer_name}}` placeholder

4. **Vehicle Details**
   - Make, Model, Year, Plate Number
   - Populated from booking vehicle data

5. **Trip Information**
   - Pickup Location & Time
   - Return Location & Time
   - Trip Duration (days)
   - Distance Traveled (km)

6. **Charges Breakdown**
   - Rental Amount (base rate × days)
   - Additional Charges (extras, fees)
   - Subtotal
   - Deposit Amount
   - **Total Amount**

7. **Footer**
   - Terms and Conditions (from template)
   - QR Code (optional, if `ShowQrCode` enabled)
   - Payment information

### Styling
- **Primary Color:** #2d7d5d (RyvePool green)
- **Font:** Arial, sans-serif
- **Layout:** Responsive, print-friendly
- **Custom CSS:** Loaded from `ReceiptTemplate.CustomCss`

### File Naming
```
receipt-{BookingReference}.pdf
Example: receipt-BK123456.pdf
```

---

## 🛠️ Admin Features

### Download Receipt for Any Booking

**Endpoint:**
```http
GET /api/v1/admin/receipts/bookings/{bookingId}/pdf
Authorization: Bearer {admin_token}
```

**Example:**
```bash
curl -H "Authorization: Bearer eyJhbGc..." \
     http://api.ryverental.com/api/v1/admin/receipts/bookings/123/pdf \
     -o receipt.pdf
```

**Response:**
- Content-Type: `application/pdf`
- File download with name: `receipt-BK{reference}.pdf`

**Authorization:**
- Requires valid admin token
- Checked via `[Authorize(Roles = "Admin")]` attribute

---

### View Receipt Data as JSON

**Endpoint:**
```http
GET /api/v1/admin/receipts/bookings/{bookingId}/json
Authorization: Bearer {admin_token}
```

**Example:**
```bash
curl -H "Authorization: Bearer eyJhbGc..." \
     http://api.ryverental.com/api/v1/admin/receipts/bookings/123/json
```

**Response:**
```json
{
  "receiptNumber": "RVP-000123",
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "vehicleInfo": "Toyota Camry 2020",
  "rentalAmount": 300.00,
  "totalAmount": 450.00,
  "currency": "GHS",
  "tripDuration": "3 days",
  "distanceTraveled": "245 km",
  // ... all receipt placeholders
}
```

**Use Cases:**
- Admin dashboard display
- Custom receipt generation
- Data export/reporting
- Debugging receipt issues

---

## 🔄 Error Handling

### Rental Agreement Attachment

**Scenario 1: No Agreement Signed**
```
Query RentalAgreementAcceptances → null
    ↓
Send email WITHOUT attachment
    ↓
Log: "No rental agreement found for booking {ref}"
```

**Scenario 2: Missing Booking Data**
```
booking.Renter is null
    ↓
Skip email sending
    ↓
Log: "Cannot send notification: Renter not found"
```

### Receipt Attachment

**Scenario 1: Receipt Generation Fails**
```csharp
try {
    var receiptHtml = await _receiptTemplateService.GenerateReceiptHtmlAsync(booking);
    // ... attach to email
}
catch (Exception receiptEx) {
    _logger.LogError(receiptEx, "Failed to generate receipt for {ref}", ref);
    // Send email WITHOUT attachment as fallback
    await _emailService.SendEmailAsync(email, subject, body);
}
```

**Scenario 2: No Active Receipt Template**
```
GetActiveTemplateAsync() → null
    ↓
Use default hardcoded template
    ↓
Log: "Using default receipt template"
```

---

## 📊 Database Dependencies

### Required Tables

#### 1. RentalAgreementAcceptances
```sql
SELECT * FROM "RentalAgreementAcceptances" WHERE "BookingId" = {id};
```

**Key Columns:**
- `BookingId` (FK to Bookings)
- `RenterId` (FK to Users)
- `AgreementSnapshot` (text of agreement)
- `TemplateCode`, `TemplateVersion`
- `AcceptedAt` (timestamp)
- `IpAddress` (for audit)
- `AcceptedNoSmoking`, `AcceptedFinesAndTickets`, `AcceptedAccidentProcedure`

#### 2. ReceiptTemplates
```sql
SELECT * FROM "ReceiptTemplates" WHERE "IsActive" = true LIMIT 1;
```

**Key Columns:**
- `LogoUrl` (URL to company logo)
- `CompanyName`, `CompanyAddress`, `CompanyPhone`, `CompanyEmail`
- `CustomCss` (styling overrides)
- `ReceiptNumberPrefix` (e.g., "RVP")
- `TermsAndConditions` (footer text)
- `ShowLogo`, `ShowQrCode` (toggles)
- `IsActive` (only one active template allowed)

---

## 🧪 Testing Guide

### Test Rental Agreement Attachment

1. **Create a booking:**
   ```bash
   POST /api/v1/bookings
   # ... booking details
   ```

2. **Accept rental agreement:**
   ```bash
   POST /api/v1/renters/rental-agreements/{bookingId}/accept
   {
     "acceptedNoSmoking": true,
     "acceptedFinesAndTickets": true,
     "acceptedAccidentProcedure": true
   }
   ```

3. **Confirm booking (trigger email):**
   ```bash
   POST /api/v1/bookings/{id}/confirm-payment
   ```

4. **Check email:**
   - Subject: "Booking Confirmed - BK{reference}"
   - Attachment: `rental-agreement-BK{reference}.pdf`
   - Open PDF, verify all sections are present

### Test Receipt Attachment

1. **Complete pickup inspection:**
   ```bash
   POST /api/v1/inspections/pickup
   # ... inspection details
   ```

2. **Complete return inspection:**
   ```bash
   POST /api/v1/inspections/return
   # ... inspection details
   ```

3. **Check email:**
   - Subject: "Rental Completed - BK{reference}"
   - Attachment: `receipt-BK{reference}.pdf`
   - Open PDF, verify RyvePool branding and charges

### Test Admin Endpoints

1. **Get admin token:**
   ```bash
   POST /api/v1/auth/login
   { "email": "admin@ryvepool.com", "password": "..." }
   ```

2. **Download receipt:**
   ```bash
   curl -H "Authorization: Bearer {token}" \
        http://api/admin/receipts/bookings/123/pdf \
        -o test-receipt.pdf
   ```

3. **Verify:**
   - File downloads successfully
   - PDF opens without errors
   - Content matches booking data

---

## 🚨 Troubleshooting

### Issue: Email sent but no attachment

**Possible Causes:**
1. Rental agreement not signed (check `RentalAgreementAcceptances`)
2. Receipt generation failed (check logs for errors)

**Solution:**
```bash
# Check if agreement exists
SELECT * FROM "RentalAgreementAcceptances" WHERE "BookingId" = 123;

# Check if receipt template is active
SELECT * FROM "ReceiptTemplates" WHERE "IsActive" = true;

# Check container logs
az container logs --resource-group ryve-pool --name ghanarental-api-v1233
```

### Issue: PDF attachment is corrupted

**Possible Causes:**
1. Incorrect content-type (should be `application/pdf`)
2. Encoding issue in base64 conversion

**Solution:**
- Verify `EmailAttachment` content-type is `"application/pdf"`
- Check Resend API logs for encoding errors
- Test with small test PDF first

### Issue: Admin endpoint returns 403 Forbidden

**Possible Causes:**
1. User is not admin role
2. Token is expired

**Solution:**
```bash
# Verify user role in database
SELECT "Email", "Role" FROM "Users" WHERE "Email" = 'admin@ryvepool.com';

# Get fresh admin token
POST /api/v1/auth/login
```

---

## 📈 Performance Considerations

### PDF Generation Time
- **Rental Agreement:** ~50-100ms (text-based, fast)
- **Receipt:** ~200-500ms (HTML generation, template rendering)

### Email Sending Time
- **Without Attachment:** ~500ms-1s
- **With Attachment:** ~1-2s (base64 encoding + larger payload)

### Memory Usage
- Typical PDF: 50-200 KB
- Base64 encoded: ~130-260 KB
- Multiple attachments: Linear growth

**Recommendations:**
- Monitor container memory if sending bulk emails
- Consider async processing for batch notifications
- Implement rate limiting for admin bulk downloads

---

## 🔐 Security Best Practices

1. **Attachment Access Control**
   - Renters can only download their own receipts/agreements
   - Admins can access all receipts (audit trail logged)

2. **Sensitive Data**
   - No credit card numbers in receipts
   - IP addresses logged for agreement acceptance (audit trail)

3. **Email Security**
   - Attachments sent via encrypted HTTPS
   - Resend uses TLS for email transmission

4. **Data Privacy**
   - Agreement snapshots stored for legal compliance
   - Receipts generated on-demand (not permanently stored)

---

## 📚 Related Documentation

- [RECEIPT_SYSTEM_API.md](RECEIPT_SYSTEM_API.md) - Complete receipt API reference
- [FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md) - Frontend implementation
- [DEPLOYMENT_v1.233_SUMMARY.md](DEPLOYMENT_v1.233_SUMMARY.md) - Deployment details

---

## 💡 Tips & Tricks

### Custom Receipt Branding

Admins can customize receipts via API:

```bash
# Update active receipt template
PUT /api/v1/admin/receipt-templates/{id}
{
  "logoUrl": "https://your-logo.png",
  "companyName": "Your Company",
  "customCss": ".receipt-header { background: #yourcolor; }"
}
```

### Bulk Receipt Downloads (Future)

For now, use admin endpoint in loop:
```bash
for id in 101 102 103; do
  curl -H "Authorization: Bearer $TOKEN" \
       http://api/admin/receipts/bookings/$id/pdf \
       -o "receipt-$id.pdf"
done
```

### Testing Attachment in Development

Use a test email service like Mailtrap:
1. Configure Resend with Mailtrap SMTP
2. Send test booking confirmation
3. Check Mailtrap inbox for attachment
4. Download and verify PDF

---

**Last Updated:** December 28, 2025  
**Version:** 1.233  
**Maintained By:** RyvePool Engineering Team
