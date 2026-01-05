# Admin & Owner Dashboard: Deleted Vehicles Management
**Version:** 1.223  
**Last Updated:** January 5, 2026

## Overview
This guide provides implementation details for integrating deleted vehicle management features into the admin and owner dashboards. The soft delete system preserves all historical data (bookings, payments, reviews) while allowing vehicles to be removed from active listings.

---

## Table of Contents
1. [Admin Dashboard Features](#admin-dashboard-features)
2. [Owner Dashboard Features](#owner-dashboard-features)
3. [API Endpoints Reference](#api-endpoints-reference)
4. [Frontend Implementation Guide](#frontend-implementation-guide)
5. [Sample Responses](#sample-responses)
6. [Error Handling](#error-handling)

---

## Admin Dashboard Features

### 1. View Deleted Vehicles
**Feature:** Display all soft-deleted vehicles across the entire platform with owner information and deletion timestamp.

**Use Cases:**
- Audit trail for compliance and accounting
- Review deleted vehicles for recovery
- Monitor owner deletion patterns
- Customer support investigations

**UI Components:**
- Deleted Vehicles page/tab in admin panel
- Table showing: Vehicle name, owner, location, deletion date, booking count
- Search and filter functionality
- Restore button for each vehicle

### 2. Restore Deleted Vehicles
**Feature:** Restore any soft-deleted vehicle back to active status.

**Use Cases:**
- Accidental deletion recovery
- Owner requests to restore vehicle
- Compliance requirements
- Business continuity

**UI Components:**
- Restore button with confirmation modal
- Success/error notification
- Real-time table update after restoration

---

## Owner Dashboard Features

### 1. Delete Vehicle (Soft Delete)
**Feature:** Owners can delete their vehicles without losing historical booking/revenue data.

**What Changed:**
- ✅ Deletion no longer destroys bookings, payments, reviews, or revenue history
- ✅ Deleted vehicles are hidden from renter searches
- ✅ All historical data preserved for accounting and compliance
- ✅ No changes to frontend delete flow - works exactly the same

**UI Components:**
- Existing delete button/action remains unchanged
- Add informational tooltip: "Deleting hides your vehicle from renters while preserving all booking history"
- Success message: "Vehicle deleted. Historical data preserved for accounting."

### 2. View Deleted Vehicles (Optional)
**Feature:** Owners can see their own deleted vehicles (if you want to implement this).

**Use Cases:**
- Review deleted vehicles
- Access historical performance data
- Restore previously deleted vehicles

**UI Components:**
- "Deleted Vehicles" tab in owner vehicle management
- Table showing deleted vehicles with deletion date
- View details button (read-only)

---

## API Endpoints Reference

### Admin Endpoints

#### 1. Get All Deleted Vehicles
```
GET /api/v1/admin/vehicles/deleted
Authorization: Bearer {admin_jwt_token}
```

**Response:** See [Sample Response 1](#sample-response-1-get-all-deleted-vehicles)

---

#### 2. Restore Deleted Vehicle
```
POST /api/v1/admin/vehicles/{vehicleId}/restore
Authorization: Bearer {admin_jwt_token}
```

**Response:** See [Sample Response 2](#sample-response-2-restore-deleted-vehicle)

---

### Owner Endpoints

#### 3. Delete Vehicle (Soft Delete)
```
DELETE /api/v1/owner/vehicles/{vehicleId}
Authorization: Bearer {owner_jwt_token}
```

**Response:** See [Sample Response 3](#sample-response-3-delete-vehicle-soft-delete)

---

#### 4. Get Owner Vehicles (Active Only)
```
GET /api/v1/owner/vehicles
Authorization: Bearer {owner_jwt_token}
```

**Note:** This endpoint now automatically filters out deleted vehicles. Only active vehicles are returned.

**Response:** See [Sample Response 4](#sample-response-4-get-owner-vehicles)

---

## Frontend Implementation Guide

### Admin Dashboard Implementation

#### Step 1: Create Deleted Vehicles Page
```typescript
// pages/admin/DeletedVehicles.tsx
import { useState, useEffect } from 'react';
import axios from 'axios';

interface DeletedVehicle {
  id: number;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  location: string;
  ownerName: string;
  ownerEmail: string;
  deletedAt: string;
  totalBookings: number;
  totalRevenue: number;
}

export default function DeletedVehiclesPage() {
  const [vehicles, setVehicles] = useState<DeletedVehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchDeletedVehicles();
  }, []);

  const fetchDeletedVehicles = async () => {
    try {
      const response = await axios.get(
        'https://your-api-url/api/v1/admin/vehicles/deleted',
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('adminToken')}`
          }
        }
      );
      setVehicles(response.data.vehicles);
      setLoading(false);
    } catch (err) {
      setError('Failed to load deleted vehicles');
      setLoading(false);
    }
  };

  const handleRestore = async (vehicleId: number) => {
    if (!confirm('Are you sure you want to restore this vehicle?')) return;

    try {
      await axios.post(
        `https://your-api-url/api/v1/admin/vehicles/${vehicleId}/restore`,
        {},
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('adminToken')}`
          }
        }
      );
      
      // Remove from list or refresh
      setVehicles(vehicles.filter(v => v.id !== vehicleId));
      alert('Vehicle restored successfully!');
    } catch (err) {
      alert('Failed to restore vehicle');
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="error">{error}</div>;

  return (
    <div className="deleted-vehicles-page">
      <h1>Deleted Vehicles ({vehicles.length})</h1>
      
      <table className="vehicles-table">
        <thead>
          <tr>
            <th>Vehicle</th>
            <th>Owner</th>
            <th>Location</th>
            <th>Deleted Date</th>
            <th>Bookings</th>
            <th>Revenue</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {vehicles.map(vehicle => (
            <tr key={vehicle.id}>
              <td>
                {vehicle.year} {vehicle.make} {vehicle.model}
                <br />
                <small>{vehicle.licensePlate}</small>
              </td>
              <td>
                {vehicle.ownerName}
                <br />
                <small>{vehicle.ownerEmail}</small>
              </td>
              <td>{vehicle.location}</td>
              <td>{new Date(vehicle.deletedAt).toLocaleDateString()}</td>
              <td>{vehicle.totalBookings}</td>
              <td>GH₵ {vehicle.totalRevenue.toLocaleString()}</td>
              <td>
                <button 
                  onClick={() => handleRestore(vehicle.id)}
                  className="btn-restore"
                >
                  Restore
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

#### Step 2: Add Navigation Link
```typescript
// Add to admin navigation menu
<NavLink to="/admin/vehicles/deleted">
  Deleted Vehicles
  {deletedCount > 0 && <Badge>{deletedCount}</Badge>}
</NavLink>
```

---

### Owner Dashboard Implementation

#### Update Delete Confirmation
```typescript
// pages/owner/VehicleManagement.tsx
const handleDeleteVehicle = async (vehicleId: number) => {
  const confirmed = confirm(
    'Are you sure you want to delete this vehicle?\n\n' +
    'Note: Your vehicle will be hidden from renters, but all booking ' +
    'history, payments, and reviews will be preserved for your records.'
  );
  
  if (!confirmed) return;

  try {
    await axios.delete(
      `https://your-api-url/api/v1/owner/vehicles/${vehicleId}`,
      {
        headers: {
          Authorization: `Bearer ${localStorage.getItem('ownerToken')}`
        }
      }
    );
    
    // Update UI
    setVehicles(vehicles.filter(v => v.id !== vehicleId));
    
    // Show success message
    showNotification(
      'Vehicle deleted successfully. All historical data has been preserved.',
      'success'
    );
  } catch (err) {
    showNotification('Failed to delete vehicle', 'error');
  }
};
```

#### Add Informational Tooltip (Optional)
```typescript
<Tooltip content="Deleting hides your vehicle from renters while preserving all booking history and revenue data for accounting purposes.">
  <InfoIcon />
</Tooltip>
```

---

## Sample Responses

### Sample Response 1: Get All Deleted Vehicles

**Request:**
```http
GET /api/v1/admin/vehicles/deleted HTTP/1.1
Host: 48.192.124.95
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Deleted vehicles retrieved successfully",
  "count": 3,
  "vehicles": [
    {
      "id": 42,
      "make": "Toyota",
      "model": "Camry",
      "year": 2020,
      "licensePlate": "GH-4582-21",
      "location": "Accra, Ghana",
      "pricePerDay": 150.00,
      "ownerName": "Kwame Mensah",
      "ownerEmail": "kwame@example.com",
      "ownerId": 15,
      "deletedAt": "2026-01-03T14:23:15Z",
      "totalBookings": 47,
      "totalRevenue": 7050.00,
      "activeBookingsAtDeletion": 0,
      "lastBookingDate": "2025-12-28T00:00:00Z",
      "images": [
        "https://ryveimages.blob.core.windows.net/vehicles/42/main.jpg"
      ]
    },
    {
      "id": 38,
      "make": "Honda",
      "model": "Accord",
      "year": 2019,
      "licensePlate": "GH-3421-20",
      "location": "Kumasi, Ghana",
      "pricePerDay": 120.00,
      "ownerName": "Ama Boateng",
      "ownerEmail": "ama@example.com",
      "ownerId": 22,
      "deletedAt": "2026-01-02T09:15:42Z",
      "totalBookings": 32,
      "totalRevenue": 3840.00,
      "activeBookingsAtDeletion": 0,
      "lastBookingDate": "2025-12-20T00:00:00Z",
      "images": []
    },
    {
      "id": 25,
      "make": "Nissan",
      "model": "Altima",
      "year": 2021,
      "licensePlate": "GH-5678-22",
      "location": "Takoradi, Ghana",
      "pricePerDay": 180.00,
      "ownerName": "Yaw Asante",
      "ownerEmail": "yaw@example.com",
      "ownerId": 8,
      "deletedAt": "2025-12-30T16:45:20Z",
      "totalBookings": 15,
      "totalRevenue": 2700.00,
      "activeBookingsAtDeletion": 0,
      "lastBookingDate": "2025-12-22T00:00:00Z",
      "images": [
        "https://ryveimages.blob.core.windows.net/vehicles/25/main.jpg",
        "https://ryveimages.blob.core.windows.net/vehicles/25/interior.jpg"
      ]
    }
  ]
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Admin authentication required"
}
```

**Error Response (403 Forbidden):**
```json
{
  "success": false,
  "message": "Access denied. Admin role required."
}
```

---

### Sample Response 2: Restore Deleted Vehicle

**Request:**
```http
POST /api/v1/admin/vehicles/42/restore HTTP/1.1
Host: 48.192.124.95
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Length: 0
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Vehicle restored successfully",
  "vehicle": {
    "id": 42,
    "make": "Toyota",
    "model": "Camry",
    "year": 2020,
    "licensePlate": "GH-4582-21",
    "location": "Accra, Ghana",
    "pricePerDay": 150.00,
    "status": "active",
    "ownerId": 15,
    "deletedAt": null,
    "restoredAt": "2026-01-05T15:30:42Z",
    "totalBookings": 47,
    "images": [
      "https://ryveimages.blob.core.windows.net/vehicles/42/main.jpg"
    ]
  }
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Deleted vehicle not found with ID: 999"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Vehicle is not deleted (already active)"
}
```

---

### Sample Response 3: Delete Vehicle (Soft Delete)

**Request:**
```http
DELETE /api/v1/owner/vehicles/42 HTTP/1.1
Host: 48.192.124.95
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Vehicle deleted successfully. All booking history and revenue data has been preserved for your records."
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Vehicle not found or already deleted"
}
```

**Error Response (403 Forbidden):**
```json
{
  "success": false,
  "message": "You don't have permission to delete this vehicle"
}
```

**Error Response (400 Bad Request - Active Bookings):**
```json
{
  "success": false,
  "message": "Cannot delete vehicle with active bookings",
  "activeBookings": [
    {
      "id": 156,
      "startDate": "2026-01-10",
      "endDate": "2026-01-15",
      "status": "confirmed"
    }
  ]
}
```

---

### Sample Response 4: Get Owner Vehicles

**Request:**
```http
GET /api/v1/owner/vehicles HTTP/1.1
Host: 48.192.124.95
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Vehicles retrieved successfully",
  "count": 2,
  "vehicles": [
    {
      "id": 45,
      "make": "Mercedes",
      "model": "C-Class",
      "year": 2022,
      "licensePlate": "GH-7890-23",
      "location": "Accra, Ghana",
      "pricePerDay": 250.00,
      "status": "active",
      "totalBookings": 23,
      "totalRevenue": 5750.00,
      "images": [
        "https://ryveimages.blob.core.windows.net/vehicles/45/main.jpg"
      ],
      "deletedAt": null
    },
    {
      "id": 48,
      "make": "BMW",
      "model": "X5",
      "year": 2021,
      "licensePlate": "GH-9012-22",
      "location": "Accra, Ghana",
      "pricePerDay": 300.00,
      "status": "active",
      "totalBookings": 18,
      "totalRevenue": 5400.00,
      "images": [
        "https://ryveimages.blob.core.windows.net/vehicles/48/main.jpg",
        "https://ryveimages.blob.core.windows.net/vehicles/48/interior.jpg"
      ],
      "deletedAt": null
    }
  ]
}
```

**Note:** Previously deleted vehicles (where `deletedAt` is not null) are automatically filtered out and will NOT appear in this response.

---

## Error Handling

### Common Error Scenarios

#### 1. Unauthorized Access
```json
{
  "success": false,
  "message": "Authentication required",
  "statusCode": 401
}
```
**Solution:** Ensure valid JWT token is included in Authorization header.

---

#### 2. Insufficient Permissions
```json
{
  "success": false,
  "message": "Admin role required",
  "statusCode": 403
}
```
**Solution:** User must have admin role to access admin endpoints.

---

#### 3. Vehicle Not Found
```json
{
  "success": false,
  "message": "Vehicle not found with ID: 123",
  "statusCode": 404
}
```
**Solution:** Verify vehicle ID exists and is deleted (for restore operation).

---

#### 4. Already Active Vehicle
```json
{
  "success": false,
  "message": "Vehicle is not deleted (already active)",
  "statusCode": 400
}
```
**Solution:** Cannot restore a vehicle that is already active.

---

#### 5. Active Bookings Conflict
```json
{
  "success": false,
  "message": "Cannot delete vehicle with active bookings",
  "activeBookings": [...],
  "statusCode": 400
}
```
**Solution:** Wait for active bookings to complete or cancel them before deleting.

---

## Testing Checklist

### Admin Dashboard
- [ ] Can view all deleted vehicles from all owners
- [ ] Deleted vehicles show correct owner information
- [ ] Total bookings and revenue are accurate
- [ ] Can restore deleted vehicle successfully
- [ ] Restored vehicle disappears from deleted list
- [ ] Restored vehicle appears in owner's active vehicles
- [ ] Cannot restore already-active vehicle
- [ ] Error messages display correctly
- [ ] Loading states work properly
- [ ] Empty state when no deleted vehicles

### Owner Dashboard
- [ ] Delete button works as before (no frontend changes needed)
- [ ] Vehicle is removed from active list after deletion
- [ ] Success message confirms data preservation
- [ ] Cannot delete vehicle with active bookings
- [ ] Deleted vehicles don't appear in vehicle list
- [ ] Historical booking data still accessible (if you implement reports)
- [ ] Error handling works correctly

### Renter Experience
- [ ] Deleted vehicles do NOT appear in search results
- [ ] Cannot access deleted vehicle detail pages
- [ ] Existing bookings for deleted vehicles still visible to renters
- [ ] No disruption to user experience

---

## Implementation Timeline

### Phase 1: Backend (✅ COMPLETE - v1.223)
- [x] Soft delete implementation
- [x] Admin endpoints created
- [x] Database migration executed
- [x] Deployed to production (IP: 48.192.124.95)

### Phase 2: Admin Dashboard (PENDING)
- [ ] Create deleted vehicles page
- [ ] Implement restore functionality
- [ ] Add navigation menu item
- [ ] Test all admin operations

### Phase 3: Owner Dashboard (OPTIONAL)
- [ ] Update delete confirmation message
- [ ] Add informational tooltips
- [ ] Test delete operation
- [ ] Verify data preservation messaging

### Phase 4: Testing & Documentation
- [ ] Complete QA testing
- [ ] Update user documentation
- [ ] Train admin/support staff
- [ ] Monitor production usage

---

## Support & Questions

### API Base URL
**Production:** `http://48.192.124.95`  
**API Version:** v1  

### Authentication
All endpoints require JWT authentication with appropriate role:
- Admin endpoints: Require `admin` role
- Owner endpoints: Require `owner` role

### Need Help?
- API Documentation: `/swagger` endpoint (if enabled)
- Backend Version: v1.223
- Feature Documentation: `VEHICLE_SOFT_DELETE_v1.223.md`

---

## Appendix: Database Schema Reference

### Vehicles Table (Relevant Columns)
```sql
"Vehicles" (
  "Id" integer PRIMARY KEY,
  "Make" text NOT NULL,
  "Model" text NOT NULL,
  "Year" integer NOT NULL,
  "LicensePlate" text NOT NULL,
  "Location" text,
  "PricePerDay" numeric NOT NULL,
  "Status" text NOT NULL,
  "OwnerId" integer NOT NULL,
  "DeletedAt" timestamp NULL,  -- NEW: Soft delete timestamp
  "CreatedAt" timestamp NOT NULL,
  "UpdatedAt" timestamp NOT NULL
)
```

### Index for Performance
```sql
CREATE INDEX "IX_Vehicles_DeletedAt" 
ON "Vehicles" ("DeletedAt") 
WHERE "DeletedAt" IS NULL;
```

This partial index optimizes queries for active vehicles (most common operation).

---

**Document Version:** 1.0  
**API Version:** 1.223  
**Last Updated:** January 5, 2026  
**Author:** Ghana Hybrid Rental API Team
