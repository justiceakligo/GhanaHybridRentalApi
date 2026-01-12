# Integration Partner Application System & API Key Expiry - Deployment Guide v1.248

## ⚠️ Critical: Two Separate Partner Systems

**DO NOT CONFUSE:**

1. **Partners** (existing - DO NOT MODIFY)
   - Regular business partners on your website
   - Table: `Partners` with `IsActive`, `IsVerified`, `Metadata`
   - Endpoints: `/api/v1/admin/partner-applications/{id}/approve` (unchanged)
   - No API access - just business listings

2. **IntegrationPartners** (new - this deployment)
   - Third-party businesses with API access
   - Table: `IntegrationPartners` with `ApiKey`, `WebhookUrl`
   - Endpoints: `/api/v1/admin/integration-partner-applications/{id}/approve`
   - Full API access to create bookings programmatically

**This deployment ONLY affects IntegrationPartners. Your existing Partners feature is completely untouched.**

## Overview
This deployment adds a **public self-service application system** and **API key expiry management** for **IntegrationPartners** (API partners). Businesses can apply for API access, and admins can manage API key lifecycles.

## Features Implemented

### 1. Public Partner Application Endpoint
- **POST /api/v1/partner-applications** (public, no auth required)
- Businesses can submit applications with full business details
- Auto-generates unique application reference (PA-YYYY-NNNNNN format)
- Validates email uniqueness and format
- Applications start as `Active=false` (pending approval)

### 2. Enhanced IntegrationPartner Application Management
- **POST /api/v1/admin/integration-partner-applications/{id}/approve** - Approve with API key generation
- **POST /api/v1/admin/integration-partner-applications/{id}/reject** - Reject and delete application
- Applications list endpoint shows all pending IntegrationPartner applications

### 3. API Key Expiry System
- **ApiKeyExpiresAt** field on IntegrationPartner (nullable DateTime)
- **POST /api/v1/admin/integration-partners/{partnerId}/set-expiry** - Set/clear key expiry
- **POST /api/v1/admin/integration-partners/{partnerId}/renew-key** - Generate new key with new expiry
- Automatic expiry validation on every partner API request
- Partners receive clear expiry error messages

### 4. Updated Partner Authentication
- All partner API endpoints now validate API key expiry automatically
- Returns 401 with detailed message when key is expired
- LastUsedAt timestamp updated on every successful request

## Database Changes

### New Columns on IntegrationPartners Table
```sql
-- API key expiry tracking
ApiKeyExpiresAt (timestamp, nullable) - NULL means no expiry

-- Application/contact information
ContactPerson (varchar 512, nullable)
Email (varchar 256, nullable)
Phone (varchar 32, nullable)
Website (varchar 512, nullable)
RegistrationNumber (varchar 128, nullable)
Description (varchar 2000, nullable)
ApplicationReference (varchar 64, nullable) - e.g., PA-2026-001234
AdminNotes (text, nullable) - Internal admin notes

-- Indexes
IX_IntegrationPartners_ApplicationReference
IX_IntegrationPartners_Email
IX_IntegrationPartners_ApiKeyExpiresAt
```

## Migration Script
**File:** `add-partner-application-fields.sql`

