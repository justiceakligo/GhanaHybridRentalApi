# Guest Booking Flow Improvements - v1.222

## Overview
Enhanced guest booking experience allowing unlimited guest bookings without registration, with self-service contact updates and gentle account suggestions for returning guests.

---

## 🎯 Key Features

### 1. **Unlimited Guest Bookings**
- No blocking based on previous email/phone usage
- Each guest booking is independent
- No forced registration

### 2. **Gentle Account Suggestions**
- Shows friendly message for returning guests
- Never blocks the booking process
- Encourages account creation without forcing it

### 3. **Self-Service Contact Updates**
- Guests can update their info using booking reference
- Updates allowed before pickup or during active rental
- Email confirmation sent to new address

### 4. **Staff Contact Management**
- Owners/Admins can update guest info at pickup
- Useful for corrections or last-minute changes

---

## 👤 Renter / Guest Capabilities

### Create Guest Booking

**Endpoint:** `POST /api/v1/bookings`

**Request Body:**
```json
{
  "vehicleId": "123e4567-e89b-12d3-a456-426614174000",
  "pickupDateTime": "2026-01-10T10:00:00Z",
  "returnDateTime": "2026-01-12T10:00:00Z",
  "pickupLocation": {
    "address": "123 Main St, Accra",
    "latitude": 5.6037,
    "longitude": -0.1870
  },
  "returnLocation": {
    "address": "123 Main St, Accra",
    "latitude": 5.6037,
    "longitude": -0.1870
  },
  "guestPhone": "+233 242841000",
  "guestEmail": "guest@example.com",
  "guestFirstName": "John",
  "guestLastName": "Doe",
  "guestDriverLicenseNumber": "DL123456",
  "guestDriverLicenseExpiryDate": "2027-12-31",
  "paymentMethod": "card"
}
```

**Response (First-Time Guest):**
```json
{
  "booking": {
    "id": "789e0123-e89b-12d3-a456-426614174000",
    "bookingReference": "RV-2026-ABC123",
    "renterId": "456e7890-e89b-12d3-a456-426614174000",
    "vehicleId": "123e4567-e89b-12d3-a456-426614174000",
    "status": "pending_payment",
    "paymentStatus": "unpaid",
    "totalAmount": 450.00,
    "currency": "GHS",
    "pickupDateTime": "2026-01-10T10:00:00Z",
    "returnDateTime": "2026-01-12T10:00:00Z",
    "guestEmail": "guest@example.com",
    "guestPhone": "+233242841000",
    "guestFirstName": "John",
    "guestLastName": "Doe",
    "createdAt": "2026-01-05T10:00:00Z"
  },
  "manageBookingUrl": "https://ryverental.info/bookings/RV-2026-ABC123"
}
```

**Response (Returning Guest):**
```json
{
  "booking": {
    "id": "789e0123-e89b-12d3-a456-426614174000",
    "bookingReference": "RV-2026-ABC123",
    // ... same as above
  },
  "accountSuggestion": "💡 We noticed you've booked with us before! Create an account to track all your bookings and enjoy faster checkout.",
  "manageBookingUrl": "https://ryverental.info/bookings/RV-2026-ABC123"
}
```

---

### Update Guest Contact Information (Self-Service)

**Endpoint:** `PUT /api/v1/bookings/guest/{bookingReference}/contact`

**Authentication:** ❌ None required (booking reference is the key)

**Allowed Statuses:** `pending_payment`, `confirmed`, `active`

**Request Body:**
```json
{
  "newEmail": "newemail@example.com",
  "newPhone": "+233 201234567",
  "newFirstName": "Jane",
  "newLastName": "Smith"
}
```

**Note:** All fields are optional. Only send the fields you want to update.

**Success Response:**
```json
{
  "message": "Contact information updated successfully",
  "booking": {
    "bookingReference": "RV-2026-ABC123",
    "email": "newemail@example.com",
    "phone": "+233201234567",
    "firstName": "Jane",
    "lastName": "Smith"
  }
}
```

