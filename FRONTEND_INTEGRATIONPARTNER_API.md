# IntegrationPartner Application - Frontend API Reference

## Public Application Submission (No Authentication)

### Submit IntegrationPartner Application

**Endpoint:** `POST /api/v1/partner-applications`  
**Authentication:** None (public endpoint)  
**Purpose:** Businesses apply to become API integration partners

#### Request Body
```json
{
  "businessName": "Accra Grand Hotel",
  "businessType": "hotel",
  "contactPerson": "John Mensah",
  "email": "john@accragrand.com",
  "phone": "+233244123456",
  "website": "https://accragrand.com",
  "registrationNumber": "CS123456789",
  "description": "5-star hotel in Accra with 200 rooms. We want to offer car rentals to our guests through our booking system.",
  "webhookUrl": "https://accragrand.com/api/webhooks/ryverental"
}
```

#### Field Details
| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `businessName` | string | ✅ Yes | Legal business name | Max 256 chars |
| `businessType` | string | ✅ Yes | Type of business | Must be one of: `hotel`, `travel_agency`, `ota`, `tour_operator`, `car_rental`, `custom` |
| `contactPerson` | string | ✅ Yes | Name of contact person | Max 512 chars |
| `email` | string | ✅ Yes | Business email (must be unique) | Valid email format, checked for duplicates |
| `phone` | string | ✅ Yes | Business phone number | Max 32 chars |
| `website` | string | ❌ No | Business website URL | Max 512 chars, valid URL if provided |
| `registrationNumber` | string | ❌ No | Business registration number | Max 128 chars |
| `description` | string | ✅ Yes | Why you want API access | Max 2000 chars |
| `webhookUrl` | string | ❌ No | Your webhook endpoint URL | Max 512 chars, valid HTTPS URL if provided |

#### Success Response (201 Created)
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "applicationReference": "PA-2026-001234",
  "businessName": "Accra Grand Hotel",
  "status": "pending",
  "submittedAt": "2026-01-11T10:30:00Z",
  "message": "Your application has been submitted successfully. Reference: PA-2026-001234. We'll review and contact you within 2-3 business days."
}
```

#### Error Responses

**400 Bad Request - Invalid business type**
```json
{
  "error": "Invalid business type"
}
```

**400 Bad Request - Invalid email**
```json
{
  "error": "Invalid email address"
}
```

**400 Bad Request - Duplicate email**
```json
{
  "error": "An application with this email already exists"
}
```

### Frontend Example (React/TypeScript)

```typescript
interface IntegrationPartnerApplicationRequest {
  businessName: string;
  businessType: 'hotel' | 'travel_agency' | 'ota' | 'tour_operator' | 'car_rental' | 'custom';
  contactPerson: string;
  email: string;
  phone: string;
  website?: string;
  registrationNumber?: string;
  description: string;
  webhookUrl?: string;
}

interface IntegrationPartnerApplicationResponse {
  id: string;
  applicationReference: string;
  businessName: string;
  status: string;
  submittedAt: string;
  message: string;
}

