# Receipt System API Documentation

## Overview
The receipt system provides professional, customizable receipts with logo support. Admins can create and customize receipt templates, while users can download receipts for their bookings.

## Receipt Download Endpoints

### 📄 Get Receipt PDF (Authenticated)
**Endpoint:** `GET /api/v1/bookings/{bookingId}/receipt/pdf`  
**Authorization:** Required (Bearer token)  
**Access:** Renter, Owner, Driver, or Admin

**Description:** Download receipt as PDF for authenticated users.

**Example:**
```bash
curl -X GET "http://api.ryvepool.com/api/v1/bookings/{bookingId}/receipt/pdf" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  --output receipt.pdf
```

---

### 📄 Get Receipt PDF (Guest)
**Endpoint:** `POST /api/v1/bookings/{bookingId}/guest-receipt/pdf`  
**Authorization:** None (email verification)  
**Access:** Anyone with booking email

**Description:** Download receipt by providing booking email for verification.

**Request Body:**
```json
{
  "email": "customer@example.com"
}
```

**Example:**
```bash
curl -X POST "http://api.ryvepool.com/api/v1/bookings/{bookingId}/guest-receipt/pdf" \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@example.com"}' \
  --output receipt.pdf
```

---

### 📄 Get Receipt JSON
**Endpoint:** `GET /api/v1/bookings/{bookingId}/receipt`  
**Authorization:** Required  
**Returns:** Receipt data as JSON

---

### 📄 Get Receipt Text
**Endpoint:** `GET /api/v1/bookings/{bookingId}/receipt/text`  
**Authorization:** Required  
**Returns:** Plain text formatted receipt

---

## Admin Receipt Template Endpoints

### 🔧 Get All Templates
**Endpoint:** `GET /api/v1/admin/receipt-templates`  
**Authorization:** Admin only  
**Description:** List all receipt templates

**Response:**
```json
[
  {
    "id": "uuid",
    "templateName": "RyvePool Default",
    "logoUrl": "https://i.imgur.com/ryvepool-logo.png",
    "companyName": "RyvePool",
    "companyAddress": "Accra, Ghana",
    "companyPhone": "+233 XX XXX XXXX",
    "companyEmail": "support@ryvepool.com",
    "companyWebsite": "www.ryvepool.com",
    "termsAndConditions": "All rentals are subject to...",
    "isActive": true,
    "showLogo": true,
    "createdAt": "2026-01-06T00:00:00Z"
  }
]
```

---

### 🔧 Get Active Template
**Endpoint:** `GET /api/v1/admin/receipt-templates/active`  
**Authorization:** Admin only  
**Description:** Get currently active receipt template

---

### 🔧 Create Template
**Endpoint:** `POST /api/v1/admin/receipt-templates`  
**Authorization:** Admin only  
**Description:** Create new receipt template

**Request Body:**
```json
{
  "templateName": "Custom Template",
  "logoUrl": "https://your-cdn.com/logo.png",
  "companyName": "Your Company",
  "companyAddress": "Accra, Ghana",
  "companyPhone": "+233 XX XXX XXXX",
  "companyEmail": "support@yourcompany.com",
  "companyWebsite": "www.yourcompany.com",
  "termsAndConditions": "Your terms here",
  "customCss": "body { font-family: Arial; }",
  "isActive": true,
  "showLogo": true,
  "showQrCode": false,
  "receiptNumberPrefix": "INV"
}
```

---

### 🔧 Update Template
**Endpoint:** `PUT /api/v1/admin/receipt-templates/{id}`  
**Authorization:** Admin only  
**Description:** Update existing template (all fields optional)

**Request Body:**
```json
{
  "logoUrl": "https://new-logo.png",
  "companyName": "Updated Name",
  "isActive": false
}
```

---

### 🔧 Delete Template
**Endpoint:** `DELETE /api/v1/admin/receipt-templates/{id}`  
**Authorization:** Admin only  
**Description:** Delete receipt template

---

### 🔧 Activate Template
**Endpoint:** `POST /api/v1/admin/receipt-templates/{id}/activate`  
**Authorization:** Admin only  
**Description:** Activate template (deactivates all others)

---

### 🔧 Preview Receipt
**Endpoint:** `GET /api/v1/admin/receipt-templates/preview/{bookingId}`  
**Authorization:** Admin only  
**Description:** Preview how receipt looks with active template

---

## Customization Features

