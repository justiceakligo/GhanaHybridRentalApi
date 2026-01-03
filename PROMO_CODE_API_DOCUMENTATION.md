# Promo Code API Documentation

Complete API documentation for implementing promo code functionality in the frontend.

---

## 🔐 Authentication

All endpoints require authentication via Bearer token:
```
Authorization: Bearer {your_jwt_token}
```

---

## 📋 Table of Contents
1. [Admin Endpoints](#admin-endpoints)
2. [Owner Endpoints](#owner-endpoints)
3. [Renter Endpoints](#renter-endpoints)
4. [Common Response Codes](#common-response-codes)

---

# Admin Endpoints

Requires `admin` role.

## 1. Create Promo Code

**Endpoint:** `POST /api/v1/admin/promo-codes`

**Description:** Create a new promo code for renters or owners.

### Request Body

```json
{
  "code": "SUMMER2026",
  "description": "Summer sale - 15% off all bookings",
  "promoType": "Percentage",
  "discountValue": 15,
  "targetUserType": "Renter",
  "appliesTo": "TotalAmount",
  "minimumBookingAmount": 200,
  "maximumDiscountAmount": 500,
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-08-31T23:59:59Z",
  "maxTotalUses": 1000,
  "maxUsesPerUser": 1,
  "firstTimeUsersOnly": false,
  "categoryId": null,
  "cityId": null
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `code` | string | Yes | Unique promo code (will be stored uppercase) |
| `description` | string | No | Human-readable description |
| `promoType` | enum | Yes | `Percentage`, `FixedAmount`, `FreeAddon`, `CommissionReduction` |
| `discountValue` | decimal | Yes | Value depends on type (e.g., 15 for 15% or GHS 15) |
| `targetUserType` | enum | Yes | `Renter`, `Owner`, `Both` |
| `appliesTo` | enum | Yes | `TotalAmount`, `PlatformFee`, `ProtectionPlan`, `RentalAmount`, `Commission` |
| `minimumBookingAmount` | decimal | No | Minimum booking value to use code |
| `maximumDiscountAmount` | decimal | No | Cap on discount amount |
| `validFrom` | datetime | Yes | Start date (ISO 8601) |
| `validUntil` | datetime | Yes | End date (ISO 8601) |
| `maxTotalUses` | integer | No | Total uses across all users (null = unlimited) |
| `maxUsesPerUser` | integer | Yes | Times each user can use this code |
| `firstTimeUsersOnly` | boolean | Yes | Only for users with no previous bookings |
| `categoryId` | guid | No | Restrict to specific vehicle category |
| `cityId` | guid | No | Restrict to specific city |

### Success Response (201 Created)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SUMMER2026",
  "description": "Summer sale - 15% off all bookings",
  "promoType": "Percentage",
  "discountValue": 15,
  "targetUserType": "Renter",
  "appliesTo": "TotalAmount",
  "minimumBookingAmount": 200,
  "maximumDiscountAmount": 500,
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-08-31T23:59:59Z",
  "maxTotalUses": 1000,
  "maxUsesPerUser": 1,
  "currentTotalUses": 0,
  "isActive": true,
  "firstTimeUsersOnly": false,
  "isReferralCode": false,
  "referrerUserId": null,
  "categoryId": null,
  "cityId": null,
  "createdBy": "admin",
  "createdByUserId": "admin-user-guid",
  "createdAt": "2026-01-02T15:30:00Z",
  "updatedAt": "2026-01-02T15:30:00Z"
}
```

### Error Response (400 Bad Request)

```json
{
  "error": "Promo code already exists"
}
```

---

## 2. Get All Promo Codes

**Endpoint:** `GET /api/v1/admin/promo-codes`

**Description:** List all promo codes with pagination and filters.

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | integer | No | Page number (default: 1) |
| `pageSize` | integer | No | Items per page (default: 20) |
| `isActive` | boolean | No | Filter by active status |
| `targetUserType` | string | No | Filter by `Renter`, `Owner`, or `Both` |

### Example Request

```
GET /api/v1/admin/promo-codes?page=1&pageSize=20&isActive=true&targetUserType=Renter
```

### Success Response (200 OK)

```json
{
  "promoCodes": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "NEWYEAR2026",
      "description": "New Year 2026 - 20% off for new renters",
      "promoType": "Percentage",
      "discountValue": 20,
      "targetUserType": "Renter",
      "appliesTo": "TotalAmount",
      "minimumBookingAmount": null,
      "maximumDiscountAmount": null,
      "validFrom": "2026-01-01T00:00:00Z",
      "validUntil": "2026-01-31T23:59:59Z",
      "maxTotalUses": 1000,
      "maxUsesPerUser": 1,
      "currentTotalUses": 145,
      "isActive": true,
      "firstTimeUsersOnly": true,
      "isReferralCode": false,
      "referrerName": null,
      "categoryId": null,
      "categoryName": null,
      "cityId": null,
      "cityName": null,
      "createdAt": "2026-01-01T00:00:00Z"
    },
    {
      "id": "7fb95f64-1234-5678-b3fc-2c963f66afa7",
      "code": "WELCOME50",
      "description": "Welcome bonus - GHS 50 off first booking",
      "promoType": "FixedAmount",
      "discountValue": 50,
      "targetUserType": "Renter",
      "appliesTo": "TotalAmount",
      "minimumBookingAmount": null,
      "maximumDiscountAmount": null,
      "validFrom": "2026-01-01T00:00:00Z",
      "validUntil": "2026-12-31T23:59:59Z",
      "maxTotalUses": null,
      "maxUsesPerUser": 1,
      "currentTotalUses": 89,
      "isActive": true,
      "firstTimeUsersOnly": true,
      "isReferralCode": false,
      "referrerName": null,
      "categoryId": null,
      "categoryName": null,
      "cityId": null,
      "cityName": null,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ],
  "total": 12,
  "page": 1,
  "pageSize": 20
}
```

---

## 3. Get Promo Code by ID

**Endpoint:** `GET /api/v1/admin/promo-codes/{id}`

**Description:** Get detailed information about a specific promo code.

### Example Request

```
GET /api/v1/admin/promo-codes/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Success Response (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SUMMER2026",
  "description": "Summer sale - 15% off all bookings",
  "promoType": "Percentage",
  "discountValue": 15,
  "targetUserType": "Renter",
  "appliesTo": "TotalAmount",
  "minimumBookingAmount": 200,
  "maximumDiscountAmount": 500,
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-08-31T23:59:59Z",
  "maxTotalUses": 1000,
  "maxUsesPerUser": 1,
  "currentTotalUses": 342,
  "isActive": true,
  "firstTimeUsersOnly": false,
  "isReferralCode": false,
  "referrerName": null,
  "categoryId": null,
  "categoryName": null,
  "cityId": null,
  "cityName": null,
  "createdAt": "2026-01-02T15:30:00Z"
}
```

### Error Response (404 Not Found)

```json
{
  "error": "Promo code not found"
}
```

---

## 4. Update Promo Code

**Endpoint:** `PUT /api/v1/admin/promo-codes/{id}`

**Description:** Update an existing promo code (partial update supported).

### Request Body

```json
{
  "description": "Updated summer sale - 20% off!",
  "discountValue": 20,
  "validUntil": "2026-09-30T23:59:59Z",
  "isActive": true
}
```

### Success Response (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SUMMER2026",
  "description": "Updated summer sale - 20% off!",
  "promoType": "Percentage",
  "discountValue": 20,
  "targetUserType": "Renter",
  "appliesTo": "TotalAmount",
  "minimumBookingAmount": 200,
  "maximumDiscountAmount": 500,
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-09-30T23:59:59Z",
  "maxTotalUses": 1000,
  "maxUsesPerUser": 1,
  "currentTotalUses": 342,
  "isActive": true,
  "firstTimeUsersOnly": false,
  "isReferralCode": false,
  "createdAt": "2026-01-02T15:30:00Z",
  "updatedAt": "2026-01-02T16:45:00Z"
}
```

---

## 5. Deactivate Promo Code

**Endpoint:** `DELETE /api/v1/admin/promo-codes/{id}`

**Description:** Soft delete (deactivate) a promo code.

### Success Response (200 OK)

```json
{
  "message": "Promo code deactivated successfully"
}
```

---

## 6. Get Promo Code Usage History

**Endpoint:** `GET /api/v1/admin/promo-codes/{id}/usage`

**Description:** View who used a promo code and when.

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | integer | No | Page number (default: 1) |
| `pageSize` | integer | No | Items per page (default: 20) |

### Example Request

```
GET /api/v1/admin/promo-codes/3fa85f64-5717-4562-b3fc-2c963f66afa6/usage?page=1&pageSize=10
```

### Success Response (200 OK)

```json
{
  "usages": [
    {
      "id": "8ea85f64-1111-2222-b3fc-2c963f66afa8",
      "code": "SUMMER2026",
      "userEmail": "john@example.com",
      "userType": "renter",
      "bookingId": "9fa85f64-3333-4444-b3fc-2c963f66afa9",
      "bookingReference": "RV-2026-ABC123",
      "originalAmount": 850.00,
      "discountAmount": 127.50,
      "finalAmount": 722.50,
      "appliedTo": "TotalAmount",
      "referrerUserId": null,
      "referrerName": null,
      "referrerRewardAmount": null,
      "referrerRewardApplied": false,
      "usedAt": "2026-06-15T10:30:00Z"
    },
    {
      "id": "8ea85f64-5555-6666-b3fc-2c963f66afa8",
      "code": "SUMMER2026",
      "userEmail": "mary@example.com",
      "userType": "renter",
      "bookingId": "9fa85f64-7777-8888-b3fc-2c963f66afa9",
      "bookingReference": "RV-2026-DEF456",
      "originalAmount": 1200.00,
      "discountAmount": 180.00,
      "finalAmount": 1020.00,
      "appliedTo": "TotalAmount",
      "referrerUserId": null,
      "referrerName": null,
      "referrerRewardAmount": null,
      "referrerRewardApplied": false,
      "usedAt": "2026-06-16T14:20:00Z"
    }
  ],
  "total": 342,
  "page": 1,
  "pageSize": 10
}
```

---

## 7. Get Promo Code Analytics

**Endpoint:** `GET /api/v1/admin/promo-codes/{id}/analytics`

**Description:** Get comprehensive analytics for a promo code.

### Example Request

```
GET /api/v1/admin/promo-codes/3fa85f64-5717-4562-b3fc-2c963f66afa6/analytics
```

### Success Response (200 OK)

```json
{
  "code": "SUMMER2026",
  "totalUses": 342,
  "uniqueUsers": 298,
  "totalDiscountGiven": 43567.50,
  "averageDiscountPerUse": 127.39,
  "newCustomersAcquired": 156,
  "revenueGenerated": 284532.50,
  "usageByDate": [
    {
      "date": "2026-06-01",
      "uses": 12,
      "discountGiven": 1524.00
    },
    {
      "date": "2026-06-02",
      "uses": 18,
      "discountGiven": 2286.00
    },
    {
      "date": "2026-06-03",
      "uses": 25,
      "discountGiven": 3175.00
    }
  ]
}
```

---

# Owner Endpoints

Requires `owner` role.

## 1. Create Vehicle-Specific Promo Code

**Endpoint:** `POST /api/v1/owner/promo-codes`

**Description:** Create a promo code for a specific vehicle owned by you. The discount reduces your earnings but can attract more bookings.

### Request Body

```json
{
  "code": "SUMMER10",
  "description": "10% off my Honda Civic for summer",
  "vehicleId": "abc85f64-5717-4562-b3fc-2c963f66afa6",
  "promoType": "Percentage",
  "discountValue": 10,
  "appliesTo": "RentalAmount",
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-08-31T23:59:59Z",
  "maxTotalUses": 50,
  "maxUsesPerUser": 2
}
```

### Success Response (201 Created)

```json
{
  "id": "def85f64-9999-0000-b3fc-2c963f66afa6",
  "code": "SUMMER10",
  "description": "10% off my Honda Civic for summer",
  "vehicleId": "abc85f64-5717-4562-b3fc-2c963f66afa6",
  "vehicleName": "Honda Civic 2024",
  "promoType": "Percentage",
  "discountValue": 10,
  "appliesTo": "RentalAmount",
  "validFrom": "2026-06-01T00:00:00Z",
  "validUntil": "2026-08-31T23:59:59Z",
  "maxTotalUses": 50,
  "maxUsesPerUser": 2,
  "currentTotalUses": 0,
  "isActive": true,
  "createdAt": "2026-01-02T16:00:00Z"
}
```

### How Vehicle Discounts Work

When a renter uses `SUMMER10` on a Honda Civic with GHS 100/day for 3 days:
- Original rental amount: GHS 300
- Discount (10%): GHS 30
- **Renter pays: GHS 270 (for rental)**
- **Owner receives: GHS 270 (minus platform fee)**

The owner reduces their daily rate to attract bookings, directly reducing their earnings.

---

## 2. Get My Vehicle Promo Codes

**Endpoint:** `GET /api/v1/owner/promo-codes`

**Description:** List all promo codes you created for your vehicles.

### Success Response (200 OK)

```json
{
  "promoCodes": [
    {
      "id": "def85f64-9999-0000-b3fc-2c963f66afa6",
      "code": "SUMMER10",
      "description": "10% off my Honda Civic for summer",
      "vehicleId": "abc85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleName": "Honda Civic 2024",
      "promoType": "Percentage",
      "discountValue": 10,
      "appliesTo": "RentalAmount",
      "validFrom": "2026-06-01T00:00:00Z",
      "validUntil": "2026-08-31T23:59:59Z",
      "maxTotalUses": 50,
      "maxUsesPerUser": 2,
      "currentTotalUses": 12,
      "isActive": true,
      "totalDiscountGiven": 360.00,
      "totalBookingsGenerated": 12,
      "createdAt": "2026-01-02T16:00:00Z"
    }
  ],
  "total": 1
}
```

---

## 3. Update My Vehicle Promo Code

**Endpoint:** `PUT /api/v1/owner/promo-codes/{id}`

**Description:** Update your vehicle promo code.

### Request Body

```json
{
  "description": "Updated: 15% off my Honda Civic!",
  "discountValue": 15,
  "isActive": true
}
```

### Success Response (200 OK)

Same structure as create response with updated values.

---

## 4. Delete My Vehicle Promo Code

**Endpoint:** `DELETE /api/v1/owner/promo-codes/{id}`

**Description:** Deactivate your vehicle promo code.

### Success Response (200 OK)

```json
{
  "message": "Promo code deactivated successfully"
}
```

---

## 5. Generate Referral Code

**Endpoint:** `POST /api/v1/owner/referrals/generate`

**Description:** Generate a referral code to earn rewards when others use it.

### Request Body

```json
{
  "customCode": "JUSTICE2026",
  "rewardType": "Credit",
  "rewardValue": 50.00,
  "validUntil": "2027-01-01T00:00:00Z"
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customCode` | string | No | Custom code (auto-generated if null) |
| `rewardType` | enum | Yes | `Credit`, `CommissionReduction`, `Cash` |
| `rewardValue` | decimal | Yes | Reward amount in GHS |
| `validUntil` | datetime | No | Expiry date (1 year if null) |

### Success Response (200 OK)

```json
{
  "code": "JUSTICE2026",
  "message": "Referral code generated successfully"
}
```

### Auto-Generated Code Example

If `customCode` is null, the system generates: `{FIRSTNAME}{4-RANDOM-DIGITS}`

Example: User "Justice" → `JUSTICE5847`

---

## 6. Get My Referral Codes

**Endpoint:** `GET /api/v1/owner/referrals/codes`

**Description:** List all your referral codes.

### Success Response (200 OK)

```json
[
  {
    "code": "JUSTICE2026",
    "referrerName": "Justice Owusu",
    "rewardType": "Credit",
    "rewardValue": 50.00,
    "totalReferrals": 15,
    "totalRewardsEarned": 750.00,
    "validFrom": "2026-01-02T16:30:00Z",
    "validUntil": "2027-01-01T00:00:00Z",
    "isActive": true
  },
  {
    "code": "JUSTICE5847",
    "referrerName": "Justice Owusu",
    "rewardType": "Credit",
    "rewardValue": 25.00,
    "totalReferrals": 8,
    "totalRewardsEarned": 200.00,
    "validFrom": "2025-12-15T10:00:00Z",
    "validUntil": "2026-12-15T10:00:00Z",
    "isActive": true
  }
]
```

---

## 7. Get Referral Stats

**Endpoint:** `GET /api/v1/owner/referrals/stats`

**Description:** Get comprehensive referral statistics.

### Success Response (200 OK)

```json
{
  "totalReferrals": 23,
  "activeReferrals": 20,
  "totalRewardsEarned": 1150.00,
  "totalBookingsFromReferrals": 67,
  "referredUsers": [
    {
      "userId": "user1-guid",
      "name": "John Doe",
      "email": "john@example.com",
      "referralType": "renter",
      "rewardEarned": 50.00,
      "bookingsCompleted": 4,
      "referredAt": "2026-01-05T10:30:00Z",
      "status": "active"
    },
    {
      "userId": "user2-guid",
      "name": "Mary Smith",
      "email": "mary@example.com",
      "referralType": "owner",
      "rewardEarned": 75.00,
      "bookingsCompleted": 6,
      "referredAt": "2026-01-08T14:20:00Z",
      "status": "active"
    }
  ]
}
```

---

# Renter Endpoints

Requires authentication (any user role).

## 1. Validate Promo Code (Preview Discount)

**Endpoint:** `POST /api/v1/promo-codes/validate`

**Description:** Validate a promo code and preview the discount BEFORE creating a booking. Use this to show users how much they'll save.

### Request Body

```json
{
  "code": "SUMMER2026",
  "bookingAmount": 850.00,
  "categoryId": "cat-guid-suv",
  "cityId": "city-guid-accra",
  "tripDuration": 3
}
```

### Success Response (200 OK) - Valid Code

```json
{
  "isValid": true,
  "errorMessage": null,
  "promoCode": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "SUMMER2026",
    "description": "Summer sale - 15% off all bookings",
    "promoType": "Percentage",
    "discountValue": 15,
    "targetUserType": "Renter",
    "appliesTo": "TotalAmount",
    "minimumBookingAmount": 200.00,
    "maximumDiscountAmount": 500.00,
    "validFrom": "2026-06-01T00:00:00Z",
    "validUntil": "2026-08-31T23:59:59Z",
    "maxTotalUses": 1000,
    "maxUsesPerUser": 1,
    "currentTotalUses": 342,
    "isActive": true,
    "firstTimeUsersOnly": false,
    "isReferralCode": false,
    "referrerName": null,
    "categoryId": null,
    "categoryName": null,
    "cityId": null,
    "cityName": null,
    "createdAt": "2026-01-02T15:30:00Z"
  },
  "originalAmount": 850.00,
  "discountAmount": 127.50,
  "finalAmount": 722.50,
  "appliesTo": "TotalAmount"
}
```

### Response (200 OK) - Invalid Code

```json
{
  "isValid": false,
  "errorMessage": "This promo code has expired or is not yet valid",
  "promoCode": null,
  "originalAmount": 850.00,
  "discountAmount": 0,
  "finalAmount": 850.00,
  "appliesTo": ""
}
```

### Frontend Usage

```javascript
// When user enters promo code in booking form
async function validatePromoCode(code, bookingDetails) {
  const response = await fetch('/api/v1/promo-codes/validate', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      code: code,
      bookingAmount: bookingDetails.totalAmount,
      categoryId: bookingDetails.categoryId,
      cityId: bookingDetails.cityId,
      tripDuration: bookingDetails.days
    })
  });
  
  const result = await response.json();
  
  if (result.isValid) {
    // Show success message
    showSuccess(`Code applied! You save GHS ${result.discountAmount.toFixed(2)}`);
    // Update UI with new total
    updateTotal(result.finalAmount);
  } else {
    // Show error
    showError(result.errorMessage);
  }
}
```

---

## 2. Apply Promo Code to Booking

**Endpoint:** `POST /api/v1/bookings`

**Description:** Create a booking with a promo code applied.

### Request Body

```json
{
  "vehicleId": "abc85f64-5717-4562-b3fc-2c963f66afa6",
  "pickupDateTime": "2026-06-15T10:00:00Z",
  "returnDateTime": "2026-06-18T10:00:00Z",
  "withDriver": false,
  "driverId": null,
  "insurancePlanId": null,
  "protectionPlanId": "prot-guid-basic",
  "pickupLocation": {
    "address": "Kotoka Airport, Accra",
    "latitude": 5.605186,
    "longitude": -0.166786
  },
  "returnLocation": {
    "address": "Kotoka Airport, Accra",
    "latitude": 5.605186,
    "longitude": -0.166786
  },
  "paymentMethod": "mobile_money",
  "promoCode": "SUMMER2026"
}
```

### Success Response (201 Created)

```json
{
  "id": "booking-guid",
  "bookingReference": "RV-2026-ABC123",
  "renterId": "user-guid",
  "vehicleId": "abc85f64-5717-4562-b3fc-2c963f66afa6",
  "ownerId": "owner-guid",
  "status": "pending_payment",
  "pickupDateTime": "2026-06-15T10:00:00Z",
  "returnDateTime": "2026-06-18T10:00:00Z",
  "withDriver": false,
  "driverId": null,
  "driverAmount": null,
  "rentalAmount": 600.00,
  "depositAmount": 500.00,
  "platformFee": 60.00,
  "insuranceAmount": null,
  "protectionAmount": 45.00,
  "promoCodeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "promoCode": "SUMMER2026",
  "promoDiscountAmount": 127.50,
  "totalAmount": 1077.50,
  "paymentStatus": "pending",
  "paymentMethod": "mobile_money",
  "createdAt": "2026-01-02T17:00:00Z",
  "vehicle": {
    "id": "abc85f64-5717-4562-b3fc-2c963f66afa6",
    "make": "Honda",
    "model": "Civic",
    "year": 2024,
    "plateNumber": "GR-1234-24",
    "photos": [
      "https://ryverentalstorage.blob.core.windows.net/uploads/honda-civic-1.jpg"
    ]
  }
}
```

### Breakdown Explanation

Original calculation (without promo):
- Rental: GHS 600.00 (GHS 200/day × 3 days)
- Deposit: GHS 500.00
- Protection: GHS 45.00
- Platform Fee: GHS 60.00 (10% of rental)
- **Original Total: GHS 1,205.00**

With `SUMMER2026` (15% off):
- Discount: GHS 127.50 (15% of GHS 850 = rental + protection + platform fee)
- **Final Total: GHS 1,077.50**

---

## 3. Common Validation Errors

### Expired Code
```json
{
  "error": "Promo code error: This promo code has expired or is not yet valid"
}
```

### Already Used
```json
{
  "error": "Promo code error: You have already used this promo code 1 time(s)"
}
```

### Wrong User Type
```json
{
  "error": "Promo code error: This promo code is only for renters"
}
```

### First-Time Only
```json
{
  "error": "Promo code error: This promo code is only for first-time users"
}
```

### Minimum Not Met
```json
{
  "error": "Promo code error: Minimum booking amount of GHS 200.00 required"
}
```

### Category Restriction
```json
{
  "error": "Promo code error: This promo code only applies to SUV vehicles"
}
```

### Max Uses Reached
```json
{
  "error": "Promo code error: This promo code has reached its maximum usage limit"
}
```

---

# Common Response Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid input or validation error |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | User doesn't have required permissions |
| 404 | Not Found | Resource not found |
| 500 | Internal Server Error | Server error (contact support) |

---

# Frontend Implementation Examples

## React Hook for Promo Validation

```typescript
import { useState } from 'react';