async function submitIntegrationPartnerApplication(
  data: IntegrationPartnerApplicationRequest
): Promise<IntegrationPartnerApplicationResponse> {
  const response = await fetch('https://ryverental.info/api/v1/partner-applications', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to submit application');
  }

  return response.json();
}
```

---

## Admin Endpoints (Require Admin Authentication)

### 1. Get Pending Applications

**Endpoint:** `GET /api/v1/admin/integration-partners/applications?page=1&pageSize=50`  
**Authentication:** Bearer token (admin only)  
**Purpose:** List all pending IntegrationPartner applications (pending = Active=false)

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response (200 OK)
```json
{
  "total": 5,
  "page": 1,
  "pageSize": 50,
  "data": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Accra Grand Hotel",
      "type": "hotel",
      "apiKey": "",
      "referralCode": null,
      "webhookUrl": "https://accragrand.com/api/webhooks/ryverental",
      "active": false,
      "createdAt": "2026-01-11T10:30:00Z",
      "lastUsedAt": null,
      "apiKeyExpiresAt": null,
      "contactPerson": "John Mensah",
      "email": "john@accragrand.com",
      "phone": "+233244123456",
      "website": "https://accragrand.com",
      "applicationReference": "PA-2026-001234",
      "commissionPercent": 15.0,
      "settlementTermDays": 30
    }
  ]
}
```

---

### 2. Approve Application

**Endpoint:** `POST /api/v1/admin/integration-partner-applications/{partnerId}/approve?expiryDays=365`  
**Authentication:** Bearer token (admin only)  
**Purpose:** Approve IntegrationPartner application and generate API key

#### URL Parameters
- `partnerId` (required): The GUID from the application
- `expiryDays` (optional): Number of days until API key expires (omit for no expiry)

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Request Body
None

#### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Integration partner 'Accra Grand Hotel' approved and activated",
  "apiKey": "ghr_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz",
  "apiKeyExpiresAt": "2027-01-11T10:30:00Z",
  "partnerId": "123e4567-e89b-12d3-a456-426614174000",
  "applicationReference": "PA-2026-001234"
}
```

⚠️ **Important:** The `apiKey` is only returned once. Admin must send it to the partner via email.

#### Error Responses

**404 Not Found**
```json
{
  "error": "Integration partner application not found"
}
```

**400 Bad Request - Already approved**
```json
{
  "error": "Integration partner application already approved"
}
```

---

### 3. Reject Application

**Endpoint:** `POST /api/v1/admin/integration-partner-applications/{partnerId}/reject`  
**Authentication:** Bearer token (admin only)  
**Purpose:** Reject IntegrationPartner application and delete it

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Request Body
```json
{
  "reason": "Incomplete business documentation. Please provide valid business registration certificate."
}
```

#### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Integration partner application 'Accra Grand Hotel' rejected and removed",
  "applicationReference": "PA-2026-001234"
}
```

#### Error Response

**404 Not Found**
```json
{
  "error": "Integration partner application not found"
}
```

---

## Complete Frontend Flow

### Public Application Form Flow

1. **User fills form** → Collects all required fields
2. **Submit** → `POST /api/v1/partner-applications`
3. **Success** → Show reference number (PA-2026-001234) and confirmation message
4. **Error** → Show validation error (duplicate email, invalid type, etc.)

### Admin Dashboard Flow

1. **View applications** → `GET /api/v1/admin/integration-partners/applications`
2. **Filter** → Show only `active: false` (pending)
3. **Review** → Display application details
4. **Approve** → `POST /api/v1/admin/integration-partner-applications/{id}/approve?expiryDays=365`
   - Admin enters optional expiry days and confirms
   - Backend generates API key, saves to DB, and sends approval email to the partner
   - Show API key in modal for admin to copy (only shown once)
   - Copy to clipboard button
   - Auto-send email button (if manual send is available)
5. **Reject** → `POST /api/v1/admin/integration-partner-applications/{id}/reject`
   - Show reason textarea
   - Confirm deletion
   - Backend sends rejection email with provided reason

> Admin UX note: On approval the UI should show a clear message like: "Approved! API key sent to partner@email.com" and present the key in a copy-only modal.

---

## Business Type Options

Use these exact values in your dropdown/select:

```typescript
const businessTypes = [
  { value: 'hotel', label: 'Hotel' },
  { value: 'travel_agency', label: 'Travel Agency' },
  { value: 'ota', label: 'Online Travel Agency (OTA)' },
  { value: 'tour_operator', label: 'Tour Operator' },
  { value: 'car_rental', label: 'Car Rental Company' },
  { value: 'custom', label: 'Other' },
];
```

---

## Form Validation Rules

```typescript
const validationSchema = {
  businessName: {
    required: true,
    maxLength: 256,
  },
  businessType: {
    required: true,
    enum: ['hotel', 'travel_agency', 'ota', 'tour_operator', 'car_rental', 'custom'],
  },
  contactPerson: {
    required: true,
    maxLength: 512,
  },
  email: {
    required: true,
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    maxLength: 256,
  },
  phone: {
    required: true,
    maxLength: 32,
  },
  website: {
    required: false,
    pattern: /^https?:\/\/.+/,
    maxLength: 512,
  },
  registrationNumber: {
    required: false,
    maxLength: 128,
  },
  description: {
    required: true,
    minLength: 50,
    maxLength: 2000,
  },
  webhookUrl: {
    required: false,
    pattern: /^https:\/\/.+/,
    maxLength: 512,
  },
};
```

---

## Complete React Example

```tsx
import React, { useState } from 'react';