### Logo Support
- Upload your company logo to any CDN or image hosting service
- Set the `logoUrl` in the template
- Toggle visibility with `showLogo` field

### Custom Styling
Add custom CSS to the template:
```css
body {
    font-family: 'Your Font', Arial, sans-serif;
}
.header {
    background-color: #your-brand-color;
}
.pricing-table .total {
    background: #f0f0f0;
    font-size: 20px;
}
```

### Available Placeholders
All receipts support these dynamic placeholders:
- `{{receiptNumber}}` - Auto-generated receipt number
- `{{receiptDate}}` - Current date
- `{{bookingReference}}` - Booking reference code
- `{{customerName}}` - Customer full name
- `{{customerEmail}}` - Customer email
- `{{customerPhone}}` - Customer phone
- `{{pickupDateTime}}` - Rental start date/time
- `{{returnDateTime}}` - Rental end date/time
- `{{totalDays}}` - Number of rental days
- `{{vehicleName}}` - Vehicle make/model/year
- `{{plateNumber}}` - License plate
- `{{currency}}` - Currency code (GHS, USD, etc.)
- `{{vehicleAmount}}` - Vehicle rental cost
- `{{driverAmount}}` - Driver service cost
- `{{insuranceAmount}}` - Insurance cost
- `{{platformFee}}` - Platform fee
- `{{totalAmount}}` - Total amount
- `{{paymentStatus}}` - Payment status
- `{{paymentMethod}}` - Payment method used

### Booking Confirmation Email Placeholders
The `booking_confirmed` email template supports additional placeholders:
- `{{qr_code}}` - Base64-encoded QR code image for quick check-in (200x200px PNG)
- `{{inspection_link}}` - Direct URL to pickup inspection page
- `{{owner_name}}` - Vehicle owner's full name
- `{{owner_phone}}` - Owner contact phone
- `{{owner_address}}` - Pickup location address
- `{{pickup_instructions}}` - Special instructions from owner

---

## Default Template (RyvePool)

The system comes with a professional default template featuring:
- ✅ RyvePool logo (green/white branding)
- ✅ Clean, modern design
- ✅ Mobile-responsive layout
- ✅ Print-friendly styling
- ✅ Professional color scheme (#2d7d5d green)
- ✅ Detailed pricing breakdown
- ✅ Payment status badges
- ✅ Terms and conditions footer

---

## Frontend Integration Examples

### React/Next.js
```typescript
// Download receipt for authenticated user
async function downloadReceipt(bookingId: string) {
  const response = await fetch(
    `/api/v1/bookings/${bookingId}/receipt/pdf`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  
  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `receipt-${bookingId}.pdf`;
  a.click();
}

// Download receipt for guest
async function downloadGuestReceipt(bookingId: string, email: string) {
  const response = await fetch(
    `/api/v1/bookings/${bookingId}/guest-receipt/pdf`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email })
    }
  );
  
  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `receipt-${bookingId}.pdf`;
  a.click();
}
```

---

## Admin Dashboard Integration

### Template Management UI
```typescript
// Get all templates
const templates = await fetch('/api/v1/admin/receipt-templates', {
  headers: { 'Authorization': `Bearer ${adminToken}` }
}).then(r => r.json());

// Create new template
await fetch('/api/v1/admin/receipt-templates', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${adminToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    templateName: "Holiday Special",
    logoUrl: "https://cdn.example.com/holiday-logo.png",
    companyName: "RyvePool",
    isActive: false,
    showLogo: true
  })
});

// Activate template
await fetch(`/api/v1/admin/receipt-templates/${templateId}/activate`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${adminToken}` }
});

// Preview receipt
window.open(
  `/api/v1/admin/receipt-templates/preview/${bookingId}`,
  '_blank'
);
```

---

## Migration

Run the SQL migration to add receipt templates:
```bash
psql -h ryve-postgres-new.postgres.database.azure.com \
     -U ryveadmin \
     -d ghanarentaldb \
     -f add-receipt-templates-table.sql
```

Or use the PostgreSQL extension in VS Code to run:
```sql
-- Execute: add-receipt-templates-table.sql
```

---

## Future Enhancements
- 📱 QR Code support for receipt verification
- 🖨️ Multi-language support
- 📧 Email receipt as attachment
- 💾 PDF generation library (QuestPDF) for true PDFs
- 🎨 Template marketplace
- 📊 Receipt analytics
