# Owner Contact & Location API Documentation

This document describes the updated API endpoints that now include owner contact and location information (BusinessPhone, BusinessAddress, GpsAddress, PickupInstructions, City, Region).

---

## 1. Get Owner Profile

**Endpoint:** `GET /api/v1/owner/profile`  
**Authentication:** Required (Bearer Token - Owner role)  
**Description:** Retrieves the authenticated owner's profile including contact and location details.

### Request Headers
```http
Authorization: Bearer <jwt_token>
```

### Response (200 OK)
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "owner@example.com",
  "phone": "+233501234567",
  "ownerProfile": {
    "ownerType": "individual",
    "displayName": "John's Car Rentals",
    "companyName": null,
    "businessRegistrationNumber": null,
    "payoutPreference": "bank_transfer",
    "businessPhone": "+233501234567",
    "businessAddress": "123 Spintex Road, Accra",
    "gpsAddress": "GA-123-4567",
    "pickupInstructions": "Park in the visitor's area. Call when you arrive and I'll come down.",
    "city": "Accra",
    "region": "Greater Accra",
    "payoutDetails": {
      "accountName": "John Doe",
      "accountNumber": "1234567890",
      "bankName": "GCB Bank",
      "branchCode": "030100"
    },
    "payoutVerificationStatus": "verified",
    "payoutDetailsPending": null
  }
}
```

### Response (200 OK) - Owner with no profile yet
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "newowner@example.com",
  "phone": "+233501234567",
  "ownerProfile": null
}
```

### Error Responses

**401 Unauthorized**
```json
{
  "error": "Unauthorized"
}
```

**404 Not Found**
```json
{
  "error": "User not found"
}
```

---

## 2. Update Owner Profile

**Endpoint:** `PUT /api/v1/owner/profile`  
**Authentication:** Required (Bearer Token - Owner role)  
**Description:** Updates the authenticated owner's profile with contact and location information.

### Request Headers
```http
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

### Request Body
```json
{
  "displayName": "John's Premium Car Rentals",
  "ownerType": "individual",
  "companyName": null,
  "businessRegistrationNumber": null,
  "payoutPreference": "bank_transfer",
  "businessPhone": "+233501234567",
  "businessAddress": "123 Spintex Road, Opposite Shell Station, Accra",
  "gpsAddress": "GA-123-4567",
  "pickupInstructions": "Park in the visitor's area behind the main building. Call me when you arrive and I'll bring the keys down. Look for a blue gate.",
  "city": "Accra",
  "region": "Greater Accra",
  "payoutDetails": {
    "accountName": "John Doe",
    "accountNumber": "1234567890",
    "bankName": "GCB Bank",
    "branchCode": "030100"
  }
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `displayName` | string | No | Display name for the owner's business |
| `ownerType` | string | No | Type of owner: `"individual"` or `"company"` |
| `companyName` | string | No | Company name (required if ownerType is "company") |
| `businessRegistrationNumber` | string | No | Business registration number |
| `payoutPreference` | string | No | Payout method: `"bank_transfer"`, `"mobile_money"`, etc. |
| `businessPhone` | string | No | **NEW** - Phone number for renters to contact owner |
| `businessAddress` | string | No | **NEW** - Physical address for vehicle pickup |
| `gpsAddress` | string | No | **NEW** - Ghana Digital Address (GPS address) |
| `pickupInstructions` | string | No | **NEW** - Special instructions for vehicle pickup |
| `city` | string | No | **NEW** - City/town of business location |
| `region` | string | No | **NEW** - Region of business location |
| `payoutDetails` | object | No | Bank or mobile money details (pending admin verification) |

### Response (200 OK)
```json
{
  "success": true
}
```

### Error Responses

**401 Unauthorized**
```json
{
  "error": "Unauthorized"
}
```

**404 Not Found**
```json
{
  "error": "User not found"
}
```

---

## 3. Get User by ID (Admin)

**Endpoint:** `GET /api/v1/users/me`  
**Authentication:** Required (Bearer Token - Any authenticated user)  
**Description:** Retrieves the authenticated user's details including owner profile.

### Request Headers
```http
Authorization: Bearer <jwt_token>
```

### Response (200 OK) - Owner User
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "owner@example.com",
  "phone": "+233501234567",
  "role": "owner",
  "status": "active",
  "phoneVerified": true,
  "ownerProfile": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ownerType": "individual",
    "displayName": "John's Car Rentals",
    "companyName": null,
    "businessRegistrationNumber": null,
    "payoutPreference": "bank_transfer",
    "payoutDetailsJson": "{\"accountName\":\"John Doe\",\"accountNumber\":\"1234567890\",\"bankName\":\"GCB Bank\",\"branchCode\":\"030100\"}",
    "payoutDetailsPendingJson": null,
    "payoutVerificationStatus": "verified",
    "stripeAccountId": null,
    "stripeOnboardingCompleted": false,
    "instantWithdrawalEnabled": false,
    "payoutFrequency": "weekly",
    "minimumPayoutAmount": 100.00,
    "businessPhone": "+233501234567",
    "businessAddress": "123 Spintex Road, Accra",
    "gpsAddress": "GA-123-4567",
    "pickupInstructions": "Park in the visitor's area. Call when you arrive.",
    "city": "Accra",
    "region": "Greater Accra"
  },
  "renterProfile": null
}
```

---

## 4. Get All Users (Admin)

**Endpoint:** `GET /api/v1/admin/users`  
**Authentication:** Required (Bearer Token - Admin role)  
**Description:** Retrieves all users with their profiles including owner contact information.

### Request Headers
```http
Authorization: Bearer <jwt_token>
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `role` | string | No | Filter by role: `"owner"`, `"renter"`, `"driver"`, `"admin"` |
| `status` | string | No | Filter by status: `"active"`, `"suspended"`, `"banned"` |
| `page` | integer | No | Page number (default: 1) |
| `pageSize` | integer | No | Items per page (default: 50) |