**Error Response (Invalid Status):**
```json
{
  "error": "Contact information can only be updated before pickup or during active rental"
}
```

**Error Response (Booking Not Found):**
```json
{
  "error": "Booking not found"
}
```

---

### Email Confirmation After Contact Update

**Sent to:** New email address only

**Email Subject:** "Booking Contact Information Updated"

**Email Content:**
```
Booking Contact Information Updated

Your contact information for booking RV-2026-ABC123 has been successfully updated.

Updated Details:
• Email: newemail@example.com
• Phone: +233201234567
• Name: Jane Smith

If you did not make this change, please contact us immediately.
```

---

## 👔 Owner Capabilities

### Update Guest Contact (At Pickup/Checkout)

**Endpoint:** `PUT /api/v1/bookings/{bookingId}/guest-contact`

**Authentication:** ✅ Required (Owner role)

**Authorization:** Owner can only update their own vehicle bookings

**Request Body:**
```json
{
  "newEmail": "corrected@example.com",
  "newPhone": "+233 209876543",
  "newFirstName": "John",
  "newLastName": "Corrected"
}
```

**Success Response:**
```json
{
  "message": "Guest contact updated successfully",
  "booking": {
    "bookingReference": "RV-2026-ABC123",
    "email": "corrected@example.com",
    "phone": "+233209876543",
    "firstName": "John",
    "lastName": "Corrected"
  }
}
```

