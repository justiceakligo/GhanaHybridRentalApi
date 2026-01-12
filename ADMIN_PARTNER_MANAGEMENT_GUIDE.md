# Admin Quick Reference - IntegrationPartner Application Management

## ⚠️ Important: Two Different Partner Systems

**Partners (Regular Business Partners - NOT THIS GUIDE)**
- Regular business partners on your website
- Endpoints: `/api/v1/admin/partner-applications/{id}/approve`
- No API access - just business listings

**IntegrationPartners (API Partners - THIS GUIDE)**
- Third-party businesses with API access to create bookings
- Endpoints: `/api/v1/admin/integration-partner-applications/{id}/approve`
- Full API access with API keys and webhooks

**This guide is ONLY for IntegrationPartners (API partners).**

## Reviewing IntegrationPartner Applications

### View Pending IntegrationPartner Applications
```
GET /api/v1/admin/partner-applications
Authorization: Bearer {admin_token}
```
**Note:** This endpoint returns IntegrationPartner applications (Active=false), NOT regular Partner applications.

**Response:**
```json
{
  "total": 5,
  "page": 1,
  "pageSize": 50,
  "data": [
    {
      "id": "guid",
      "name": "Accra Hotels Ltd",
      "type": "hotel",
      "applicationReference": "PA-2026-001234",
      "email": "contact@accrahotels.com",
      "contactPerson": "John Mensah",
      "phone": "+233244123456",
      "website": "https://accrahotels.com",
      "description": "Leading hotel chain...",
      "active": false,
      "createdAt": "2026-01-15T10:30:00Z"
    }
  ]
}
```

### Approve IntegrationPartner Application

**Option 1: Approve with 1-year expiry**
```
POST /api/v1/admin/integration-partner-applications/{partnerId}/approve?expiryDays=365
Authorization: Bearer {admin_token}
```

**Option 2: Approve with no expiry**
```
POST /api/v1/admin/integration-partner-applications/{partnerId}/approve
Authorization: Bearer {admin_token}
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

**⚠️ Important:** Copy the API key and send it to the integration partner via secure channel (email). This is their authentication credential for API access.

### Reject IntegrationPartner Application
```
POST /api/v1/admin/integration-partner-applications/{partnerId}/reject
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "reason": "Incomplete business documentation provided"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Integration partner application 'Accra Hotels Ltd' rejected and removed",
  "applicationReference": "PA-2026-001234"
}
```

## Managing Existing IntegrationPartners

### View All IntegrationPartners
```
GET /api/v1/admin/integration-partners?active=true
Authorization: Bearer {admin_token}
```

### Set API Key Expiry

**Set expiry to specific date:**
```
POST /api/v1/admin/integration-partners/{partnerId}/set-expiry
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "expiresAt": "2027-12-31T23:59:59Z"
}
```

**Remove expiry (never expires):**
```
POST /api/v1/admin/integration-partners/{partnerId}/set-expiry
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "expiresAt": null
}
```

**Response:**
```json
{
  "partnerId": "guid",
  "apiKeyExpiresAt": "2027-12-31T23:59:59Z",
  "message": "API key will expire on 2027-12-31 23:59:59 UTC"
}
```

### Renew API Key

**Renew with 1-year expiry:**
```
POST /api/v1/admin/integration-partners/{partnerId}/renew-key
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "expiryDays": 365
}
```

**Renew with no expiry:**
```
POST /api/v1/admin/integration-partners/{partnerId}/renew-key
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "expiryDays": null
}
```

**Response:**
```json
{
  "id": "guid",
  "apiKey": "ghr_new_abc123...",
  "apiKeyExpiresAt": "2027-01-15T10:30:00Z",
  "message": "API key renewed successfully. Expires on 2027-01-15 10:30:00 UTC"
}
```

**⚠️ Important:** Old API key is immediately invalidated. Send new API key to partner.

### Regenerate API Key (Keep Same Expiry)
```
POST /api/v1/admin/integration-partners/{partnerId}/regenerate-key
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "id": "guid",
  "apiKey": "ghr_regenerated_abc123..."
}
```

## Common Expiry Durations

| Duration | expiryDays | Use Case |
|----------|------------|----------|
| 30 days | 30 | Trial partners |
| 3 months | 90 | Short-term partnerships |
| 6 months | 180 | Standard contracts |
| 1 year | 365 | Annual partnerships |
| 2 years | 730 | Long-term partners |
| No expiry | null | Permanent trusted partners |

## Monitoring Partner Keys

### Keys Expiring Soon
Run this query to find keys expiring in next 30 days:
```sql
SELECT 
    "Name",
    "Email",
    "ContactPerson",
    "ApiKeyExpiresAt",
    EXTRACT(DAY FROM ("ApiKeyExpiresAt" - NOW())) as days_remaining
FROM "IntegrationPartners"
WHERE "Active" = true 
AND "ApiKeyExpiresAt" IS NOT NULL
AND "ApiKeyExpiresAt" > NOW()
AND "ApiKeyExpiresAt" < NOW() + INTERVAL '30 days'
ORDER BY "ApiKeyExpiresAt";
```

### Expired Keys
Find partners with expired keys:
```sql
SELECT 
    "Name",
    "Email",
    "ContactPerson",
    "ApiKeyExpiresAt",
    "LastUsedAt"