interface PromoValidation {
  isValid: boolean;
  errorMessage: string | null;
  discountAmount: number;
  finalAmount: number;
}

export function usePromoCode() {
  const [validation, setValidation] = useState<PromoValidation | null>(null);
  const [loading, setLoading] = useState(false);

  const validateCode = async (code: string, bookingAmount: number) => {
    setLoading(true);
    try {
      const response = await fetch('/api/v1/promo-codes/validate', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          code,
          bookingAmount,
          categoryId: null,
          cityId: null,
          tripDuration: null
        })
      });

      const result = await response.json();
      setValidation(result);
      return result;
    } catch (error) {
      console.error('Promo validation failed:', error);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const clearPromo = () => setValidation(null);

  return { validation, loading, validateCode, clearPromo };
}
```

## Usage in Booking Form

```tsx
function BookingForm() {
  const [promoCode, setPromoCode] = useState('');
  const { validation, loading, validateCode, clearPromo } = usePromoCode();
  const [totalAmount, setTotalAmount] = useState(850);

  const handleApplyPromo = async () => {
    const result = await validateCode(promoCode, totalAmount);
    if (result?.isValid) {
      toast.success(`Saved GHS ${result.discountAmount.toFixed(2)}!`);
    } else {
      toast.error(result?.errorMessage || 'Invalid promo code');
    }
  };

  const displayTotal = validation?.isValid 
    ? validation.finalAmount 
    : totalAmount;

  return (
    <div>
      {/* Booking form fields */}
      
      <div className="promo-section">
        <input
          type="text"
          placeholder="Enter promo code"
          value={promoCode}
          onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
        />
        <button onClick={handleApplyPromo} disabled={loading}>
          {loading ? 'Validating...' : 'Apply'}
        </button>
        {validation?.isValid && (
          <button onClick={clearPromo}>Remove</button>
        )}
      </div>

      {validation?.isValid && (
        <div className="discount-applied">
          ✓ {validation.promoCode?.description}
          <br />
          Discount: GHS {validation.discountAmount.toFixed(2)}
        </div>
      )}

      {validation?.errorMessage && (
        <div className="error">
          {validation.errorMessage}
        </div>
      )}

      <div className="total">
        {validation?.isValid && (
          <div className="original-price">
            <s>GHS {totalAmount.toFixed(2)}</s>
          </div>
        )}
        <div className="final-price">
          Total: GHS {displayTotal.toFixed(2)}
        </div>
      </div>

      <button onClick={createBooking}>
        Confirm Booking
      </button>
    </div>
  );
}
```

---

# Testing with Sample Codes

Use these pre-created codes for testing:

| Code | Type | Discount | Target | Applies To | Notes |
|------|------|----------|--------|------------|-------|
| `NEWYEAR2026` | Percentage | 20% | Renter | Total | First-time only, expires Jan 31 |
| `WELCOME50` | Fixed | GHS 50 | Renter | Total | First-time only, valid all year |
| `FREEPROTECTION` | Free Addon | GHS 100 | Renter | Protection | 3 uses per user, expires Mar 31 |
| `OWNER50OFF` | Commission | 50% | Owner | Commission | 10 uses per owner |
| `LISTFREE` | Commission | 100% | Owner | Commission | First 3 bookings only |

---

# Need Help?

- Check response error messages for specific validation failures
- Use validation endpoint before creating bookings
- Monitor analytics to track promo performance
- Contact support for commission reduction setup

**API Base URL:** `https://ryverental.info`
