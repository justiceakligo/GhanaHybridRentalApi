# Frontend API Updates - December 27, 2025

This document outlines all API changes and new endpoints for the Admin and Owner dashboards.

---

## Table of Contents
1. [Document Verification System](#1-document-verification-system)
2. [Renters Management](#2-renters-management)
3. [Scheduled Payouts Visibility](#3-scheduled-payouts-visibility)
4. [User Status Management Updates](#4-user-status-management-updates)

---

## 1. Document Verification System

### Overview
Complete overhaul of the document management system to include owner details and enable admin verification of renter and driver documents.

### 1.1 Get All Documents (Enhanced)

**Endpoint:** `GET /api/v1/admin/documents`

**Authorization:** Admin only

**Description:** Retrieves all documents across the system (vehicle, renter, driver) with owner/user details.

**Response Structure:**
```json
{
  "total": 45,
  "vehicleDocs": [
    {
      "id": "vehicle-{vehicleId}-insurance",
      "documentId": "uuid-here",
      "type": "vehicle",
      "subType": "insurance",
      "title": "Vehicle Insurance",
      "url": "https://storage.url/path",
      "status": "pending",
      "vehicleId": "uuid",
      "vehiclePlateNumber": "GE-1234-20",
      "vehicleMake": "Toyota",
      "vehicleModel": "Corolla",
      "ownerId": "uuid",
      "ownerName": "John Doe",
      "ownerEmail": "john@example.com",
      "ownerPhone": "+233501234567",
      "uploadedAt": "2025-12-20T10:30:00Z",
      "expiryDate": "2026-12-20T00:00:00Z"
    },
    {
      "id": "vehicle-{vehicleId}-roadworthy",
      "documentId": "uuid-here",
      "type": "vehicle",
      "subType": "roadworthy",
      "title": "Roadworthiness Certificate",
      "url": "https://storage.url/path",
      "status": "verified",
      "vehicleId": "uuid",
      "vehiclePlateNumber": "GE-5678-21",
      "vehicleMake": "Honda",
      "vehicleModel": "Civic",
      "ownerId": "uuid",
      "ownerName": "Jane Smith",
      "ownerEmail": "jane@example.com",
      "ownerPhone": "+233509876543",
      "uploadedAt": "2025-12-15T08:00:00Z",
      "expiryDate": "2026-06-15T00:00:00Z"
    }
  ],
  "renterDocs": [
    {
      "id": "renter-{userId}-nationalId",
      "documentId": "uuid-here",
      "type": "renter",
      "subType": "nationalId",
      "title": "National ID",
      "url": "https://storage.url/path",
      "status": "pending",
      "userId": "uuid",
      "userName": "Alice Johnson",
      "userEmail": "alice@example.com",
      "userPhone": "+233201234567",
      "licenseNumber": null,
      "expiryDate": null,
      "uploadedAt": "2025-12-25T14:20:00Z"
    }
  ],
  "driverDocs": [
    {
      "id": "driver-{userId}-license",
      "documentId": "uuid-here",
      "type": "driver",
      "subType": "license",
      "title": "Driver License",
      "url": "https://storage.url/path",
      "status": "verified",
      "userId": "uuid",
      "userName": "Bob Wilson",
      "userEmail": "bob@example.com",
      "userPhone": "+233507654321",
      "licenseNumber": "DL-12345678",
      "expiryDate": "2027-03-15T00:00:00Z",
      "uploadedAt": "2025-11-10T09:45:00Z"
    }
  ]
}
```

**Frontend Implementation Notes:**
- **Document Status Values:** `pending`, `verified`, `rejected`
- **Vehicle Document Types:** `insurance`, `roadworthy`
- **Renter Document Types:** `nationalId`
- **Driver Document Types:** `license`, `photo`
- Use the `id` field (composite string) for UI identification
- Display owner/user details alongside each document
- Filter by type using the `type` field
- Sort by `status` to prioritize pending documents

**UI Recommendations:**
```typescript
// Display format example
{vehiclePlateNumber} - {vehicleMake} {vehicleModel}
Owner: {ownerName} ({ownerEmail})
Document: {title}
Status: {status}
```

---

### 1.2 Verify Renter Documents

**Endpoint:** `PUT /api/v1/admin/documents/renter/{userId}/verify`

**Authorization:** Admin only

**Request Body:**
```json
{
  "approve": true  // true = verify, false = reject
}
```

**Request Parameters:**
- `userId` (path, UUID) - The renter's user ID

**Response (Success):**
```json
{
  "success": true,
  "userId": "uuid",
  "verificationStatus": "basic_verified",
  "message": "Renter documents verified successfully"
}
```

**Response (Rejection):**
```json
{
  "success": true,
  "userId": "uuid",
  "verificationStatus": "unverified",
  "message": "Renter documents rejected"
}
```

**Error Responses:**
```json
// 404 Not Found
{
  "error": "Renter not found"
}
```

**Frontend Implementation:**
- Add "Approve" and "Reject" buttons for each renter document
- Show confirmation dialog before submission
- Update document status in UI after successful verification
- Display success/error toast notification

---

### 1.3 Verify Driver Documents

**Endpoint:** `PUT /api/v1/admin/documents/driver/{userId}/verify`

**Authorization:** Admin only

**Request Body:**
```json
{
  "approve": true  // true = verify, false = reject
}
```

**Request Parameters:**
- `userId` (path, UUID) - The driver's user ID

**Response (Success):**
```json
{
  "success": true,
  "userId": "uuid",
  "verificationStatus": "verified",
  "message": "Driver documents verified successfully"
}
```

**Response (Rejection):**
```json
{
  "success": true,
  "userId": "uuid",
  "verificationStatus": "rejected",
  "message": "Driver documents rejected"
}
```

**Error Responses:**
```json
// 404 Not Found
{
  "error": "Driver not found"
}
```

---

## 2. Renters Management

### 2.1 Get Renters List

**Endpoint:** `GET /api/v1/admin/renters`

**Authorization:** Admin only

**Query Parameters:**
- `search` (string, optional) - Search by name, email, or phone
- `status` (string, optional) - Filter by verification status (`unverified`, `basic_verified`, `driver_verified`)
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 50) - Items per page

**Example Requests:**
```
GET /api/v1/admin/renters
GET /api/v1/admin/renters?search=john
GET /api/v1/admin/renters?status=basic_verified
GET /api/v1/admin/renters?search=john&page=1&pageSize=20
```

**Response:**
```json
{
  "total": 150,
  "page": 1,
  "pageSize": 50,
  "data": [
    {
      "userId": "uuid",
      "renterId": "uuid",
      "email": "john.doe@example.com",
      "phone": "+233501234567",
      "firstName": "John",
      "lastName": "Doe",
      "userStatus": "active",
      "verificationStatus": "basic_verified",
      "nationalIdUrl": "https://storage.url/path",
      "createdAt": "2025-12-01T10:30:00Z"
    },
    {
      "userId": "uuid",
      "renterId": "uuid",
      "email": "jane.smith@example.com",
      "phone": "+233509876543",
      "firstName": "Jane",
      "lastName": "Smith",
      "userStatus": "active",
      "verificationStatus": "unverified",
      "nationalIdUrl": null,
      "createdAt": "2025-12-15T14:20:00Z"
    }
  ]
}
```

**Verification Status Values:**
- `unverified` - No documents uploaded or verified
- `basic_verified` - National ID verified
- `driver_verified` - Can also act as a driver

**User Status Values:**
- `active` - Account is active
- `pending` - Account pending approval
- `suspended` - Account is suspended

**Frontend Implementation:**
- Implement search input with debouncing (300ms)
- Handle "undefined" string gracefully (API filters it out)
- Show verification status badges with color coding:
  - `unverified` → Red/Orange
  - `basic_verified` → Blue
  - `driver_verified` → Green
- Add pagination controls
- Link to renter's document verification page

---

## 3. Scheduled Payouts Visibility

### 3.1 Get Payouts List (Enhanced)

**Endpoint:** `GET /api/v1/admin/payouts`

**Authorization:** Admin only

**Query Parameters:**
- `ownerId` (UUID, optional) - Filter by owner
- `status` (string, optional) - Filter by status (`pending`, `processing`, `completed`, `failed`)
- `page` (int, default: 1)
- `pageSize` (int, default: 50)

**Response (Enhanced):**
```json
{
  "total": 45,
  "page": 1,
  "pageSize": 50,
  "scheduledDue": {
    "count": 3,
    "totalAmount": 1250.50,
    "message": "3 scheduled payout(s) are due. Check /api/v1/admin/payouts/scheduled/due for details."
  },
  "data": [
    {
      "id": "uuid",
      "ownerId": "uuid",
      "amount": 500.00,
      "currency": "GHS",
      "status": "pending",
      "method": "momo",
      "reference": "PAYOUT-ABC123DEF456",
      "periodStart": "2025-12-01T00:00:00Z",
      "periodEnd": "2025-12-25T23:59:59Z",
      "createdAt": "2025-12-26T10:00:00Z",
      "completedAt": null,
      "bookingCount": 12
    }
  ]
}
```

**Frontend Implementation:**
- Display prominent alert/banner when `scheduledDue.count > 0`
- Show total amount due and count in the banner
- Add "View Scheduled Payouts" button that navigates to scheduled payouts page
- Color-code the alert (e.g., yellow/orange for attention)

**Example Banner:**
```
⚠️ Action Required: 3 scheduled payouts are due (GHS 1,250.50)
[View Scheduled Payouts]
```

---

### 3.2 Get Scheduled Payouts Due

**Endpoint:** `GET /api/v1/admin/payouts/scheduled/due`

**Authorization:** Admin only

**Query Parameters:**
- `date` (DateTime, optional) - Check for specific date (default: today)

**Example Requests:**
```
GET /api/v1/admin/payouts/scheduled/due
GET /api/v1/admin/payouts/scheduled/due?date=2025-12-27
```

**Response:**
```json
{
  "date": "2025-12-27T00:00:00Z",
  "totalDue": 3,
  "totalAmount": 1250.50,
  "payouts": [
    {
      "ownerId": "uuid",
      "ownerName": "John Doe",
      "ownerEmail": "john@example.com",
      "availableBalance": 750.25,
      "minimumPayoutAmount": 50.00,
      "payoutFrequency": "weekly",
      "lastPayoutDate": "2025-12-20T10:00:00Z",
      "nextPayoutDate": "2025-12-27T00:00:00Z",
      "isDueToday": true,
      "payoutPreference": "momo",
      "payoutDetails": {
        "momoNumber": "+233501234567",
        "momoName": "John Doe",
        "provider": "MTN"
      },
      "payoutDetailsPending": null,
      "payoutVerificationStatus": "verified"
    },
    {
      "ownerId": "uuid",
      "ownerName": "Jane Smith",
      "ownerEmail": "jane@example.com",
      "availableBalance": 300.00,
      "minimumPayoutAmount": 100.00,
      "payoutFrequency": "biweekly",
      "lastPayoutDate": "2025-12-13T14:30:00Z",
      "nextPayoutDate": "2025-12-27T00:00:00Z",
      "isDueToday": true,
      "payoutPreference": "bank",
      "payoutDetails": {
        "bankName": "GCB Bank",
        "accountNumber": "1234567890",
        "accountName": "Jane Smith",
        "branchCode": "GH123"
      },
      "payoutDetailsPending": null,
      "payoutVerificationStatus": "verified"
    }
  ]
}
```

**Payout Frequency Values:**
- `daily` - Daily payouts
- `weekly` - Weekly payouts (every 7 days)
- `biweekly` - Biweekly payouts (every 14 days)
- `monthly` - Monthly payouts

**Frontend Implementation:**
- Create dedicated "Scheduled Payouts" page/tab
- Display payouts in a table/card grid
- Show owner details, balance, and payout method
- Highlight payouts that are overdue (nextPayoutDate < today)
- Add "Process Payout" button for each owner
- Show total summary at the top
- Add date picker to check future/past scheduled payouts

**UI Table Columns:**
```
Owner Name | Email | Balance | Frequency | Last Payout | Next Payout | Method | Actions
```

---

### 3.3 Process Scheduled Payouts

**Endpoint:** `POST /api/v1/admin/payouts/scheduled/process`

**Authorization:** Admin only

**Request Body:**
```json
{
  "ownerIds": [
    "uuid-1",
    "uuid-2",
    "uuid-3"
  ]
}
```

**Response:**
```json
{
  "processed": [
    {
      "ownerId": "uuid-1",
      "payoutId": "new-uuid",
      "amount": 750.25,
      "status": "pending"
    }
  ],
  "failed": [
    {
      "ownerId": "uuid-2",
      "error": "Insufficient balance"
    }
  ]
}
```

**Frontend Implementation:**
- Add checkbox selection for multiple owners
- Add "Process Selected" bulk action button
- Add "Process All" button with confirmation dialog
- Show progress indicator during processing
- Display success/failure results in a modal or toast
- Refresh the scheduled payouts list after processing

---

## 4. User Status Management Updates

### 4.1 Update User Status (Verified Alias)

**Endpoint:** `PUT /api/v1/admin/users/{userId}/status`

**Authorization:** Admin only

**Request Body:**
```json
{
  "status": "verified"  // Now accepts: active, pending, suspended, verified
}
```

**Note:** The backend now accepts `"verified"` as an alias for `"active"`. This provides better UX consistency.

**Valid Status Values:**
- `active` - Account is active and can operate
- `verified` - Alias for `active` (recommended for UI)
- `pending` - Account pending approval (owners/admins cannot login)
- `suspended` - Account is suspended

**Response:**
```json
{
  "id": "uuid",
  "status": "active"  // Returns "active" even if "verified" was sent
}
```

**Frontend Implementation:**
- Update dropdown/select options to use "Verified" instead of "Active"
- Map "verified" → "active" in UI display if needed
- Show appropriate status badges

---

## 5. API Error Handling

All endpoints follow consistent error response patterns:

### 400 Bad Request
```json
{
  "error": "Descriptive error message"
}
```

### 401 Unauthorized
```json
{
  "error": "Unauthorized"
}
```

### 404 Not Found
```json
{
  "error": "Resource not found message"
}
```

### 500 Internal Server Error
```json
{
  "error": "Error: {message} | Inner: {innerMessage}"
}
```

---

## 6. Implementation Checklist

### Admin Dashboard - Documents Section
- [ ] Update GET /api/v1/admin/documents integration
- [ ] Display owner details for vehicle documents
- [ ] Display user details for renter/driver documents
- [ ] Add "Approve" and "Reject" buttons for renter documents
- [ ] Add "Approve" and "Reject" buttons for driver documents
- [ ] Implement verification confirmation dialogs
- [ ] Update document status after verification
- [ ] Add status badges (pending, verified, rejected)
- [ ] Fix "Unknown Owner" display issue

### Admin Dashboard - Renters Section
- [ ] Create new Renters page/section
- [ ] Implement GET /api/v1/admin/renters integration
- [ ] Add search input with debouncing
- [ ] Add verification status filter dropdown
- [ ] Implement pagination controls
- [ ] Display verification status badges
- [ ] Link to renter's documents for verification

### Admin Dashboard - Payouts Section
- [ ] Update GET /api/v1/admin/payouts integration
- [ ] Display scheduled payouts alert banner
- [ ] Create "Scheduled Payouts" page/tab
- [ ] Implement GET /api/v1/admin/payouts/scheduled/due integration
- [ ] Display scheduled payouts table/grid
- [ ] Add owner selection checkboxes
- [ ] Implement "Process Selected" functionality
- [ ] Implement "Process All" with confirmation
- [ ] Add date picker for scheduled payout queries
- [ ] Show payout frequency information
- [ ] Display payout method details (MoMo/Bank)

### General Updates
- [ ] Update user status options to include "Verified"
- [ ] Test all new endpoints with Postman/Insomnia
- [ ] Update API documentation
- [ ] Handle "undefined" string in search parameters
- [ ] Add loading states for all API calls
- [ ] Implement proper error handling and user feedback
- [ ] Test pagination edge cases
- [ ] Verify mobile responsiveness

---

## 7. Testing Scenarios

### Document Verification
1. **Pending Documents:** Verify that pending documents appear in the list
2. **Owner Details:** Confirm owner name, email, and phone display correctly
3. **Approve Flow:** Test approving a renter document
4. **Reject Flow:** Test rejecting a driver document
5. **Status Update:** Verify UI updates after verification
6. **Error Handling:** Test with invalid userId

### Renters Management
1. **Search Functionality:** Test search by name, email, phone
2. **Status Filtering:** Test filtering by verification status
3. **Pagination:** Test navigation between pages
4. **Empty State:** Test with no search results
5. **Undefined Handling:** Verify "undefined" string is filtered out

### Scheduled Payouts
1. **Banner Display:** Verify alert shows when payouts are due
2. **Due Payouts List:** Test scheduled payouts endpoint
3. **Process Single:** Process one owner's payout
4. **Process Multiple:** Process multiple owners at once
5. **Error Scenarios:** Test with insufficient balance
6. **Date Queries:** Test querying future/past dates
7. **Frequency Display:** Verify frequency labels are correct

---

## 8. Example API Integration (TypeScript/React)

### Document Verification Service
```typescript
// services/documentService.ts

export interface DocumentItem {
  id: string;
  documentId: string;
  type: 'vehicle' | 'renter' | 'driver';
  subType: string;
  title: string;
  url: string;
  status: 'pending' | 'verified' | 'rejected';
  ownerId?: string;
  ownerName?: string;
  ownerEmail?: string;
  ownerPhone?: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  userPhone?: string;
  uploadedAt?: string;
  expiryDate?: string;
}

export interface DocumentsResponse {
  total: number;
  vehicleDocs: DocumentItem[];
  renterDocs: DocumentItem[];
  driverDocs: DocumentItem[];
}

export async function getDocuments(): Promise<DocumentsResponse> {
  const response = await fetch('/api/v1/admin/documents', {
    headers: {
      'Authorization': `Bearer ${getToken()}`,
    },
  });
  
  if (!response.ok) {
    throw new Error('Failed to fetch documents');
  }
  
  return response.json();
}

export async function verifyRenterDocuments(
  userId: string, 
  approve: boolean
): Promise<void> {
  const response = await fetch(
    `/api/v1/admin/documents/renter/${userId}/verify`, 
    {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ approve }),
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Verification failed');
  }
}

export async function verifyDriverDocuments(
  userId: string, 
  approve: boolean
): Promise<void> {
  const response = await fetch(
    `/api/v1/admin/documents/driver/${userId}/verify`, 
    {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ approve }),
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Verification failed');
  }
}
```

### Renters Service
```typescript
// services/renterService.ts

export interface Renter {
  userId: string;
  renterId: string;
  email: string;
  phone: string;
  firstName: string;
  lastName: string;
  userStatus: string;
  verificationStatus: string;
  nationalIdUrl: string | null;
  createdAt: string;
}

export interface RentersResponse {
  total: number;
  page: number;
  pageSize: number;
  data: Renter[];
}

export async function getRenters(params: {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<RentersResponse> {
  const queryParams = new URLSearchParams();
  
  if (params.search && params.search !== 'undefined') {
    queryParams.set('search', params.search);
  }
  if (params.status) {
    queryParams.set('status', params.status);
  }
  if (params.page) {
    queryParams.set('page', params.page.toString());
  }
  if (params.pageSize) {
    queryParams.set('pageSize', params.pageSize.toString());
  }
  
  const response = await fetch(
    `/api/v1/admin/renters?${queryParams}`,
    {
      headers: {
        'Authorization': `Bearer ${getToken()}`,
      },
    }
  );
  
  if (!response.ok) {
    throw new Error('Failed to fetch renters');
  }
  
  return response.json();
}
```

### Scheduled Payouts Service
```typescript
// services/payoutService.ts

export interface ScheduledPayout {
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  availableBalance: number;
  minimumPayoutAmount: number;
  payoutFrequency: string;
  lastPayoutDate: string;
  nextPayoutDate: string;
  isDueToday: boolean;
  payoutPreference: string;
  payoutDetails: any;
  payoutDetailsPending: any;
  payoutVerificationStatus: string;
}

export interface ScheduledPayoutsResponse {
  date: string;
  totalDue: number;
  totalAmount: number;
  payouts: ScheduledPayout[];
}

export async function getScheduledPayoutsDue(
  date?: string
): Promise<ScheduledPayoutsResponse> {
  const queryParams = new URLSearchParams();
  if (date) {
    queryParams.set('date', date);
  }
  
  const response = await fetch(
    `/api/v1/admin/payouts/scheduled/due?${queryParams}`,
    {
      headers: {
        'Authorization': `Bearer ${getToken()}`,
      },
    }
  );
  
  if (!response.ok) {
    throw new Error('Failed to fetch scheduled payouts');
  }
  
  return response.json();
}

export async function processScheduledPayouts(
  ownerIds: string[]
): Promise<any> {
  const response = await fetch(
    '/api/v1/admin/payouts/scheduled/process',
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ ownerIds }),
    }
  );
  
  if (!response.ok) {
    throw new Error('Failed to process scheduled payouts');
  }
  
  return response.json();
}
```

---

## 9. UI/UX Recommendations

### Document Verification Page
- Group documents by type (Vehicle, Renter, Driver) with tabs or sections
- Show pending documents first (sort by status)
- Display document preview/modal on click
- Use color-coded status badges:
  - 🔴 Pending - Red/Orange
  - 🟢 Verified - Green
  - 🔴 Rejected - Red
- Add bulk actions (approve/reject multiple)
- Show verification history/audit log

### Renters Page
- Clean table layout with search and filters at the top
- Quick actions in each row (View Details, View Documents)
- Status badges with tooltips explaining each status
- Export to CSV functionality
- Click row to view full renter profile

### Scheduled Payouts Page
- Dashboard widget showing summary (total due, count)
- Calendar view option to see upcoming payouts
- Detailed list view with owner information
- Highlight overdue payouts
- Show payout method icons (MoMo, Bank)
- Add notes/comments for each payout processing

---

## 10. Migration Notes

### Breaking Changes
None - all changes are additions or enhancements to existing endpoints.

### New Permissions Required
All new endpoints require `admin` role - ensure frontend properly checks user roles before displaying these features.

### Deprecated Endpoints
None

---

## 11. Support & Questions

For questions or issues with implementation:
1. Check API response format matches documentation
2. Verify authorization headers are correctly set
3. Review console logs for error details
4. Test with Postman/Insomnia to isolate frontend issues

---

**Document Version:** 1.0  
**Last Updated:** December 27, 2025  
**API Base URL:** `https://ryverental.info`  
**Environment:** Production
