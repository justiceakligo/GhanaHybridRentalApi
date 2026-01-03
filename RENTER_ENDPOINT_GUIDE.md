# Renter Endpoint Guide for Frontend

## Get All Renters

### Endpoint
```
GET /api/v1/admin/renters
```

### Authentication
Requires admin role. Include JWT token in Authorization header.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `search` | string | No | - | Search by name, email, or phone |
| `status` | string | No | - | Filter by verification status (`unverified`, `basic_verified`, `verified`) |
| `page` | integer | No | 1 | Page number for pagination |
| `pageSize` | integer | No | 50 | Number of results per page |

---

## Sample Requests

### 1. Get All Renters (Default)
```javascript
const response = await fetch('https://ryverental.info/api/v1/admin/renters', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${adminToken}`,
    'Content-Type': 'application/json'
  }
});

const data = await response.json();
```

### 2. Search Renters by Name or Email
```javascript
const searchTerm = 'alex';
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters?search=${encodeURIComponent(searchTerm)}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
```

### 3. Filter by Verification Status
```javascript
const status = 'verified';
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters?status=${status}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
```

### 4. Paginated Request
```javascript
const page = 2;
const pageSize = 20;
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters?page=${page}&pageSize=${pageSize}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
```

### 5. Combined Filters
```javascript
const params = new URLSearchParams({
  search: 'john',
  status: 'verified',
  page: '1',
  pageSize: '25'
});

const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters?${params.toString()}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
```

---

## Sample Response (Success - 200 OK)

```json
{
  "total": 45,
  "page": 1,
  "pageSize": 50,
  "data": [
    {
      "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "renterId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "email": "john.doe@example.com",
      "phone": "233240123456",
      "firstName": "John",
      "lastName": "Doe",
      "fullName": "John Doe",
      "nationality": "Ghanaian",
      "dob": "1990-05-15T00:00:00Z",
      "userStatus": "active",
      "verificationStatus": "driver_verified",
      "driverLicenseNumber": "DL123456789",
      "driverLicenseExpiryDate": "2027-05-15T00:00:00Z",
      "driverLicensePhotoUrl": "https://storage.blob.core.windows.net/uploads/driver-license-123.jpg",
      "nationalIdNumber": "GHA-123456789-0",
      "nationalIdPhotoUrl": "https://storage.blob.core.windows.net/uploads/national-id-123.jpg",
      "passportNumber": null,
      "passportExpiryDate": null,
      "passportPhotoUrl": null,
      "createdAt": "2025-12-15T10:30:00Z"
    },
    {
      "userId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "renterId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "email": "jane.smith@example.com",
      "phone": "233241234567",
      "firstName": "Jane",
      "lastName": "Smith",
      "fullName": "Jane Smith",
      "nationality": "Nigerian",
      "dob": "1992-08-20T00:00:00Z",
      "userStatus": "active",
      "verificationStatus": "basic_verified",
      "driverLicenseNumber": null,
      "driverLicenseExpiryDate": null,
      "driverLicensePhotoUrl": null,
      "nationalIdNumber": null,
      "nationalIdPhotoUrl": null,
      "passportNumber": "P12345678",
      "passportExpiryDate": "2028-08-20T00:00:00Z",
      "passportPhotoUrl": "https://storage.blob.core.windows.net/uploads/passport-456.jpg",
      "createdAt": "2025-12-20T14:20:00Z"
    },
    {
      "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "renterId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "email": "alex.johnson@example.com",
      "phone": "233242345678",
      "firstName": "Alex",
      "lastName": "Johnson",
      "fullName": "Alex Johnson",
      "nationality": null,
      "dob": null,
      "userStatus": "active",
      "verificationStatus": "unverified",
      "driverLicenseNumber": null,
      "driverLicenseExpiryDate": null,
      "driverLicensePhotoUrl": null,
      "nationalIdNumber": null,
      "nationalIdPhotoUrl": null,
      "passportNumber": null,
      "passportExpiryDate": null,
      "passportPhotoUrl": null,
      "createdAt": "2025-12-25T08:15:00Z"
    }
  ]
}
```

---

## Sample Response (Empty Results - 200 OK)

```json
{
  "total": 0,
  "page": 1,
  "pageSize": 50,
  "data": []
}
```

---

## Sample Response (Error - 500)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Error: Database connection failed | Inner: Unable to connect to database"
}
```

---

## React/TypeScript Example

