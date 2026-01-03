# User Management & Rental Agreement Guide

## User Status Management

### Understanding User Status
Users can have three statuses:
- **pending** - Newly registered, awaiting approval (owners/admins) or verification in progress
- **active** - Verified and allowed to operate fully
- **suspended** - Blocked from all operations

### Viewing All Users (Including Suspended/Deactivated)

**Get all users (no filtering):**
```http
GET /api/v1/admin/users
```
This returns ALL users regardless of status (including suspended and deactivated).

**Filter by status:**
```http
GET /api/v1/admin/users?status=suspended
GET /api/v1/admin/users?status=active
GET /api/v1/admin/users?status=pending
```

**Filter by role:**
```http
GET /api/v1/admin/users?role=owner
GET /api/v1/admin/users?role=renter
GET /api/v1/admin/users?role=admin
```

**Combine filters:**
```http
GET /api/v1/admin/users?role=owner&status=suspended
```

### Reactivating/Unsuspending Users

**To reactivate a suspended user:**
```http
PUT /api/v1/admin/users/{userId}/status
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "status": "active"
}
```

**To suspend a user:**
```http
PUT /api/v1/admin/users/{userId}/status
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "status": "suspended"
}
```

**To set user back to pending:**
```http
PUT /api/v1/admin/users/{userId}/status
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "status": "pending"
}
```

### Valid Status Values
- `active` - User can login and operate
- `pending` - User awaiting verification (owners/admins cannot login)
- `suspended` - User blocked from login and all operations

---

## Rental Agreement Access

### For Renters

**View all rental agreements you've signed:**
```http
GET /api/v1/renter/rental-agreements
Authorization: Bearer {renter-token}
```

Response:
```json
{
  "total": 5,
  "page": 1,
  "pageSize": 50,
  "data": [
    {
      "bookingId": "...",
      "bookingReference": "RV-2025-...",
      "templateCode": "default",
      "templateVersion": "1.0.0",
      "acceptedAt": "2025-12-20T10:30:00Z",
      "acceptedNoSmoking": true,
      "acceptedFinesAndTickets": true,
      "acceptedAccidentProcedure": true
    }
  ]
}
```

**View specific rental agreement for a booking:**
```http
GET /api/v1/renter/bookings/{bookingId}/rental-agreement
Authorization: Bearer {renter-token}
```

Response includes:
- Full agreement text (`agreementSnapshot`)
- All acceptance checkboxes
- When it was signed (`acceptedAt`)
- IP address of signer

### For Owners

**View rental agreement for a booking on your vehicle:**
```http
GET /api/v1/owner/bookings/{bookingId}/rental-agreement
Authorization: Bearer {owner-token}
```

Response:
```json
{
  "bookingId": "...",
  "bookingReference": "RV-2025-...",
  "renterId": "...",
  "renterName": "John Doe",
  "renterEmail": "john@example.com",
  "templateCode": "default",
  "templateVersion": "1.0.0",
  "agreementSnapshot": "RYVE RENTAL AGREEMENT\n\n...",
  "acceptedNoSmoking": true,
  "acceptedFinesAndTickets": true,
  "acceptedAccidentProcedure": true,
  "acceptedAt": "2025-12-20T10:30:00Z",
  "ipAddress": "192.168.1.1"
}
```

### Existing Public Endpoints (No Auth Required)

**View agreement for any booking (guests/renters):**
```http
GET /api/v1/bookings/{bookingId}/rental-agreement
```

**Sign agreement:**
```http
POST /api/v1/bookings/{bookingId}/rental-agreement/accept
Content-Type: application/json

{
  "acceptedNoSmoking": true,
  "acceptedFinesAndTickets": true,
  "acceptedAccidentProcedure": true,
  "customerEmail": "renter@example.com",  // For guests only
  "customerName": "John Doe"              // For guests only
}
```

**Check acceptance (owner/admin/renter):**
```http
GET /api/v1/bookings/{bookingId}/rental-agreement/acceptance
Authorization: Bearer {token}
```

---

## Common Workflows

### Workflow 1: Unsuspend a User
1. Find the user:
   ```http
   GET /api/v1/admin/users?status=suspended&email=user@example.com
   ```
2. Reactivate them:
   ```http
   PUT /api/v1/admin/users/{userId}/status
   { "status": "active" }
   ```
3. Verify they can login now

### Workflow 2: Owner Views Signed Agreements
1. Get owner's bookings:
   ```http
   GET /api/v1/owner/bookings
   ```
2. For each booking, view signed agreement:
   ```http
   GET /api/v1/owner/bookings/{bookingId}/rental-agreement
   ```

### Workflow 3: Renter Reviews Their Agreements
1. List all signed agreements:
   ```http
   GET /api/v1/renter/rental-agreements
   ```
2. View full details of specific agreement:
   ```http
   GET /api/v1/renter/bookings/{bookingId}/rental-agreement
   ```

---

## Key Points

✅ **User Status Changes**
- Admin can change any user's status at any time
- Status changes are immediate
- Suspended users cannot login
- Pending owners/admins cannot login (but pending renters can)

✅ **Rental Agreements**
- Renters can always view agreements they've signed
- Owners can view agreements for bookings on their vehicles
- Agreements include full snapshot of terms at time of signing
- IP address and timestamp are recorded for legal purposes

✅ **Access Control**
- Renters: Can only view their own agreements
- Owners: Can only view agreements for their vehicles' bookings
- Admin: Can view any agreement via the existing endpoint