### Example Request
```http
GET /api/v1/admin/users?role=owner&status=active&page=1&pageSize=20
```

### Response (200 OK)
```json
{
  "total": 45,
  "page": 1,
  "pageSize": 20,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "owner1@example.com",
      "phone": "+233501234567",
      "firstName": "John",
      "lastName": "Doe",
      "role": "owner",
      "status": "active",
      "phoneVerified": true,
      "createdAt": "2025-01-01T10:30:00Z",
      "ownerProfile": {
        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "ownerType": "individual",
        "displayName": "John's Car Rentals",
        "companyName": null,
        "businessRegistrationNumber": null,
        "payoutPreference": "bank_transfer",
        "payoutDetailsJson": "{...}",
        "payoutDetailsPendingJson": null,
        "payoutVerificationStatus": "verified",
        "stripeAccountId": null,
        "stripeOnboardingCompleted": false,
        "instantWithdrawalEnabled": false,
        "payoutFrequency": "weekly",
        "minimumPayoutAmount": 100.00,
        "businessPhone": "+233501234567",
        "businessAddress": "123 Spintex Road, Accra",
        "gpsAddress": "GA-123-4567",
        "pickupInstructions": "Park in the visitor's area. Call when you arrive.",
        "city": "Accra",
        "region": "Greater Accra"
      },
      "renterProfile": null
    },
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "email": "owner2@example.com",
      "phone": "+233509876543",
      "firstName": "Jane",
      "lastName": "Smith",
      "role": "owner",
      "status": "active",
      "phoneVerified": true,
      "createdAt": "2025-01-02T14:20:00Z",
      "ownerProfile": {
        "userId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
        "ownerType": "company",
        "displayName": "ABC Car Hire Ltd",
        "companyName": "ABC Car Hire Limited",
        "businessRegistrationNumber": "CS123456789",
        "payoutPreference": "bank_transfer",
        "payoutDetailsJson": "{...}",
        "payoutDetailsPendingJson": null,
        "payoutVerificationStatus": "pending",
        "stripeAccountId": null,
        "stripeOnboardingCompleted": false,
        "instantWithdrawalEnabled": false,
        "payoutFrequency": "monthly",
        "minimumPayoutAmount": 500.00,
        "businessPhone": "+233509876543",
        "businessAddress": "45 Oxford Street, Osu, Accra",
        "gpsAddress": "GA-456-7890",
        "pickupInstructions": "Enter through the main gate. Our office is on the ground floor, second door on the left.",
        "city": "Accra",
        "region": "Greater Accra"
      },
      "renterProfile": null
    }
  ]
}
```

---

## Email Template Changes

The following email templates now include owner contact information:

### 1. Booking Confirmation (Customer)
**Template:** `booking_confirmation_customer`

**New Placeholders:**
- `{{owner_name}}` - Owner's full name
- `{{owner_phone}}` - Owner's business phone (fallback to user phone)
- `{{owner_address}}` - Owner's business address (fallback to pickup location)
- `{{owner_gps_address}}` - Ghana GPS address
- `{{pickup_instructions}}` - Special pickup instructions
- `{{inspection_link}}` - Vehicle inspection link

### 2. Booking Confirmed (After Payment)
**Template:** `booking_confirmed`

**New Placeholders:**
- `{{owner_name}}` - Owner's full name
- `{{owner_phone}}` - Owner's business phone
- `{{owner_address}}` - Owner's business address
- `{{owner_gps_address}}` - Ghana GPS address
- `{{pickup_instructions}}` - Special pickup instructions
- `{{inspection_link}}` - Vehicle inspection link

---

## Frontend Integration Notes

### 1. Owner Profile Management Page

**Display Fields:**
- Show all contact fields in the profile form
- Make GPS address field prominent (common in Ghana)
- Use textarea for pickup instructions (allow multi-line)
- Add validation for phone numbers (Ghana format: +233XXXXXXXXX)