**Error Response (Not Authorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

---

### Search Bookings (Existing Endpoint)

**Endpoint:** `GET /api/v1/bookings/lookup`

**Authentication:** ✅ Required (Owner role)

**Query Parameters:**
- `bookingReference` - Search by booking reference
- `phone` - Search by guest phone
- `firstName` - Search by guest first name
- `lastName` - Search by guest last name
- `qrToken` - Search by QR magic link token

**Example Request:**
```http
GET /api/v1/bookings/lookup?bookingReference=RV-2026-ABC123
Authorization: Bearer <owner-token>
```

**Response:**
```json
{
  "total": 1,
  "data": [
    {
      "id": "789e0123-e89b-12d3-a456-426614174000",
      "bookingReference": "RV-2026-ABC123",
      "renter": {
        "id": "456e7890-e89b-12d3-a456-426614174000",
        "phone": "+233242841000",
        "email": "guest@example.com",
        "firstName": "John",
        "lastName": "Doe"
      },
      "vehicle": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "make": "Toyota",
        "model": "Corolla",
        "year": 2023
      },
      "status": "confirmed",
      "paymentStatus": "paid",
      "totalAmount": 450.00,
      "pickupDateTime": "2026-01-10T10:00:00Z",
      "returnDateTime": "2026-01-12T10:00:00Z"
    }
  ]
}
```

---

## 👨‍💼 Admin Capabilities

### Update Any Guest Contact

**Endpoint:** `PUT /api/v1/bookings/{bookingId}/guest-contact`

**Authentication:** ✅ Required (Admin role)

**Authorization:** Admin can update any booking

**Request Body:**
```json
{
  "newEmail": "admin-corrected@example.com",
  "newPhone": "+233 204567890",
  "newFirstName": "Corrected",
  "newLastName": "ByAdmin"
}
```

**Success Response:**
```json
{
  "message": "Guest contact updated successfully",
  "booking": {
    "bookingReference": "RV-2026-ABC123",
    "email": "admin-corrected@example.com",
    "phone": "+233204567890",
    "firstName": "Corrected",
    "lastName": "ByAdmin"
  }
}
```

---

### Search All Bookings (Existing Endpoint)

**Endpoint:** `GET /api/v1/bookings/lookup`

**Authentication:** ✅ Required (Admin role)

**Authorization:** Admin can search ALL bookings (not limited to own vehicles)

**Query Parameters:**
- `bookingReference` - Search by booking reference
- `phone` - Search by guest phone (normalized)
- `firstName` - Search by guest first name
- `lastName` - Search by guest last name
- `qrToken` - Search by QR magic link token

**Example Request:**
```http
GET /api/v1/bookings/lookup?phone=0242841000
Authorization: Bearer <admin-token>
```

**Response:**
```json
{
  "total": 3,
  "data": [
    {
      "id": "789e0123-e89b-12d3-a456-426614174000",
      "bookingReference": "RV-2026-ABC123",
      "guestPhone": "+233242841000",
      "guestEmail": "guest@example.com",
      "guestFirstName": "John",
      "guestLastName": "Doe",
      "status": "confirmed",
      "totalAmount": 450.00
    },
    {
      "id": "890e1234-e89b-12d3-a456-426614174000",
      "bookingReference": "RV-2025-XYZ789",
      "guestPhone": "+233242841000",
      "guestEmail": "oldguest@example.com",
      "guestFirstName": "John",
      "guestLastName": "Doe",
      "status": "completed",
      "totalAmount": 320.00
    },
    // ... more bookings
  ]
}
```

---

## 🔒 Security & Validation

### Guest Self-Service Updates
- **No authentication required** - Booking reference acts as authorization
- **Status validation** - Only `pending_payment`, `confirmed`, or `active` bookings
- **Email confirmation** - Sent to new email address to verify ownership
- **Audit trail** - UpdatedAt timestamp tracks changes

### Staff/Admin Updates
- **Authentication required** - Valid JWT token
- **Role-based access**:
  - Owner: Can only update bookings for their own vehicles
  - Admin: Can update any booking
- **No email sent** - Assumed to be done in person at pickup

### Phone Number Normalization
All phone numbers are automatically normalized to E.164 format:
- Input: `0242841000` → Stored: `+233242841000`
- Input: `+233 242 841 000` → Stored: `+233242841000`
- Input: `242841000` → Stored: `+233242841000`

---

## 🎨 Frontend Integration

### Booking Confirmation Page

After booking creation, display:

```jsx
// Check if account suggestion exists
if (response.accountSuggestion) {
  showNotification({
    type: 'info',
    message: response.accountSuggestion,
    action: {
      label: 'Create Account',
      onClick: () => navigate('/register')
    }
  });
}

// Show manage booking link
showManageBookingButton({
  url: response.manageBookingUrl,
  label: 'Manage Your Booking'
});
```

### Guest Booking Management Page

URL: `https://ryverental.info/bookings/{bookingReference}`

**Features:**
- View booking details
- Update contact information
- Cancel booking (if allowed)
- View payment status
- Contact support

**Sample Contact Update Form:**
```jsx
const updateContact = async (bookingReference, data) => {
  const response = await fetch(
    `/api/v1/bookings/guest/${bookingReference}/contact`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        newEmail: data.email,
        newPhone: data.phone,
        newFirstName: data.firstName,
        newLastName: data.lastName
      })
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error);
  }
  
  return await response.json();
};
```

### Owner Pickup/Checkout Flow

At vehicle handover, owner can update guest details:

```jsx
const updateGuestContact = async (bookingId, data, ownerToken) => {
  const response = await fetch(
    `/api/v1/bookings/${bookingId}/guest-contact`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${ownerToken}`
      },
      body: JSON.stringify(data)
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to update contact');
  }
  
  return await response.json();
};
```

---

## 📊 Use Cases

### Use Case 1: Returning Guest Books Again
**Scenario:** Guest booked 3 months ago, wants to book again

**Flow:**
1. Guest enters same email/phone on booking form
2. Backend detects previous booking
3. Booking is created successfully (no blocking)
4. Response includes `accountSuggestion` field
5. Frontend shows gentle message encouraging account creation
6. Guest can choose to create account or continue as guest

**Benefit:** Guest is never blocked or frustrated

---

### Use Case 2: Typo in Email Address
**Scenario:** Guest made a typo in their email during booking

**Flow:**
1. Guest realizes email is wrong
2. Guest visits `https://ryverental.info/bookings/RV-2026-ABC123`
3. Guest clicks "Update Contact Info"
4. Guest enters correct email
5. Confirmation email sent to NEW email address
6. Guest verifies they received it

