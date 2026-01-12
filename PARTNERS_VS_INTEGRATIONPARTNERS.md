# Partners vs IntegrationPartners - Quick Reference

## Two Separate Systems - DO NOT CONFUSE!

Your RyveRental system has **TWO completely different partner systems**:

---

## 1. Partners (Regular Business Partners)

**Purpose:** Regular business relationships - partners you work with and display on your website

**Database Table:** `Partners`

**Key Fields:**
- `Name`, `Description`, `Metadata`
- `IsActive`, `IsVerified`
- `Logo`, `Website`, `ContactInfo`

**Endpoints:**
- `GET /api/v1/admin/partners`
- `POST /api/v1/admin/partner-applications/{id}/approve`
- `POST /api/v1/admin/partner-applications/{id}/reject`

**What They Do:**
- Listed on your website as trusted partners
- No API access
- No technical integration
- Just business relationships (e.g., hotels you recommend, tour operators you work with)

**Example Use Cases:**
- Display partner hotels on website
- Show trusted tour operators
- List recommended car rental agencies
- Feature partner businesses

---

## 2. IntegrationPartners (API Integration Partners)

**Purpose:** Third-party businesses that integrate with your API to create bookings programmatically

**Database Table:** `IntegrationPartners`

**Key Fields:**
- `Name`, `Type`, `ApiKey`, `ApiKeyExpiresAt`
- `WebhookUrl`, `Active`
- `CommissionPercent`, `SettlementTermDays`
- `ContactPerson`, `Email`, `Phone`
- `ApplicationReference`

**Endpoints:**
- `GET /api/v1/admin/integration-partners`
- `POST /api/v1/admin/integration-partner-applications/{id}/approve`
- `POST /api/v1/admin/integration-partner-applications/{id}/reject`
- `POST /api/v1/admin/integration-partners/{id}/set-expiry`
- `POST /api/v1/admin/integration-partners/{id}/renew-key`

**Public Endpoint:**
- `POST /api/v1/partner-applications` (businesses apply for API access)

**What They Do:**
- Integrate via API to create bookings
- Receive API key for authentication
- Can create bookings on behalf of customers
- Commission-based revenue sharing
- Automated settlement tracking

**Example Use Cases:**
- Hotel booking system creates car rental bookings
- Travel agency platform integrates your vehicles
- OTA (Online Travel Agency) lists your inventory
- Tour operator creates bookings via API

**Partner API Endpoints (they use these):**
- `POST /api/v1/partner/bookings` - Create booking
- `GET /api/v1/partner/vehicles` - Get available vehicles
- `GET /api/v1/partner/protection-plans` - Get protection plans
- `POST /api/v1/partner/validate-promo` - Validate promo codes

---

## Key Differences at a Glance

| Feature | Partners | IntegrationPartners |
|---------|----------|---------------------|
| **Purpose** | Business listing | API integration |
| **Database** | `Partners` table | `IntegrationPartners` table |
| **API Access** | ❌ No | ✅ Yes (with API key) |
| **Authentication** | N/A | API key (X-API-Key header) |
| **Can Create Bookings** | ❌ No | ✅ Yes (via API) |
| **Commission/Revenue** | ❌ No | ✅ Yes (tracked) |
| **Settlements** | ❌ No | ✅ Yes (automated) |
| **Webhooks** | ❌ No | ✅ Yes (optional) |
| **Listed on Website** | ✅ Yes | ❌ No |
| **Approval Endpoint** | `/admin/partner-applications/{id}/approve` | `/admin/integration-partner-applications/{id}/approve` |
| **Application Form** | Internal/manual | Public self-service |

---

## When to Use Which?

### Use **Partners** when:
- You want to feature a business on your website
- It's a business relationship (referral, partnership)
- No technical integration needed
- Just want to list them as a trusted partner
- Example: "Our Trusted Partners" page on website

### Use **IntegrationPartners** when:
- A business wants to integrate your API
- They need to create bookings programmatically
- They have their own booking system/platform
- You need API key authentication
- Commission-based revenue sharing
- Example: A hotel wants to offer car rentals to their guests via API

---

## Common Scenarios

### Scenario 1: Hotel wants to be listed on your site
**Answer:** Use **Partners**
- Add them via admin panel
- Upload their logo
- Display on "Our Partners" page
- No API involved

### Scenario 2: Hotel wants to create car bookings from their booking system
**Answer:** Use **IntegrationPartners**
- They submit public application (POST /api/v1/partner-applications)
- Admin approves (generates API key)
- They integrate using Partner API
- Bookings tracked with commission

### Scenario 3: Tour operator wants referral arrangement
**Answer:** Use **Partners**
- Manual business relationship
- No API needed
- Listed on website

### Scenario 4: OTA wants to list your vehicles on their platform
**Answer:** Use **IntegrationPartners**
- Technical API integration required
- They pull your vehicle inventory
- Create bookings via API
- Automated settlements

---

## Database Tables Summary

### Partners Table
```sql
CREATE TABLE "Partners" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(256),
    "Description" text,
    "IsActive" boolean,
    "IsVerified" boolean,
    "Metadata" jsonb,
    "Logo" varchar(512),
    "Website" varchar(512),
    "CreatedAt" timestamp,
    "UpdatedAt" timestamp
);
```

### IntegrationPartners Table
```sql
CREATE TABLE "IntegrationPartners" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(256),
    "Type" varchar(64),
    "ApiKey" varchar(256),
    "ApiKeyExpiresAt" timestamp,
    "WebhookUrl" varchar(512),
    "Active" boolean,
    "ReferralCode" varchar(64),
    "CommissionPercent" decimal,
    "SettlementTermDays" integer,
    "ContactPerson" varchar(512),
    "Email" varchar(256),
    "Phone" varchar(32),
    "Website" varchar(512),
    "ApplicationReference" varchar(64),
    "CreatedAt" timestamp,
    "LastUsedAt" timestamp
);
```

---

## Admin Dashboard Organization

### Partners Section
- Path: `/dashboard/partners`
- Shows: Regular business partners
- Actions: Add, Edit, Activate, Deactivate
- No API keys shown

### IntegrationPartners Section
- Path: `/dashboard/integration-partners`
- Shows: API integration partners
- Actions: Approve applications, Set expiry, Renew key
- Shows API keys, expiry dates, commission rates

### Applications (IntegrationPartners)
- Path: `/dashboard/integration-partner-applications`
- Shows: Pending API access applications
- Actions: Approve with expiry, Reject

---

## Important Reminders

1. **Never mix the two systems** - they serve completely different purposes
2. **Regular Partners have no API access** - they're just business listings
3. **IntegrationPartners require API keys** - they integrate programmatically
4. **Different approval endpoints** - use the correct one for each type
5. **v1.248 ONLY affects IntegrationPartners** - Partners feature unchanged

---

## Questions?

- **"Can a Partner also be an IntegrationPartner?"**  
  Yes! You can have the same business in both tables if they're both listed on your site AND use your API.

- **"Which one is new in v1.248?"**  
  The **IntegrationPartner application system** (public form + API key expiry). Regular Partners existed before and are unchanged.

- **"Can Partners create bookings?"**  
  No, only IntegrationPartners can create bookings via API. Regular Partners are just listings.

- **"Do IntegrationPartners appear on the website?"**  
  No, they're backend-only for API integration. If you want them on the site too, add them as a regular Partner.

---

**Last Updated:** v1.248  
**Date:** January 11, 2026