```sql
-- Add API key expiry
ALTER TABLE "IntegrationPartners" 
ADD COLUMN "ApiKeyExpiresAt" timestamp with time zone NULL;

-- Add application/contact fields
ALTER TABLE "IntegrationPartners" 
ADD COLUMN "ContactPerson" varchar(512) NULL,
ADD COLUMN "Email" varchar(256) NULL,
ADD COLUMN "Phone" varchar(32) NULL,
ADD COLUMN "Website" varchar(512) NULL,
ADD COLUMN "RegistrationNumber" varchar(128) NULL,
ADD COLUMN "Description" varchar(2000) NULL,
ADD COLUMN "ApplicationReference" varchar(64) NULL,
ADD COLUMN "AdminNotes" text NULL;

-- Create indexes
CREATE INDEX "IX_IntegrationPartners_ApplicationReference" 
ON "IntegrationPartners" ("ApplicationReference");

CREATE INDEX "IX_IntegrationPartners_Email" 
ON "IntegrationPartners" ("Email");

CREATE INDEX "IX_IntegrationPartners_ApiKeyExpiresAt" 
ON "IntegrationPartners" ("ApiKeyExpiresAt");

COMMENT ON COLUMN "IntegrationPartners"."ApiKeyExpiresAt" IS 'API key expiry date (null = no expiry)';
COMMENT ON COLUMN "IntegrationPartners"."ApplicationReference" IS 'Application reference number (e.g., PA-2026-001234)';
COMMENT ON COLUMN "IntegrationPartners"."AdminNotes" IS 'Admin notes about partner application/account';
```

## Files Modified

### 1. `Models/IntegrationPartner.cs`
- Added 9 new properties for application tracking and API key expiry
- All properties nullable to maintain backward compatibility

### 2. `Dtos/AdminDtos.cs`
- **PartnerApplicationRequest** - Public application submission DTO
- **PartnerApplicationResponse** - Application confirmation response
- **SetApiKeyExpiryRequest** - Admin sets API key expiry
- **RenewApiKeyRequest** - Admin renews API key with new expiry
- Updated **IntegrationPartnerResponse** with new fields

### 3. `Endpoints/IntegrationPartnerEndpoints.cs`
- **SubmitPartnerApplicationAsync** - Public endpoint for application submission
- **SetApiKeyExpiryAsync** - Admin sets/clears API key expiry
- **RenewApiKeyAsync** - Admin generates new API key with optional expiry
- Updated all response DTOs to include new fields

### 4. `Endpoints/AdminEndpoints.cs`
- **ApprovePartnerApplicationAsync** - Now generates API key with optional expiry
- **RejectPartnerApplicationAsync** - Fixed to work with IntegrationPartners
- Both endpoints accept optional `expiryDays` query parameter

### 5. `Endpoints/WebhookEndpoints.cs`
- Added **PartnerAuthHelper** class for centralized API key validation
- All partner endpoints now validate API key expiry automatically
- Better error messages for expired keys

## API Endpoints Summary

### Public Endpoints
```
POST /api/v1/partner-applications
```
**Body:**
```json
{
  "businessName": "Accra Hotels Ltd",
  "businessType": "hotel",
  "contactPerson": "John Mensah",
  "email": "john@accrahotels.com",
  "phone": "+233244123456",
  "website": "https://accrahotels.com",
  "registrationNumber": "CS123456789",
  "description": "Leading hotel chain in Accra with 5 locations",
  "webhookUrl": "https://accrahotels.com/api/webhooks/ryverental"
}
```
**Response:**
```json
{
  "id": "guid",
  "applicationReference": "PA-2026-001234",
  "businessName": "Accra Hotels Ltd",
  "status": "pending",
  "submittedAt": "2026-01-15T10:30:00Z",
  "message": "Your application has been submitted successfully. Reference: PA-2026-001234. We'll review and contact you within 2-3 business days."
}
```

### Admin Endpoints
```
GET /api/v1/admin/partner-applications  (lists IntegrationPartner applications)
POST /api/v1/admin/integration-partner-applications/{partnerId}/approve?expiryDays=365
POST /api/v1/admin/integration-partner-applications/{partnerId}/reject
POST /api/v1/admin/integration-partners/{partnerId}/set-expiry
POST /api/v1/admin/integration-partners/{partnerId}/renew-key
```

**Approve IntegrationPartner Application (with 1 year expiry):**
```
POST /api/v1/admin/integration-partner-applications/{partnerId}/approve?expiryDays=365
```
**Response:**
```json
{
  "success": true,
  "message": "Integration partner 'Accra Hotels Ltd' approved and activated",
  "apiKey": "ghr_abc123...",
  "apiKeyExpiresAt": "2027-01-15T10:30:00Z",
  "partnerId": "guid",
  "applicationReference": "PA-2026-001234"
}
```