FROM "IntegrationPartners"
WHERE "Active" = true 
AND "ApiKeyExpiresAt" IS NOT NULL
AND "ApiKeyExpiresAt" < NOW()
ORDER BY "ApiKeyExpiresAt" DESC;
```

### Never-Expiring Keys
Find partners with no expiry set:
```sql
SELECT 
    "Name",
    "Email",
    "ContactPerson",
    "CreatedAt",
    "LastUsedAt"
FROM "IntegrationPartners"
WHERE "Active" = true 
AND "ApiKeyExpiresAt" IS NULL
ORDER BY "CreatedAt" DESC;
```

## Workflow Examples

### Standard Application Approval
1. Review application in admin dashboard
2. Verify business details (website, registration number)
3. Approve with 1-year expiry: `?expiryDays=365`
4. Copy generated API key
5. Send welcome email with:
   - API key
   - Expiry date
   - API documentation link
   - Commission rate (15% default)
   - Settlement terms (Net 30 default)

### Renewing Expiring Key
1. Get notification 30 days before expiry
2. Contact partner to confirm renewal
3. Use renew-key endpoint with new expiry: `{ "expiryDays": 365 }`
4. Send partner new API key via secure channel
5. Confirm old key is deactivated

### Emergency Key Revocation
1. Set expiry to immediate past: `{ "expiresAt": "2020-01-01T00:00:00Z" }`
2. Partner API requests fail immediately
3. Contact partner about revocation
4. If issue resolved, use renew-key to issue new credentials

### Converting Trial to Permanent
1. Partner completes 30-day trial successfully
2. Remove expiry: `{ "expiresAt": null }`
3. Or set long-term expiry: `{ "expiryDays": 730 }` (2 years)
4. Notify partner of upgrade

## Email Templates

### Approval Email
```
Subject: RyveRental Partner Application Approved

Dear {ContactPerson},

Your RyveRental partner application has been approved!

Application Reference: {ApplicationReference}
Business: {BusinessName}

**API Credentials:**
API Key: {ApiKey}
Expires: {ApiKeyExpiresAt} ({DaysUntilExpiry} days from now)

**Your Settings:**
Commission Rate: {CommissionPercent}%
Settlement Terms: Net {SettlementTermDays} days
Webhook URL: {WebhookUrl}

**Getting Started:**
1. Read API docs: https://docs.ryverental.info/partner-api
2. Test your integration in sandbox
3. Contact support@ryverental.com for assistance

Keep your API key secure and never share it publicly.

Welcome to RyveRental!
Partnership Team
```

### Expiry Reminder Email (30 days before)
```
Subject: API Key Expiring Soon - {BusinessName}

Dear {ContactPerson},

Your RyveRental partner API key is expiring soon.

API Key: ...{Last8Chars}
Expires: {ApiKeyExpiresAt}
Days Remaining: {DaysUntilExpiry}

To avoid service interruption, please contact us to renew:
Email: partnerships@ryverental.com
Reference: {ApplicationReference}

Your partnership is important to us!
RyveRental Team
```

### Renewal Confirmation Email
```
Subject: API Key Renewed - {BusinessName}

Dear {ContactPerson},

Your RyveRental partner API key has been renewed.

**New Credentials:**
API Key: {NewApiKey}
Expires: {NewExpiresAt}

⚠️ Action Required:
Update your integration with the new API key. Your old key is no longer valid.

Questions? Contact support@ryverental.com

RyveRental Partnership Team
```

## Best Practices

### Security
- ✅ Always use HTTPS for API communication
- ✅ Set reasonable expiry dates (1 year standard)
- ✅ Send API keys via secure channels only
- ✅ Log all key generation/renewal activities
- ✅ Monitor for unusual partner activity

### Process
- ✅ Verify business registration before approval
- ✅ Set calendar reminders for expiring keys
- ✅ Contact partners before expiry
- ✅ Document rejection reasons clearly
- ✅ Keep admin notes updated

### Communication
- ✅ Send approval email immediately
- ✅ Remind partners 30 days before expiry
- ✅ Provide clear renewal process
- ✅ Respond to partner queries within 24 hours
- ✅ Document all partner interactions

## Troubleshooting

### Partner Can't Use API Key
1. Check if key has expired: Look at `ApiKeyExpiresAt`
2. Check if partner is active: `Active` should be `true`
3. Check if partner is using correct key format: Should start with `ghr_`
4. Check if partner is sending header: `X-API-Key: {key}`

### Application Not Showing in List
1. Check filter: Are you filtering for `active=false`?
2. Check pagination: Is application on another page?
3. Check database: Query IntegrationPartners where Active=false

### Can't Approve Application
1. Verify admin JWT token is valid
2. Check admin role in token claims
3. Verify partner ID is correct UUID
4. Check if application already approved

## Quick Links

- **Admin Dashboard:** https://dashboard.ryverental.com
- **API Documentation:** https://docs.ryverental.info
- **Support Email:** support@ryverental.com
- **Partnerships Email:** partnerships@ryverental.com

---

**Last Updated:** v1.248  
**For Technical Issues:** Contact DevOps Team