**Benefit:** Self-service fix without calling support

---

### Use Case 3: Phone Number Changed
**Scenario:** Guest's phone number changed since booking

**Flow:**
1. Owner notices phone number doesn't work at pickup
2. Owner asks for current phone number
3. Owner updates guest contact via dashboard
4. Booking now has correct phone for emergencies

**Benefit:** Owner can fix issues on the spot

---

### Use Case 4: Admin Data Correction
**Scenario:** Customer service received complaint about wrong contact info

**Flow:**
1. Admin searches booking by reference
2. Admin updates guest contact information
3. System updates booking record
4. Admin confirms fix with customer

**Benefit:** Admin can resolve issues quickly

---

## 🚀 Migration Notes

### Backward Compatibility
- ✅ All existing bookings work as before
- ✅ Existing API endpoints unchanged
- ✅ Only new optional fields in responses
- ✅ No database schema changes required

### Database Fields Used
All fields already exist in `Bookings` table:
- `GuestEmail` - Guest's email address
- `GuestPhone` - Guest's phone (normalized)
- `GuestFirstName` - Guest's first name
- `GuestLastName` - Guest's last name
- `BookingReference` - Unique booking reference
- `UpdatedAt` - Tracks when booking was last modified

---

## ✅ Testing Checklist

### Guest Booking Creation
- [ ] First-time guest can book successfully
- [ ] Returning guest can book without blocking
- [ ] `accountSuggestion` appears for returning guests
- [ ] `manageBookingUrl` is included in response
- [ ] Booking reference is unique and valid

### Guest Contact Updates
- [ ] Guest can update email with valid reference
- [ ] Guest can update phone with valid reference
- [ ] Guest can update name with valid reference
- [ ] Confirmation email sent to new address
- [ ] Update rejected for completed bookings
- [ ] Update rejected for cancelled bookings
- [ ] Update rejected with invalid reference

### Staff Contact Updates
- [ ] Owner can update their bookings
- [ ] Owner cannot update other owner's bookings
- [ ] Admin can update any booking
- [ ] Renter role cannot update guest contacts
- [ ] Phone numbers are normalized correctly

### Search & Lookup
- [ ] Owner can search by booking reference
- [ ] Owner can search by phone (normalized)
- [ ] Owner can search by name
- [ ] Owner only sees their bookings
- [ ] Admin sees all bookings

---

## 📝 API Endpoint Summary

| Endpoint | Method | Auth | Role | Purpose |
|----------|--------|------|------|---------|
| `/api/v1/bookings` | POST | Optional | Any | Create guest booking |
| `/api/v1/bookings/guest/{ref}/contact` | PUT | None | Guest | Update guest contact (self-service) |
| `/api/v1/bookings/{id}/guest-contact` | PUT | Required | Owner/Admin | Update guest contact (staff) |
| `/api/v1/bookings/lookup` | GET | Required | Owner/Admin | Search bookings |

---

## 🎯 Benefits

### For Guests
- ✅ No forced registration
- ✅ Book as many times as needed
- ✅ Fix contact info mistakes easily
- ✅ Gentle encouragement to create account
- ✅ Manage booking via simple URL

### For Owners
- ✅ Fix guest info at pickup
- ✅ No blocked bookings = more revenue
- ✅ Better customer experience
- ✅ Quick search by phone/name

### For Platform
- ✅ Higher conversion rate (no registration barrier)
- ✅ Reduced support load (self-service updates)
- ✅ Better data quality (easy corrections)
- ✅ Gradual account adoption (via gentle suggestions)

---

## 📞 Support

For questions or issues:
- Email: support@ryverental.info
- Phone: +233 242 841 000
- Web: https://ryverental.info/support