**Set API Key Expiry:**
```
POST /api/v1/admin/integration-partners/{partnerId}/set-expiry
Body: { "expiresAt": "2027-12-31T23:59:59Z" }  // null to remove expiry
```

**Renew API Key:**
```
POST /api/v1/admin/integration-partners/{partnerId}/renew-key
Body: { "expiryDays": 365 }  // null for no expiry
```

## Deployment Steps

### 1. Pre-Deployment (Azure PostgreSQL)
```powershell
# Connect to Azure PostgreSQL
$env:PGPASSWORD = "your-password"
psql -h ryve-postgres-new.postgres.database.azure.com -U ryveadmin -d ghanarentaldb -f add-partner-application-fields.sql
```

**Verify migration:**
```sql
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'IntegrationPartners' 
AND column_name IN ('ApiKeyExpiresAt', 'ContactPerson', 'Email', 'ApplicationReference');
```

### 2. Build & Deploy
```powershell
# Build project
dotnet build GhanaHybridRentalApi.csproj --configuration Release

# Publish to Azure
dotnet publish -c Release

# Deploy to Azure Container Instance (or your deployment method)
az acr build --registry ghanarentalacr --image ryverental-api:v1.248 .
az container create --resource-group GhanaRental-RG --name ryverental-api ...
```

### 3. Post-Deployment Verification
Test the public application endpoint:
```powershell
$body = @{
    businessName = "Test Hotel"
    businessType = "hotel"
    contactPerson = "Test User"
    email = "test@example.com"
    phone = "+233244000000"
    description = "Test application"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://ryverental.info/api/v1/partner-applications" `
    -Method POST -Body $body -ContentType "application/json"
```

Test admin approval with expiry:
```powershell
$adminToken = "your-admin-jwt-token"
Invoke-RestMethod -Uri "https://ryverental.info/api/v1/admin/integration-partner-applications/{partnerId}/approve?expiryDays=365" `
    -Method POST -Headers @{ Authorization = "Bearer $adminToken" }
```

Test partner API with expired key:
```powershell
# Set expiry to past date
Invoke-RestMethod -Uri "https://ryverental.info/api/v1/admin/integration-partners/{partnerId}/set-expiry" `
    -Method POST -Body '{"expiresAt":"2020-01-01T00:00:00Z"}' `
    -ContentType "application/json" -Headers @{ Authorization = "Bearer $adminToken" }

# Try to use partner API - should fail with expiry message
Invoke-RestMethod -Uri "https://ryverental.info/api/v1/partner/vehicles" `
    -Method GET -Headers @{ "X-API-Key" = "ghr_testkey..." }