**Example Form:**
```jsx
<form>
  <input name="displayName" placeholder="Business Display Name" />
  <select name="ownerType">
    <option value="individual">Individual</option>
    <option value="company">Company</option>
  </select>
  
  {/* Contact Information Section */}
  <h3>Contact Information</h3>
  <input 
    name="businessPhone" 
    placeholder="+233501234567" 
    pattern="^\+233[0-9]{9}$"
  />
  
  {/* Location Section */}
  <h3>Location Details</h3>
  <input name="city" placeholder="e.g., Accra" />
  <select name="region">
    <option value="Greater Accra">Greater Accra</option>
    <option value="Ashanti">Ashanti</option>
    {/* Other regions */}
  </select>
  <input name="businessAddress" placeholder="Physical address" />
  <input 
    name="gpsAddress" 
    placeholder="Ghana GPS Address (e.g., GA-123-4567)" 
    pattern="^[A-Z]{2}-[0-9]{3}-[0-9]{4}$"
  />
  
  {/* Pickup Instructions */}
  <h3>Pickup Instructions</h3>
  <textarea 
    name="pickupInstructions" 
    placeholder="Provide clear instructions for renters to find you and collect the vehicle"
    rows="4"
  />
</form>
```

### 2. Booking Confirmation Display

**Show Owner Contact in Booking Details:**
```jsx
<div className="owner-contact-section">
  <h3>Owner Contact Information</h3>
  <div className="contact-details">
    <p><strong>Owner:</strong> {booking.ownerName}</p>
    <p><strong>Phone:</strong> 
      <a href={`tel:${booking.ownerPhone}`}>{booking.ownerPhone}</a>
    </p>
    <p><strong>Pickup Address:</strong> {booking.ownerAddress}</p>
    {booking.ownerGpsAddress && (
      <p><strong>GPS Address:</strong> 
        <code>{booking.ownerGpsAddress}</code>
        <button onClick={() => openGPSInMaps(booking.ownerGpsAddress)}>
          Open in Maps
        </button>
      </p>
    )}
  </div>
  
  {booking.pickupInstructions && (
    <div className="pickup-instructions">
      <h4>Pickup Instructions</h4>
      <p>{booking.pickupInstructions}</p>
    </div>
  )}
  
  {booking.inspectionLink && (
    <a href={booking.inspectionLink} className="btn btn-primary">
      Start Vehicle Inspection
    </a>
  )}
</div>
```

### 3. Admin Dashboard - Owner Management

**Display Owner Contact in User List:**
```jsx
<table>
  <thead>
    <tr>
      <th>Owner Name</th>
      <th>Email</th>
      <th>Phone</th>
      <th>Location</th>
      <th>GPS Address</th>
      <th>Status</th>
    </tr>
  </thead>
  <tbody>
    {owners.map(owner => (
      <tr key={owner.id}>
        <td>{owner.firstName} {owner.lastName}</td>
        <td>{owner.email}</td>
        <td>{owner.ownerProfile?.businessPhone || owner.phone}</td>
        <td>
          {owner.ownerProfile?.city}, {owner.ownerProfile?.region}
        </td>
        <td>
          <code>{owner.ownerProfile?.gpsAddress || 'N/A'}</code>
        </td>
        <td>{owner.status}</td>
      </tr>
    ))}
  </tbody>
</table>
```

---

## Validation Rules

### Business Phone
- **Format:** `+233XXXXXXXXX` (Ghana country code)
- **Length:** 13 characters
- **Pattern:** `^\+233[0-9]{9}$`
- **Example:** `+233501234567`

### GPS Address
- **Format:** `XX-XXX-XXXX` (Ghana Digital Address format)
- **Pattern:** `^[A-Z]{2}-[0-9]{3}-[0-9]{4}$`
- **Example:** `GA-123-4567`
- **Note:** First 2 letters indicate region (GA = Greater Accra, AK = Ashanti, etc.)

### Pickup Instructions
- **Max Length:** 500 characters
- **Format:** Plain text, multi-line allowed

### Business Address
- **Max Length:** 256 characters
- **Required:** No, but highly recommended

---

## Migration Notes

### Existing Owners
- All new fields are **optional**
- Existing owners will have `null` values for new fields
- Encourage owners to update their profiles via dashboard
- Show profile completion percentage to incentivize updates

### Email Fallbacks
- If `businessPhone` is null, system falls back to user's `phone`
- If `businessAddress` is null, system uses pickup location from booking
- If `gpsAddress` is null, field is hidden in emails
- If `pickupInstructions` is null, shows default message: "Contact owner for pickup details"

---

## Testing Recommendations

1. **Test Profile Update:**
   - Update owner profile with all new fields
   - Verify fields are saved correctly
   - Check API response includes new fields

2. **Test Booking Flow:**
   - Create a booking with an owner who has complete contact info
   - Verify booking confirmation email includes owner contact section
   - Test inspection link functionality

3. **Test Fallbacks:**
   - Create booking with owner who has no contact info
   - Verify fallback values are used
   - Ensure no null/undefined errors

4. **Test Admin View:**
   - List all owners in admin dashboard
   - Verify contact fields are displayed
   - Test filtering and sorting

---

## Support

For questions or issues, contact the backend team or refer to:
- Main API Documentation: `README.md`
- Promo Code Integration: `PROMO_CODE_FRONTEND_GUIDE.md`
- Renter Endpoints: `RENTER_ENDPOINT_GUIDE.md`
