# RyveRental Partner Integration Guide

## Overview

RyveRental provides a comprehensive Partner API that allows third-party platforms (hotels, travel agencies, booking sites, OTAs) to integrate vehicle rental services directly into their applications. Partners can display available vehicles, accept bookings, and earn commission on each transaction.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Authentication](#authentication)
3. [Integration Flow](#integration-flow)
4. [API Endpoints](#api-endpoints)
5. [Webhooks](#webhooks)
6. [Settlement & Payments](#settlement--payments)
7. [Testing & Go-Live](#testing--go-live)
8. [Support](#support)

---

## Getting Started

### Becoming a Partner

#### Step 1: Apply for Partnership

Submit a partnership application through our partner portal or contact our partnerships team:

**Application Requirements:**
- Business name and registration details
- Website URL
- Business type (hotel, travel agency, OTA, custom)
- Contact information
- Expected booking volume
- Integration use case

**Application Form Fields:**
```json
{
  "businessName": "Your Business Name",
  "businessType": "hotel|travel_agency|ota|custom",
  "website": "https://yourbusiness.com",
  "contactPerson": "John Doe",
  "email": "partner@yourbusiness.com",
  "phone": "+233 XX XXX XXXX",
  "country": "Ghana",
  "city": "Accra",
  "description": "Brief description of your business",
  "expectedMonthlyBookings": 50,
  "useCase": "How you plan to integrate our services"
}
```

#### Step 2: Review & Approval

- Our team reviews your application (typically 1-3 business days)
- You'll receive an email notification upon approval/rejection
- Approved partners receive:
  - API Key (for authentication)
  - Partner ID
  - Commission rate (default 15%, negotiable)
  - Settlement terms (default 30 days, negotiable)
  - Webhook URL setup instructions

#### Step 3: Integration Setup

1. Configure your webhook endpoint (optional but recommended)
2. Test API access using provided credentials
3. Implement booking flow in your application
4. Complete test bookings in sandbox environment
5. Request production access

---

## Authentication

All Partner API requests require authentication using an API Key sent in the request header.

### API Key Authentication

**Header Format:**
```
X-API-Key: your_api_key_here
```

**Example Request:**
```bash
curl -X GET "https://ryverental.info/api/v1/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"
```

### Security Best Practices

1. **Never expose your API key** in client-side code or public repositories
2. **Store API keys securely** using environment variables or secret management systems
3. **Use HTTPS** for all API requests
4. **Rotate keys periodically** (request new keys from partner dashboard)
5. **Monitor API usage** for suspicious activity

---

## Integration Flow

### Complete Booking Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        Partner Website                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Fetch Available Vehicles                                │
│ GET /api/v1/partner/vehicles?startDate=...&endDate=...          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Display Vehicles to Customer                            │
│ - Show vehicle details, photos, pricing                         │
│ - Calculate rental amount based on dates                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Fetch Protection Plans                                  │
│ GET /api/v1/partner/protection-plans                            │
│ - Display mandatory protection plan options                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: Validate Promo Code (Optional)                          │
│ POST /api/v1/partner/validate-promo                             │
│ - Show discount to customer before checkout                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 5: Customer Pays Partner                                   │
│ - Use your own payment gateway                                  │
│ - Collect full booking amount + your service fee                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 6: Create Booking                                          │
│ POST /api/v1/partner/bookings                                   │
│ - Booking created with status "confirmed" or "pending"          │
│ - Settlement record created for future payment                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 7: Receive Confirmation                                    │
│ - Booking reference (RV-2026-XXXXXXXX)                          │
│ - Settlement details (amount owed, due date)                    │
│ - Customer receives confirmation email                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Step 8: Webhook Notifications (if configured)                   │
│ - booking.completed (when rental ends)                          │
│ - booking.cancelled (if booking is cancelled)                   │
└─────────────────────────────────────────────────────────────────┘
```

### Monthly Settlement Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Month End: Settlement Period Closes                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ RyveRental Sends Invoice                                        │
│ - Total bookings: GHS 10,000                                    │
│ - Commission (15%): GHS 1,500                                   │
│ - Settlement due: GHS 8,500                                     │
│ - Due date: 30 days from invoice                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Partner Pays Settlement                                         │
│ - Bank transfer to RyveRental account                           │
│ - Reference: Settlement ID                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ RyveRental Confirms Payment                                     │
│ - Settlement marked as "paid"                                   │
│ - Next period begins                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### Base URL

**Production:** `https://ryverental.info`  
**Sandbox:** Contact partner support for sandbox access

---

### 1. Get Available Vehicles

Fetch a list of vehicles available for the specified dates.

**Endpoint:** `GET /api/v1/partner/vehicles`

**Headers:**
```
X-API-Key: your_api_key_here
```

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `startDate` | ISO 8601 DateTime | No | Rental start date (filters availability) |
| `endDate` | ISO 8601 DateTime | No | Rental end date (filters availability) |
| `categoryId` | GUID | No | Filter by vehicle category |
| `cityId` | GUID | No | Filter by city |

**Example Request:**
```bash
curl -X GET "https://ryverental.info/api/v1/partner/vehicles?startDate=2026-01-20T10:00:00Z&endDate=2026-01-25T10:00:00Z&cityId=123e4567-e89b-12d3-a456-426614174000" \
  -H "X-API-Key: your_api_key_here"
```

**Example Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "make": "Toyota",
    "model": "Corolla",
    "year": 2023,
    "transmission": "Automatic",
    "fuelType": "Petrol",
    "seatingCapacity": 5,
    "hasAC": true,
    "cityId": "123e4567-e89b-12d3-a456-426614174000",
    "category": {
      "name": "Economy",
      "defaultDailyRate": 150.00,
      "defaultDepositAmount": 500.00
    },
    "photos": [
      "https://ryverental.blob.core.windows.net/vehicles/toyota-corolla-1.jpg",
      "https://ryverental.blob.core.windows.net/vehicles/toyota-corolla-2.jpg"
    ]
  }
]
```

---

### 2. Get Protection Plans

Fetch available protection plans (mandatory for all bookings).

**Endpoint:** `GET /api/v1/partner/protection-plans`

**Headers:**
```
X-API-Key: your_api_key_here
```

**Example Request:**
```bash
curl -X GET "https://ryverental.info/api/v1/partner/protection-plans" \
  -H "X-API-Key: your_api_key_here"
```

**Example Response:**
```json
[
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "code": "BASIC",
    "name": "Basic Protection",
    "description": "Covers basic damages up to GHS 5,000",
    "pricingMode": "per_day",
    "dailyPrice": 25.00,
    "fixedPrice": 0.00,
    "minFee": 25.00,
    "maxFee": 200.00,
    "currency": "GHS",
    "includesMinorDamageWaiver": true,
    "minorWaiverCap": 1000.00,
    "deductible": 500.00,
    "exclusions": ["theft", "total_loss"],
    "isMandatory": true,
    "isDefault": true
  },
  {
    "id": "8d0e7780-8536-51ef-c05c-f18gd2g01bf8",
    "code": "PREMIUM",
    "name": "Premium Protection",
    "description": "Comprehensive coverage with zero deductible",
    "pricingMode": "per_day",
    "dailyPrice": 50.00,
    "fixedPrice": 0.00,
    "minFee": 50.00,
    "maxFee": 400.00,
    "currency": "GHS",
    "includesMinorDamageWaiver": true,
    "minorWaiverCap": 5000.00,
    "deductible": 0.00,
    "exclusions": [],
    "isMandatory": false,
    "isDefault": false
  }
]
```

---

### 3. Validate Promo Code

Validate a promo code before booking to show discount to customer.

**Endpoint:** `POST /api/v1/partner/validate-promo`

**Headers:**
```
X-API-Key: your_api_key_here
Content-Type: application/json
```

**Request Body:**
```json
{
  "promoCode": "SUMMER2026",
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "bookingAmount": 1500.00,
  "rentalDays": 5
}
```

**Example Request:**
```bash
curl -X POST "https://ryverental.info/api/v1/partner/validate-promo" \
  -H "X-API-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "promoCode": "SUMMER2026",
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "bookingAmount": 1500.00,
    "rentalDays": 5
  }'
```

**Success Response:**
```json
{
  "isValid": true,
  "promoCode": "SUMMER2026",
  "promoType": "RenterDiscount",
  "discountType": "Percentage",
  "discountAmount": 150.00,
  "originalAmount": 1500.00,
  "finalAmount": 1350.00,
  "message": "10% discount applied"
}
```

**Error Response:**
```json
{
  "isValid": false,
  "error": "Promo code expired"
}
```

---

### 4. Create Booking

Create a new booking after customer payment.

**Endpoint:** `POST /api/v1/partner/bookings`

**Headers:**
```
X-API-Key: your_api_key_here
Content-Type: application/json
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `vehicleId` | GUID | Yes | Vehicle ID from vehicles endpoint |
| `pickupDateTime` | ISO 8601 DateTime | Yes | Rental start date/time |
| `returnDateTime` | ISO 8601 DateTime | Yes | Rental end date/time |
| `withDriver` | Boolean | Yes | Whether booking includes a driver |
| `renterEmail` | String | Yes | Customer email address |
| `renterPhone` | String | No | Customer phone number |
| `renterName` | String | Yes | Customer full name |
| `paymentMethod` | String | Yes | Payment method used (card, mobile_money, etc.) |
| `protectionPlanId` | GUID | No | Protection plan ID (uses default if not provided) |
| `promoCode` | String | No | Promo code for discount |

**Example Request:**
```bash
curl -X POST "https://ryverental.info/api/v1/partner/bookings" \
  -H "X-API-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "pickupDateTime": "2026-01-20T10:00:00Z",
    "returnDateTime": "2026-01-25T10:00:00Z",
    "withDriver": false,
    "renterEmail": "customer@example.com",
    "renterPhone": "+233 XX XXX XXXX",
    "renterName": "Jane Doe",
    "paymentMethod": "card",
    "protectionPlanId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "promoCode": "SUMMER2026"
  }'
```

**Success Response:**
```json
{
  "id": "9f8e7d6c-5b4a-3210-9876-543210fedcba",
  "bookingReference": "RV-2026-A1B2C3D4",
  "renterId": "1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p",
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "confirmed",
  "paymentStatus": "paid",
  "totalAmount": 1350.00,
  "pickupDateTime": "2026-01-20T10:00:00Z",
  "returnDateTime": "2026-01-25T10:00:00Z",
  "partner": {
    "commissionPercent": 15.00,
    "commissionAmount": 202.50,
    "settlementAmount": 1147.50,
    "settlementDueDate": "2026-02-19T00:00:00Z"
  }
}
```

**Booking Calculation Breakdown:**
```
Daily Rate:              GHS 150.00 x 5 days = GHS 750.00
Deposit Amount:          GHS 500.00
Protection Plan:         GHS 25.00 x 5 days  = GHS 125.00
Platform Fee (15%):      GHS 750.00 * 15%    = GHS 112.50
Subtotal:                                      GHS 1,487.50
Promo Discount (-10%):                       - GHS 148.75
Total Amount:                                  GHS 1,338.75

Your Commission (15%):   GHS 1,338.75 * 15%  = GHS 200.81
You Owe RyveRental:      GHS 1,338.75 - 200.81 = GHS 1,137.94
Due Date:                30 days from booking
```

**Error Responses:**

```json
// Vehicle not available
{
  "error": "Vehicle not available for requested dates"
}

// Invalid promo code
{
  "error": "Promo code error: Code has expired"
}

// Protection plan required
{
  "error": "Protection plan is required. Please select a valid protection plan."
}

// Missing fields
{
  "error": "RenterEmail and RenterName are required"
}
```

---

## Webhooks

Webhooks allow RyveRental to send real-time notifications about booking events to your server.

### Setup

1. Provide a webhook URL during partner registration or update it in your partner dashboard
2. Ensure your endpoint accepts POST requests
3. Verify webhook authenticity using the `X-API-Key` header
4. Return HTTP 200 to acknowledge receipt

### Webhook Endpoint Configuration

**Your Webhook URL:** `https://yoursite.com/api/webhooks/ryverental`

**RyveRental will send:**
- `X-API-Key` header with your partner API key for verification
- JSON payload with event details

### Webhook Events

#### 1. Booking Created
**Event:** `booking.created`

Sent immediately after a booking is created via partner API.

**Payload:**
```json
{
  "event": "booking.created",
  "bookingId": "9f8e7d6c-5b4a-3210-9876-543210fedcba",
  "data": {
    "bookingReference": "RV-2026-A1B2C3D4",
    "status": "confirmed",
    "totalAmount": 1350.00,
    "pickupDateTime": "2026-01-20T10:00:00Z",
    "returnDateTime": "2026-01-25T10:00:00Z",
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "renterEmail": "customer@example.com"
  }
}
```

#### 2. Booking Completed
**Event:** `booking.completed`

Sent when the rental period ends and vehicle is returned.

**Payload:**
```json
{
  "event": "booking.completed",
  "bookingId": "9f8e7d6c-5b4a-3210-9876-543210fedcba",
  "data": {
    "bookingReference": "RV-2026-A1B2C3D4",
    "status": "completed",
    "completedAt": "2026-01-25T14:30:00Z",
    "finalAmount": 1350.00
  }
}
```

#### 3. Booking Cancelled
**Event:** `booking.cancelled`

Sent if a booking is cancelled by customer or admin.

**Payload:**
```json
{
  "event": "booking.cancelled",
  "bookingId": "9f8e7d6c-5b4a-3210-9876-543210fedcba",
  "data": {
    "bookingReference": "RV-2026-A1B2C3D4",
    "status": "cancelled",
    "cancelledAt": "2026-01-18T09:15:00Z",
    "cancellationReason": "Customer request",
    "refundAmount": 1350.00
  }
}
```

### Webhook Implementation Example (Node.js)

```javascript
const express = require('express');
const app = express();

app.use(express.json());

app.post('/api/webhooks/ryverental', (req, res) => {
  // Verify authenticity
  const apiKey = req.headers['x-api-key'];
  if (apiKey !== process.env.RYVERENTAL_API_KEY) {
    return res.status(401).json({ error: 'Unauthorized' });
  }

  const { event, bookingId, data } = req.body;

  // Handle different events
  switch (event) {
    case 'booking.created':
      console.log('New booking:', data.bookingReference);
      // Update your database, send customer notifications, etc.
      break;

    case 'booking.completed':
      console.log('Booking completed:', data.bookingReference);
      // Trigger customer feedback, update analytics, etc.
      break;

    case 'booking.cancelled':
      console.log('Booking cancelled:', data.bookingReference);
      // Process refund, notify customer, etc.
      break;

    default:
      console.log('Unknown event:', event);
  }

  // Acknowledge receipt
  res.status(200).json({ message: 'Webhook received' });
});

app.listen(3000, () => {
  console.log('Webhook server running on port 3000');
});
```

---

## Settlement & Payments

### How It Works

1. **Partner collects payment** from customer for full booking amount
2. **Partner keeps commission** (default 15%, negotiable)
3. **Partner owes RyveRental** the remaining amount (85%)
4. **Settlement period** is monthly (default 30 days, negotiable)
5. **Invoice sent** at end of each month with total owed
6. **Partner pays** via bank transfer within settlement terms

### Settlement Calculation Example

**Booking Details:**
- Total Amount Collected: GHS 10,000
- Partner Commission (15%): GHS 1,500
- Settlement Amount Owed: GHS 8,500

**Monthly Invoice:**
```
RyveRental Partner Settlement Invoice
Partner: Your Business Name
Period: January 2026
Invoice Date: February 1, 2026
Due Date: March 3, 2026 (30 days)

Total Bookings: 20
Gross Revenue: GHS 10,000.00
Commission Earned (15%): GHS 1,500.00
Net Settlement Due: GHS 8,500.00

Payment Instructions:
Bank: Ecobank Ghana
Account Name: RyveRental Ltd
Account Number: XXXXXXXXXX
Reference: SETTLEMENT-JAN-2026-PARTNER-XXX
```

### Payment Terms

| Settlement Term | Description | Default |
|----------------|-------------|---------|
| **Daily** | Payment due next business day | No |
| **Weekly** | Payment due end of week | No |
| **Bi-weekly** | Payment due every 2 weeks | No |
| **Monthly** | Payment due end of month + grace period | **Yes (30 days)** |
| **Quarterly** | Payment due end of quarter | No |

### Late Payment

- **Grace Period:** 7 days after due date
- **Late Fee:** 2% per week (max 10%)
- **Suspension:** Account suspended after 30 days overdue
- **Collections:** Debt sent to collections after 60 days

---

## Testing & Go-Live

### Sandbox Environment

Contact partner support to request sandbox access:
- **Email:** partners@ryverental.com
- **Phone:** +233 XX XXX XXXX

**Sandbox Features:**
- Test API keys with limited rate limits
- Mock vehicle inventory
- No actual settlements or payments
- Full webhook support
- Test promo codes

### Pre-Launch Checklist

- [ ] API key received and stored securely
- [ ] Vehicle listing endpoint tested
- [ ] Protection plans endpoint tested
- [ ] Promo code validation tested
- [ ] Booking creation tested with all scenarios
- [ ] Webhook endpoint configured and tested
- [ ] Error handling implemented
- [ ] Payment collection flow integrated
- [ ] Customer notification system ready
- [ ] Settlement payment process documented
- [ ] Legal agreements signed

### Go-Live Process

1. Submit go-live request via partner dashboard
2. RyveRental team reviews integration
3. Production API credentials issued
4. Switch from sandbox to production URLs
5. Monitor first few bookings closely
6. Gradual rollout recommended

---

## Support

### Partner Support Channels

**Email:** partners@ryverental.com  
**Phone:** +233 XX XXX XXXX  
**Hours:** Monday - Friday, 9:00 AM - 6:00 PM GMT

**Partner Dashboard:** https://dashboard.ryverental.com/partners

### Technical Support

- **API Status:** https://status.ryverental.com
- **Developer Docs:** https://docs.ryverental.com
- **API Changelog:** https://docs.ryverental.com/changelog
- **Postman Collection:** Available on request

### Service Level Agreement (SLA)

| Issue Severity | Response Time | Resolution Time |
|---------------|---------------|-----------------|
| Critical (API Down) | 30 minutes | 2 hours |
| High (Booking Failures) | 2 hours | 8 hours |
| Medium (Feature Issues) | 8 hours | 2 business days |
| Low (General Questions) | 24 hours | 5 business days |

---

## Rate Limits

| Endpoint | Rate Limit | Burst Limit |
|----------|------------|-------------|
| GET /partner/vehicles | 100 requests/minute | 200 requests/minute |
| GET /partner/protection-plans | 100 requests/minute | 200 requests/minute |
| POST /partner/validate-promo | 50 requests/minute | 100 requests/minute |
| POST /partner/bookings | 20 requests/minute | 40 requests/minute |

**Rate Limit Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 75
X-RateLimit-Reset: 1640995200
```

**Rate Limit Exceeded Response:**
```json
{
  "error": "Rate limit exceeded",
  "retryAfter": 60
}
```

---

## Best Practices

### 1. Caching
- Cache vehicle lists for 5-15 minutes
- Cache protection plans for 1 hour
- Invalidate cache when booking fails due to availability

### 2. Error Handling
- Implement exponential backoff for retries
- Log all API errors with request IDs
- Display user-friendly error messages
- Have fallback flow for API failures

### 3. Security
- Use environment variables for API keys
- Implement webhook signature verification
- Validate all user inputs
- Use HTTPS for all requests

### 4. Performance
- Parallelize API calls where possible
- Implement request timeouts (30 seconds recommended)
- Monitor API response times
- Report slow endpoints to support

### 5. User Experience
- Show real-time availability
- Display accurate pricing breakdowns
- Provide booking confirmations immediately
- Send customer confirmation emails

---

## Frequently Asked Questions

**Q: How long does partner approval take?**  
A: Typically 1-3 business days after submitting a complete application.

**Q: Can I negotiate commission rates?**  
A: Yes, commission rates are negotiable based on expected volume and partnership terms.

**Q: What happens if a customer cancels?**  
A: You refund the customer, and we adjust your next settlement invoice accordingly.

**Q: Can I white-label the booking experience?**  
A: Yes, the Partner API is designed for full white-labeling of the booking flow.

**Q: Do you provide customer support for partner bookings?**  
A: Yes, RyveRental handles all rental operations including customer support, vehicle coordination, and issue resolution.

**Q: How do I track my earnings?**  
A: Access your partner dashboard at https://dashboard.ryverental.com/partners for real-time analytics and settlement tracking.

**Q: Can I offer my own promo codes?**  
A: Yes, we can create partner-specific promo codes. Contact partner support to set these up.

**Q: What payment methods do you accept for settlements?**  
A: Bank transfer (local and international), mobile money, and ACH transfers.

---

## Appendix

### Vehicle Categories

| Category | Description | Typical Daily Rate |
|----------|-------------|-------------------|
| Economy | Small, fuel-efficient cars | GHS 100-150 |
| Compact | Mid-size sedans | GHS 150-200 |
| Standard | Full-size sedans | GHS 200-250 |
| SUV | Sport utility vehicles | GHS 250-350 |
| Luxury | Premium vehicles | GHS 350-500+ |
| Van | 7+ passenger vehicles | GHS 300-400 |

### Cities Served

- Accra
- Kumasi
- Takoradi
- Tamale
- Cape Coast
- Tema

### Payment Methods Accepted

- Credit/Debit Card (Visa, Mastercard)
- Mobile Money (MTN, Vodafone, AirtelTigo)
- Bank Transfer
- Cash (for select partners)

### Booking Statuses

| Status | Description |
|--------|-------------|
| `pending` | Awaiting admin approval (if AutoConfirmBookings = false) |
| `confirmed` | Booking approved, customer can pick up vehicle |
| `active` | Rental in progress |
| `completed` | Rental ended, vehicle returned |
| `cancelled` | Booking cancelled |

### Settlement Statuses

| Status | Description |
|--------|-------------|
| `pending` | Settlement invoice generated, payment due |
| `paid` | Partner has paid settlement |
| `overdue` | Payment past due date |
| `disputed` | Settlement under dispute |

---

**Document Version:** 1.0  
**Last Updated:** January 11, 2026  
**Next Review:** April 11, 2026

For the latest version of this document and API updates, visit: https://docs.ryverental.com/partners