```typescript
interface Renter {
  userId: string;
  renterId: string;
  email: string | null;
  phone: string | null;
  firstName: string | null;
  lastName: string | null;
  fullName: string | null;
  nationality: string | null;
  dob: string | null;
  userStatus: string | null;
  verificationStatus: string;
  // Driver's License
  driverLicenseNumber: string | null;
  driverLicenseExpiryDate: string | null;
  driverLicensePhotoUrl: string | null;
  // National ID
  nationalIdNumber: string | null;
  nationalIdPhotoUrl: string | null;
  // Passport
  passportNumber: string | null;
  passportExpiryDate: string | null;
  passportPhotoUrl: string | null;
  createdAt: string;
}

interface RentersResponse {
  total: number;
  page: number;
  pageSize: number;
  data: Renter[];
}

async function fetchRenters(
  search?: string,
  status?: string,
  page: number = 1,
  pageSize: number = 50
): Promise<RentersResponse> {
  const params = new URLSearchParams();
  
  if (search) params.append('search', search);
  if (status) params.append('status', status);
  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());

  const token = localStorage.getItem('adminToken');
  
  const response = await fetch(
    `https://ryverental.info/api/v1/admin/renters?${params.toString()}`,
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to fetch renters: ${response.statusText}`);
  }

  return await response.json();
}

// Usage in component
const loadRenters = async () => {
  try {
    const result = await fetchRenters('alex', 'verified', 1, 20);
    console.log(`Found ${result.total} renters`);
    setRenters(result.data);
  } catch (error) {
    console.error('Error loading renters:', error);
  }
};
```

---

## Verification Status Values

| Value | Description |
|-------|-------------|
| `unverified` | Renter has not uploaded documents or not verified |
| `basic_verified` | Renter has basic verification (National ID or Passport uploaded) |
| `driver_verified` | Renter is fully verified with driver's license and ID approved |

---

## User Status Values

| Value | Description |
|-------|-------------|
| `active` | User account is active and can use the platform |
| `suspended` | User account is temporarily suspended |
| `deleted` | User account has been deleted |

---

## Renter Management Endpoints

### Verify Renter's Driver's License

**Endpoint:** `POST /api/v1/admin/renters/{renterId}/verify-license`

**Description:** Approves a renter's driver's license and upgrades their verification status to `driver_verified`.

**Sample Request:**
```javascript
const renterId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters/${renterId}/verify-license`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({})
  }
);

const data = await response.json();
```

**Sample Response (200 OK):**
```json
{
  "success": true,
  "renterId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "verificationStatus": "driver_verified",
  "message": "Driver's license verified successfully"
}
```

---

### Reject Renter's Driver's License

**Endpoint:** `POST /api/v1/admin/renters/{renterId}/reject-license`

**Description:** Rejects a renter's driver's license. Downgrades verification to `basic_verified` if they have National ID/Passport, or `unverified` if they don't.

**Request Body:**
```json
{
  "reason": "License image is blurry and unreadable"
}
```

**Sample Request:**
```javascript
const renterId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters/${renterId}/reject-license`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      reason: 'License image is blurry and unreadable'
    })
  }
);

const data = await response.json();
```

**Sample Response (200 OK):**
```json
{
  "success": true,
  "renterId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "verificationStatus": "basic_verified",
  "message": "Driver's license rejected",
  "reason": "License image is blurry and unreadable"
}
```

---

### Suspend Renter Account

**Endpoint:** `POST /api/v1/admin/renters/{renterId}/suspend`

**Description:** Suspends a renter's account, preventing them from making new bookings.

**Request Body:**
```json
{
  "reason": "Multiple violations of rental terms"
}
```

**Sample Request:**
```javascript
const renterId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
const response = await fetch(
  `https://ryverental.info/api/v1/admin/renters/${renterId}/suspend`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      reason: 'Multiple violations of rental terms'
    })
  }
);

const data = await response.json();
```

**Sample Response (200 OK):**
```json
{
  "success": true,
  "renterId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "suspended",
  "message": "Renter account suspended successfully",
  "reason": "Multiple violations of rental terms"
}
```

---

## Common Issues & Solutions

### Issue: Empty results when renters exist
**Solution:** Users must have entries in the `RenterProfiles` table. If users signed up but never completed renter registration, they won't appear.

### Issue: 401 Unauthorized
**Solution:** Ensure the JWT token is valid and has admin role. Check token expiration.

### Issue: 404 Not Found
**Solution:** Verify the API base URL is correct: `https://ryverental.info` (not the direct IP unless testing)

### Issue: CORS errors
**Solution:** The API allows requests from `https://dashboard.ryverental.com`. Ensure your frontend is hosted on this domain or contact backend admin to add your domain.

---

## Testing with cURL

```bash
# Get all renters
curl -X GET "https://ryverental.info/api/v1/admin/renters" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json"

# Search renters
curl -X GET "https://ryverental.info/api/v1/admin/renters?search=alex&status=verified" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json"
```

---

## Notes

- All timestamps are in UTC (ISO 8601 format)
- The `renterId` and `userId` are the same value (renter's user ID)
- Null values indicate data not provided by the user
- Pagination starts at page 1 (not 0)
- Maximum pageSize is enforced by the backend (default: 50)
