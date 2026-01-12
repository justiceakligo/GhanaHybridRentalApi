# Admin IntegrationPartner Management - Frontend API Reference

> ⚠️ **IMPORTANT:** This document is ONLY for **IntegrationPartners** (API integration partners with API keys).  
> This is **NOT** for regular **Partners** (business partners on your website).  
> These are two completely separate systems.

---

## What is an IntegrationPartner?

**IntegrationPartners** are businesses that integrate with your API to create bookings programmatically:
- Hotels offering car rentals to their guests
- Travel agencies with booking systems
- Online Travel Agencies (OTAs)
- Tour operators with automated bookings

**They receive:**
- API key for authentication
- Access to booking API endpoints
- Webhook notifications
- Commission-based revenue sharing

**Table:** `IntegrationPartners`  
**Authentication:** API key (`X-API-Key` header)

---

## Admin Dashboard Endpoints

All endpoints require admin authentication (`Authorization: Bearer <admin_token>`)

### 1. List Pending IntegrationPartner Applications

**Endpoint:** `GET /api/v1/admin/integration-partners/applications`  
**Purpose:** Get all pending IntegrationPartner applications (not yet approved)

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Query Parameters
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | integer | No | 1 | Page number |
| `pageSize` | integer | No | 50 | Items per page |

#### Response (200 OK)
```json
{
  "total": 3,
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
      "registrationNumber": "CS123456789",
      "description": "5-star hotel in Accra with 200 rooms. We want to offer car rentals to our guests.",
      "applicationReference": "PA-2026-001234",
      "adminNotes": null,
      "commissionPercent": 15.0,
      "settlementTermDays": 30
    }
  ]
}
```

#### Key Fields for Admin Review
- `applicationReference`: Unique reference (e.g., PA-2026-001234)
- `active`: `false` = pending, `true` = approved
- `email`: Contact email (must be unique)
- `description`: Why they want API access
- `type`: Business type (hotel, travel_agency, ota, etc.)

---

### 2. List Active IntegrationPartners

**Endpoint:** `GET /api/v1/admin/integration-partners`  
**Purpose:** Get all approved and active IntegrationPartners

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response (200 OK)
```json
{
  "total": 12,
  "data": [
    {
      "id": "987fcdeb-51a2-43d1-b789-123456789abc",
      "name": "Golden Tulip Hotel",
      "type": "hotel",
      "apiKey": "ghr_xyz789...",
      "active": true,
      "createdAt": "2025-12-15T08:00:00Z",
      "lastUsedAt": "2026-01-10T14:22:00Z",
      "apiKeyExpiresAt": "2026-12-15T08:00:00Z",
      "contactPerson": "Sarah Osei",
      "email": "sarah@goldentulip.gh",
      "phone": "+233201234567",
      "commissionPercent": 15.0,
      "settlementTermDays": 30,
      "totalBookings": 45,
      "totalRevenue": 18500.00
    }
  ]
}
```

#### Key Indicators
- `lastUsedAt`: Recent activity shows partner is using the API
- `apiKeyExpiresAt`: `null` = never expires, date = expiry date
- `totalBookings`: Number of bookings created via API
- `totalRevenue`: Total revenue from this partner

---

### 3. Approve IntegrationPartner Application

**Endpoint:** `POST /api/v1/admin/integration-partner-applications/{partnerId}/approve`  
**Purpose:** Approve application, generate API key, activate partner

#### URL Parameters
- `{partnerId}`: The UUID from the application (required)

#### Query Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `expiryDays` | integer | No | Days until API key expires (omit for no expiry) |

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Example Requests

**Approve with 1-year expiry:**
```
POST /api/v1/admin/integration-partner-applications/123e4567-e89b-12d3-a456-426614174000/approve?expiryDays=365
```

**Approve with no expiry:**
```
POST /api/v1/admin/integration-partner-applications/123e4567-e89b-12d3-a456-426614174000/approve
```

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