# Expected: 401 with "API key expired on 2020-01-01. Please contact support to renew."
```

## Admin Dashboard Updates Needed

### 1. Partner Applications List Page
**Path:** `dashboard.ryverental.com/partner-applications`

**Features to add:**
- Display pending applications (Active=false)
- Show: ApplicationReference, BusinessName, ContactPerson, Email, Phone, SubmittedAt
- Actions: Approve (with expiry input), Reject (with reason input)
- Filter by submission date
- Search by reference number or email

**API Call:**
```javascript
fetch('https://ryverental.info/api/v1/admin/partner-applications', {
  headers: { 'Authorization': `Bearer ${adminToken}` }
})
```

### 2. Active Partners List Page
**Path:** `dashboard.ryverental.com/integration-partners`

**Features to add:**
- Show API key expiry status (expires in X days, expired, no expiry)
- Highlight partners with keys expiring within 30 days
- Quick actions: Set Expiry, Renew Key, Regenerate Key
- Display: ApiKeyExpiresAt, Email, ContactPerson, Website
- Sort by expiry date

**API Call:**
```javascript
fetch('https://ryverental.info/api/v1/admin/integration-partners', {
  headers: { 'Authorization': `Bearer ${adminToken}` }
})
```

### 3. Partner Details/Edit Page
**New fields to display:**
- Application Reference (read-only)
- Contact Person (editable)
- Email (editable)
- Phone (editable)
- Website (editable)
- Registration Number (editable)
- Description (editable)
- Admin Notes (editable, admin-only)
- API Key Expires At (with Set/Clear button)

### 4. API Key Management Modal
**Actions:**
- **Set Expiry:** Date picker to set expiry date
- **Remove Expiry:** Button to clear expiry (set to NULL)
- **Renew Key:** Input for expiry days, generates new key
- **Regenerate Key:** Keeps same expiry, generates new key

## Frontend Application Form

Refer to `PARTNER_APPLICATION_FORM_SPEC.md` for complete React implementation.

**Quick Example:**
```jsx
async function submitApplication(formData) {
  const response = await fetch('https://ryverental.info/api/v1/partner-applications', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(formData)
  });
  
  const result = await response.json();
  
  if (response.ok) {
    // Show success with application reference
    alert(`Application submitted! Reference: ${result.applicationReference}`);
  } else {
    // Show error
    alert(result.error);
  }
}
```

## Email Notifications (Recommended)

### 1. Application Received (Auto-send to applicant)
```
Subject: Partner Application Received - {ApplicationReference}

Dear {ContactPerson},

Thank you for applying to become a RyveRental integration partner!

Application Reference: {ApplicationReference}
Business Name: {BusinessName}
Submitted: {SubmittedAt}

We've received your application and will review it within 2-3 business days. 
You'll receive an email once your application has been processed.

Need to make changes? Reply to this email with your application reference.

Best regards,
RyveRental Partnership Team
```

### 2. Application Approved (Auto-send to applicant)
```
Subject: Partner Application Approved - Welcome to RyveRental!

Dear {ContactPerson},

Congratulations! Your RyveRental partner application has been approved.

Business Name: {BusinessName}
Application Reference: {ApplicationReference}

**API Credentials:**
API Key: {ApiKey}
API Key Expires: {ApiKeyExpiresAt} (or "Never expires")

**Getting Started:**
1. Read the API documentation: https://docs.ryverental.info/partner-api
2. Test in sandbox: Use your API key with test endpoints
3. Webhook URL: {WebhookUrl}
4. Commission Rate: {CommissionPercent}%
5. Settlement Terms: Net {SettlementTermDays} days

**Important:**
- Keep your API key secure
- Your key expires on {ApiKeyExpiresAt} - we'll send a renewal reminder
- Contact support@ryverental.com for technical assistance

Welcome aboard!
RyveRental Partnership Team
```

### 3. Application Rejected (Auto-send to applicant)
```
Subject: Partner Application Update - {ApplicationReference}

Dear {ContactPerson},

Thank you for your interest in partnering with RyveRental.

After careful review, we're unable to approve your application at this time.

Application Reference: {ApplicationReference}
Business Name: {BusinessName}

Reason: {AdminNotes}

You're welcome to reapply in the future. If you have questions, please contact 
partnerships@ryverental.com with your application reference.

Best regards,
RyveRental Partnership Team
```

### 4. API Key Expiring Soon (Auto-send 30 days before expiry)
```
Subject: API Key Expiring Soon - Action Required

Dear {ContactPerson},

Your RyveRental partner API key is expiring soon.

Partner: {BusinessName}
API Key: {ApiKey} (last 8 chars only)
Expires: {ApiKeyExpiresAt}
Days Remaining: {DaysUntilExpiry}

**Action Required:**
Contact your account manager or email support@ryverental.com to renew your API key.

Without renewal, your integration will stop working after the expiry date.