export default function IntegrationPartnerApplicationForm() {
  const [formData, setFormData] = useState({
    businessName: '',
    businessType: 'hotel',
    contactPerson: '',
    email: '',
    phone: '',
    website: '',
    registrationNumber: '',
    description: '',
    webhookUrl: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError('');
    setResult(null);

    try {
      const response = await fetch('https://ryverental.info/api/v1/partner-applications', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          businessName: formData.businessName,
          businessType: formData.businessType,
          contactPerson: formData.contactPerson,
          email: formData.email,
          phone: formData.phone,
          website: formData.website || undefined,
          registrationNumber: formData.registrationNumber || undefined,
          description: formData.description,
          webhookUrl: formData.webhookUrl || undefined,
        }),
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.error || 'Failed to submit application');
      }

      setResult(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (result) {
    return (
      <div className="success-message">
        <h2>Application Submitted Successfully!</h2>
        <p><strong>Reference Number:</strong> {result.applicationReference}</p>
        <p><strong>Business Name:</strong> {result.businessName}</p>
        <p><strong>Status:</strong> {result.status}</p>
        <p>{result.message}</p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit}>
      <h2>Become an Integration Partner</h2>
      
      {error && <div className="error">{error}</div>}
      
      <label>
        Business Name *
        <input
          type="text"
          value={formData.businessName}
          onChange={(e) => setFormData({ ...formData, businessName: e.target.value })}
          required
          maxLength={256}
        />
      </label>

      <label>
        Business Type *
        <select
          value={formData.businessType}
          onChange={(e) => setFormData({ ...formData, businessType: e.target.value })}
          required
        >
          <option value="hotel">Hotel</option>
          <option value="travel_agency">Travel Agency</option>
          <option value="ota">Online Travel Agency (OTA)</option>
          <option value="tour_operator">Tour Operator</option>
          <option value="car_rental">Car Rental Company</option>
          <option value="custom">Other</option>
        </select>
      </label>

      <label>
        Contact Person *
        <input
          type="text"
          value={formData.contactPerson}
          onChange={(e) => setFormData({ ...formData, contactPerson: e.target.value })}
          required
          maxLength={512}
        />
      </label>

      <label>
        Email *
        <input
          type="email"
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
          required
          maxLength={256}
        />
      </label>

      <label>
        Phone *
        <input
          type="tel"
          value={formData.phone}
          onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
          required
          maxLength={32}
          placeholder="+233244123456"
        />
      </label>

      <label>
        Website
        <input
          type="url"
          value={formData.website}
          onChange={(e) => setFormData({ ...formData, website: e.target.value })}
          maxLength={512}
          placeholder="https://yourbusiness.com"
        />
      </label>

      <label>
        Business Registration Number
        <input
          type="text"
          value={formData.registrationNumber}
          onChange={(e) => setFormData({ ...formData, registrationNumber: e.target.value })}
          maxLength={128}
        />
      </label>

      <label>
        Description (Why do you need API access?) *
        <textarea
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          required
          minLength={50}
          maxLength={2000}
          rows={5}
          placeholder="Describe your business and how you plan to use our API..."
        />
      </label>

      <label>
        Webhook URL (optional)
        <input
          type="url"
          value={formData.webhookUrl}
          onChange={(e) => setFormData({ ...formData, webhookUrl: e.target.value })}
          maxLength={512}
          placeholder="https://yourbusiness.com/api/webhooks/ryverental"
        />
      </label>

      <button type="submit" disabled={submitting}>
        {submitting ? 'Submitting...' : 'Submit Application'}
      </button>
    </form>
  );
}
```

---

## Key Points for Frontend

1. **Public endpoint** - No authentication needed for application submission
2. **Email must be unique** - Show clear error if duplicate
3. **Application reference** - Display prominently on success (user needs this)
4. **Business types** - Use exact values: `hotel`, `travel_agency`, `ota`, `tour_operator`, `car_rental`, `custom`
5. **Admin endpoints** - Use `/api/v1/admin/integration-partners/applications` for listing, and `/api/v1/admin/integration-partner-applications/{id}/approve` or `/api/v1/admin/integration-partner-applications/{id}/reject` for actions — **NOT** `/api/v1/admin/partner-applications/`
6. **API key** - Only shown once on approval, admin must copy it and the system should auto-send it in the approval email
7. **Expiry is optional** - Admin can approve without expiry (key never expires)
8. **Registration number** - `registrationNumber` is optional; max 128 chars (business registration or tax ID), include it if available. 

---

## Email Flow (Automated)

Summary of recommended automated emails:

1. **Application Received** (sent immediately when the public form is submitted)
   - Contents: applicationReference, summary of submitted data, expected review time (2-3 business days)
   - Purpose: Acknowledge receipt and reduce follow-up support requests

2. **Application Approved (Send API Key)** (sent when admin clicks Approve)
   - Generated when admin clicks Approve
   - Backend: generate API key, save to DB (hashed if you store hashes), set expiry if provided
   - Email: include the API key (shown only once), key expiry (or "Never"), application reference, getting-started link
   - UI: Admin sees success: "Approved! API key sent to partner@email.com" and a copy-only modal showing the key
   - Security: Mark key as deliverable only once; do not display again in the UI after approval

3. **Application Rejected** (sent when admin clicks Reject)
   - Contents: rejection reason and guidance for reapplying
   - Backend: delete application record (or mark as rejected with adminNotes) and send rejection email

Implementation notes:
- API key delivery: send in approval email (industry standard). Also present it once in the admin modal for copy.
- Emails should be sent via a trusted provider (Resend, SendGrid, AWS SES) and use templating for variables.
- Log all sends and delivery status. Add alerts if email delivery fails.

---

## Testing URLs

**Production:**
- Application submission: `https://ryverental.info/api/v1/partner-applications`
- Admin list: `https://ryverental.info/api/v1/admin/integration-partners/applications`
- Admin approve: `https://ryverental.info/api/v1/admin/integration-partner-applications/{id}/approve`
- Admin reject: `https://ryverental.info/api/v1/admin/integration-partner-applications/{id}/reject`

**Production:**
- Application submission: `https://ryverental.info/api/v1/partner-applications`
- Admin list: `https://ryverental.info/api/v1/admin/partner-applications`
- Admin approve: `https://ryverental.info/api/v1/admin/integration-partner-applications/{id}/approve`
- Admin reject: `https://ryverental.info/api/v1/admin/integration-partner-applications/{id}/reject`

**Test with cURL:**
```bash
# Submit application
curl -X POST https://ryverental.info/api/v1/partner-applications \
  -H "Content-Type: application/json" \
  -d '{
    "businessName": "Test Hotel",
    "businessType": "hotel",
    "contactPerson": "Test User",
    "email": "test@example.com",
    "phone": "+233244000000",
    "description": "Test application for integration partner"
  }'
```

---

**Last Updated:** January 11, 2026  
**Version:** v1.248