⚠️ **CRITICAL:** The `apiKey` is only returned once. Admin must:
1. Copy it immediately
2. Send it to the partner via email
3. Partner must store it securely

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
  "error": "Integration partner already approved and activated"
}
```

---

### 4. Reject IntegrationPartner Application

**Endpoint:** `POST /api/v1/admin/integration-partner-applications/{partnerId}/reject`  
**Purpose:** Reject application with reason and delete the record

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Request Body
```json
{
  "reason": "Incomplete business documentation. Please provide a valid business registration certificate and reapply."
}
```

#### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Integration partner application 'Accra Grand Hotel' rejected and removed",
  "applicationReference": "PA-2026-001234",
  "rejectionReason": "Incomplete business documentation. Please provide a valid business registration certificate and reapply."
}
```

**Note:** The application record is permanently deleted. Partner must reapply if they want to try again.

---

### 5. Set API Key Expiry

**Endpoint:** `POST /api/v1/admin/integration-partners/{partnerId}/set-expiry`  
**Purpose:** Set or clear expiry date for existing API key

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Request Body

**Set expiry to specific date:**
```json
{
  "expiryDate": "2027-06-30T23:59:59Z"
}
```

**Clear expiry (key never expires):**
```json
{
  "expiryDate": null
}
```

#### Success Response (200 OK)
```json
{
  "success": true,
  "message": "API key expiry updated for 'Accra Grand Hotel'",
  "apiKeyExpiresAt": "2027-06-30T23:59:59Z"
}
```

---

### 6. Renew API Key

**Endpoint:** `POST /api/v1/admin/integration-partners/{partnerId}/renew-key`  
**Purpose:** Generate new API key (old one becomes invalid)

#### Request Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Request Body
```json
{
  "expiryDays": 365
}
```

**Or for no expiry:**
```json
{
  "expiryDays": null
}
```

#### Success Response (200 OK)
```json
{
  "success": true,
  "message": "New API key generated for 'Accra Grand Hotel'",
  "apiKey": "ghr_newkey123abc456def789ghi012jkl345mno678pqr901stu234",
  "apiKeyExpiresAt": "2027-01-11T15:00:00Z"
}
```

⚠️ **CRITICAL:** 
- Old API key is immediately invalidated
- New API key is only shown once
- Partner must update their system immediately

---

## Frontend Implementation Guide

### Admin Dashboard Page Structure

```
/admin/integration-partners
├── /applications (pending applications)
│   ├── List view (table)
│   ├── Approve modal
│   └── Reject modal
│
├── /active (approved partners)
│   ├── List view (table)
│   ├── Details page
│   └── API key management
```

---

### React Example: Pending Applications Page