RyveRental Partnership Team
```

## Monitoring & Alerts

### 1. Track Application Volume
```sql
SELECT 
    DATE_TRUNC('day', "CreatedAt") as date,
    COUNT(*) as applications,
    COUNT(CASE WHEN "Active" = true THEN 1 END) as approved,
    COUNT(CASE WHEN "Active" = false THEN 1 END) as pending
FROM "IntegrationPartners"
WHERE "CreatedAt" >= NOW() - INTERVAL '30 days'
GROUP BY DATE_TRUNC('day', "CreatedAt")
ORDER BY date DESC;
```

### 2. Monitor Expiring Keys
```sql
SELECT 
    "Name",
    "Email",
    "ApiKeyExpiresAt",
    EXTRACT(DAY FROM ("ApiKeyExpiresAt" - NOW())) as days_until_expiry
FROM "IntegrationPartners"
WHERE "Active" = true 
AND "ApiKeyExpiresAt" IS NOT NULL
AND "ApiKeyExpiresAt" > NOW()
AND "ApiKeyExpiresAt" < NOW() + INTERVAL '30 days'
ORDER BY "ApiKeyExpiresAt";
```

### 3. Track Expired Keys
```sql
SELECT 
    "Name",
    "Email",
    "ApiKeyExpiresAt",
    "LastUsedAt"
FROM "IntegrationPartners"
WHERE "Active" = true 
AND "ApiKeyExpiresAt" IS NOT NULL
AND "ApiKeyExpiresAt" < NOW()
ORDER BY "ApiKeyExpiresAt" DESC;
```

## Security Considerations

1. **API Key Storage:** Never log full API keys, always redact except last 8 chars
2. **Email Validation:** Validate email format and prevent duplicate applications
3. **Rate Limiting:** Consider adding rate limiting to public application endpoint
4. **CAPTCHA:** Add Cloudflare Turnstile to application form to prevent spam
5. **Webhook Security:** Validate partner webhooks use HTTPS
6. **Admin Authorization:** All admin endpoints require AdminOnly policy

## Rollback Plan

If issues occur:

```sql
-- Remove new columns (data loss!)
ALTER TABLE "IntegrationPartners" 
DROP COLUMN "ApiKeyExpiresAt",
DROP COLUMN "ContactPerson",
DROP COLUMN "Email",
DROP COLUMN "Phone",
DROP COLUMN "Website",
DROP COLUMN "RegistrationNumber",
DROP COLUMN "Description",
DROP COLUMN "ApplicationReference",
DROP COLUMN "AdminNotes";

DROP INDEX "IX_IntegrationPartners_ApplicationReference";
DROP INDEX "IX_IntegrationPartners_Email";
DROP INDEX "IX_IntegrationPartners_ApiKeyExpiresAt";
```

Then redeploy previous version (v1.247 or earlier).

## Testing Checklist

- [ ] Public application submission works
- [ ] Email uniqueness validation prevents duplicates
- [ ] Application reference generates correctly (PA-YYYY-NNNNNN)
- [ ] Admin can view pending applications
- [ ] Admin can approve with expiry (generates API key)
- [ ] Admin can approve without expiry (API key never expires)
- [ ] Admin can reject application (deletes record)
- [ ] Admin can set API key expiry on existing partner
- [ ] Admin can remove expiry (set to NULL)
- [ ] Admin can renew API key with new expiry
- [ ] Partner API requests fail with expired key
- [ ] Partner API requests succeed with valid non-expired key
- [ ] Partner API requests succeed with no expiry set
- [ ] LastUsedAt updates on every partner API call
- [ ] Error messages are clear and user-friendly

## Support

For issues or questions:
- Technical: support@ryverental.com
- Partnerships: partnerships@ryverental.com
- Documentation: https://docs.ryverental.info

## Version
**v1.248** - Partner Application System & API Key Expiry Management
**Date:** 2026-01-15
**Status:** Ready for Production