```tsx
import React, { useState, useEffect } from 'react';

interface IntegrationPartnerApplication {
  id: string;
  name: string;
  type: string;
  applicationReference: string;
  contactPerson: string;
  email: string;
  phone: string;
  description: string;
  createdAt: string;
}

export default function IntegrationPartnerApplicationsPage() {
  const [applications, setApplications] = useState<IntegrationPartnerApplication[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchApplications();
  }, []);

  const fetchApplications = async () => {
    const response = await fetch('https://ryverental.info/api/v1/admin/integration-partners/applications', {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
      },
    });
    const data = await response.json();
    setApplications(data.data);
    setLoading(false);
  };

  const handleApprove = async (partnerId: string, expiryDays?: number) => {
    const url = expiryDays
      ? `https://ryverental.info/api/v1/admin/integration-partner-applications/${partnerId}/approve?expiryDays=${expiryDays}`
      : `https://ryverental.info/api/v1/admin/integration-partner-applications/${partnerId}/approve`;

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
      },
    });

    const result = await response.json();
    
    if (result.success) {
      // Show API key to admin (they must copy it)
      alert(`Partner approved! API Key (copy now): ${result.apiKey}`);
      
      // Refresh list
      fetchApplications();
    }
  };

  const handleReject = async (partnerId: string, reason: string) => {
    const response = await fetch(`https://ryverental.info/api/v1/admin/integration-partner-applications/${partnerId}/reject`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ reason }),
    });

    const result = await response.json();
    
    if (result.success) {
      alert('Application rejected');
      fetchApplications();
    }
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <h1>Pending IntegrationPartner Applications</h1>
      <p className="info">
        These are businesses applying for API access. Review and approve/reject.
      </p>

      <table>
        <thead>
          <tr>
            <th>Reference</th>
            <th>Business Name</th>
            <th>Type</th>
            <th>Contact</th>
            <th>Email</th>
            <th>Applied</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {applications.map((app) => (
            <tr key={app.id}>
              <td>{app.applicationReference}</td>
              <td>{app.name}</td>
              <td>{app.type}</td>
              <td>{app.contactPerson}</td>
              <td>{app.email}</td>
              <td>{new Date(app.createdAt).toLocaleDateString()}</td>
              <td>
                <button onClick={() => handleApprove(app.id, 365)}>
                  Approve (1 year)
                </button>
                <button onClick={() => handleApprove(app.id)}>
                  Approve (no expiry)
                </button>
                <button onClick={() => {
                  const reason = prompt('Rejection reason:');
                  if (reason) handleReject(app.id, reason);
                }}>
                  Reject
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

### React Example: Active IntegrationPartners Page

```tsx
import React, { useState, useEffect } from 'react';

interface ActiveIntegrationPartner {
  id: string;
  name: string;
  type: string;
  email: string;
  active: boolean;
  lastUsedAt: string | null;
  apiKeyExpiresAt: string | null;
  totalBookings: number;
  totalRevenue: number;
}

export default function ActiveIntegrationPartnersPage() {
  const [partners, setPartners] = useState<ActiveIntegrationPartner[]>([]);

  useEffect(() => {
    fetchPartners();
  }, []);

  const fetchPartners = async () => {
    const response = await fetch('https://ryverental.info/api/v1/admin/integration-partners', {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
      },
    });
    const data = await response.json();
    setPartners(data.data);
  };

  const handleRenewKey = async (partnerId: string, expiryDays?: number) => {
    const response = await fetch(`https://ryverental.info/api/v1/admin/integration-partners/${partnerId}/renew-key`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ expiryDays }),
    });

    const result = await response.json();
    
    if (result.success) {
      alert(`New API Key (copy now): ${result.apiKey}`);
      fetchPartners();
    }
  };

  const getExpiryStatus = (expiresAt: string | null) => {
    if (!expiresAt) return { text: 'Never', color: 'green' };
    
    const daysLeft = Math.ceil((new Date(expiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
    
    if (daysLeft < 0) return { text: 'Expired', color: 'red' };
    if (daysLeft < 30) return { text: `${daysLeft} days left`, color: 'orange' };
    return { text: `${daysLeft} days left`, color: 'green' };
  };

  return (
    <div>
      <h1>Active IntegrationPartners</h1>
      <p className="info">
        These are approved API integration partners with active API keys.
      </p>

      <table>
        <thead>
          <tr>
            <th>Business Name</th>
            <th>Type</th>
            <th>Email</th>
            <th>Last Used</th>
            <th>Key Expiry</th>
            <th>Bookings</th>
            <th>Revenue</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {partners.map((partner) => {
            const expiryStatus = getExpiryStatus(partner.apiKeyExpiresAt);
            
            return (
              <tr key={partner.id}>
                <td>{partner.name}</td>
                <td>{partner.type}</td>
                <td>{partner.email}</td>
                <td>
                  {partner.lastUsedAt 
                    ? new Date(partner.lastUsedAt).toLocaleDateString()
                    : 'Never'}
                </td>
                <td style={{ color: expiryStatus.color }}>
                  {expiryStatus.text}
                </td>
                <td>{partner.totalBookings}</td>
                <td>GHS {partner.totalRevenue.toFixed(2)}</td>
                <td>
                  <button onClick={() => handleRenewKey(partner.id, 365)}>
                    Renew Key
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
```

---

## UI/UX Guidelines

### Pending Applications Table
- **Sort by:** `createdAt` (newest first)
- **Highlight:** Applications older than 7 days (overdue review)
- **Quick actions:** Approve/Reject buttons inline
- **Details modal:** Show full description, business details

### Active Partners Table
- **Sort by:** `lastUsedAt` (most active first)
- **Color coding:**
  - 🟢 Green: Key never expires or >90 days left
  - 🟠 Orange: Key expires in 30-90 days
  - 🔴 Red: Key expired or <30 days left
- **Quick actions:** Renew key, View details, Edit settings

### Approval Flow
1. Admin clicks "Approve"
2. Modal asks: "Set expiry?" (Yes: input days, No: never expires)
3. Confirm approval
4. Show API key in copyable modal
5. Auto-send email to partner (recommended)
6. Refresh applications list

### Rejection Flow
1. Admin clicks "Reject"
2. Modal asks: "Rejection reason?" (required textarea)
3. Confirm rejection
4. Record is deleted
5. Auto-send email to partner with reason (recommended)

---

## Email Templates (Recommended)

### 1. Application Received (Auto-send)
```
Subject: IntegrationPartner Application Received - PA-2026-001234

Hi [ContactPerson],

Thank you for applying to become a RyveRental IntegrationPartner.

Your application reference: PA-2026-001234

We'll review your application within 2-3 business days and contact you at [Email].

Best regards,
RyveRental Team
```

### 2. Application Approved (Send with API Key)
```
Subject: Welcome to RyveRental IntegrationPartner Program!

Hi [ContactPerson],

Great news! Your IntegrationPartner application has been approved.

Business: [BusinessName]
Reference: PA-2026-001234

API CREDENTIALS:
API Key: ghr_abc123... (copy and save securely)
Key Expires: January 11, 2027 (or "Never" if no expiry)

API Documentation: https://docs.ryverental.info
Support: api-support@ryverental.com

⚠️ IMPORTANT: Save your API key now. It won't be shown again.

Best regards,
RyveRental Team
```

### 3. Application Rejected
```
Subject: IntegrationPartner Application Update - PA-2026-001234

Hi [ContactPerson],

Thank you for your interest in the RyveRental IntegrationPartner program.

After review, we are unable to approve your application at this time.

Reason: [RejectionReason]

You're welcome to reapply once the above is addressed.

Best regards,
RyveRental Team
```

---

## Key Differences: IntegrationPartners vs Partners

| Feature | IntegrationPartner (API) | Partner (Website) |
|---------|-------------------------|-------------------|
| **Purpose** | API integration | Website listing |
| **Authentication** | API key | None |
| **Database Table** | `IntegrationPartners` | `Partners` |
| **Admin Endpoints** | `/admin/integration-partners/*` | `/admin/partner-applications/*` |
| **Application Flow** | Public form → Admin approval → API key | Different system |
| **Revenue Model** | Commission on bookings | Different model |
| **Key Features** | API access, webhooks, settlements | Website display |

---

## Testing with Postman/cURL

### Get Pending Applications
```bash
curl -X GET "https://ryverental.info/api/v1/admin/integration-partners/applications" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### Approve Application (1 year expiry)
```bash
curl -X POST "https://ryverental.info/api/v1/admin/integration-partner-applications/123e4567-e89b-12d3-a456-426614174000/approve?expiryDays=365" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### Reject Application
```bash
curl -X POST "https://ryverental.info/api/v1/admin/integration-partner-applications/123e4567-e89b-12d3-a456-426614174000/reject" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Incomplete documentation"}'
```

---

**Document Version:** v1.248  
**Last Updated:** January 11, 2026  
**System:** IntegrationPartners (API Integration) ONLY
