# Vehicle Soft Delete - v1.223

## Overview
Implemented soft delete functionality for vehicles to preserve all bookings, revenue, and historical data when vehicles are removed by owners.

---

## Problem Solved
**Before:** When a vehicle was deleted (hard delete), all associated bookings, payments, reviews, and revenue history were permanently lost due to CASCADE delete constraints.

**After:** Vehicles are "soft deleted" by setting a `DeletedAt` timestamp. All historical data is preserved for accounting, audit, and analytics purposes.

---

## Database Changes

### New Column Added
```sql
ALTER TABLE "Vehicles" 
ADD COLUMN "DeletedAt" timestamp without time zone NULL;
```

- **NULL = Active vehicle** (normal operation)
- **NOT NULL = Deleted vehicle** (hidden from users, data preserved)

### Performance Index
```sql
CREATE INDEX "IX_Vehicles_DeletedAt" ON "Vehicles" ("DeletedAt") 
WHERE "DeletedAt" IS NULL;
```
Optimizes queries for active vehicles (most common case).

---

## API Changes

### Behavior Changes (No Frontend Changes Required)

#### 1. Owner Delete Endpoint
**Endpoint:** `DELETE /api/v1/owner/vehicles/{vehicleId}`

**Before:**
- Set status to "inactive"
- Vehicle still appeared in some queries

**After:**
- Sets `DeletedAt = NOW()`
- Vehicle completely hidden from all normal queries
- Returns: `{ "message": "Vehicle removed successfully. All booking history preserved.", "vehicleId": "...", "deletedAt": "2026-01-05T14:30:00Z" }`

#### 2. All Vehicle Queries
All endpoints now automatically filter out deleted vehicles:
- `GET /api/v1/vehicles` (public search)
- `GET /api/v1/vehicles/{id}` (vehicle details)
- `GET /api/v1/owner/vehicles` (owner's vehicles)
- `GET /api/v1/admin/vehicles` (admin list)
- Owner/admin vehicle updates and photo uploads

**Frontend Impact:** ZERO - deleted vehicles simply don't appear anymore

---

## New Admin Features

### 1. View Deleted Vehicles

**Endpoint:** `GET /api/v1/admin/vehicles/deleted?ownerId={ownerId}`

**Authorization:** Admin only

**Response:**
```json
{
  "total": 5,
  "message": null,
  "data": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "plateNumber": "GR-1234-20",
      "make": "Toyota",
      "model": "Corolla",
      "year": 2020,
      "status": "active",
      "deletedAt": "2026-01-05T14:30:00Z",
      "deletedDaysAgo": 0,
      "owner": {
        "id": "...",
        "email": "owner@example.com",
        "name": "John Doe"
      },
      "category": {
        "id": "...",
        "name": "Compact"
      },
      "city": {
        "id": "...",
        "name": "Accra"
      },
      "photos": ["https://..."],
      "bookingCount": 15
    }
  ]
}
```

**Use Cases:**
- Review recently deleted vehicles
- Identify accidental deletions
- Audit vehicle removals
- Analyze deleted vehicle patterns

---

### 2. Restore Deleted Vehicle

**Endpoint:** `POST /api/v1/admin/vehicles/{vehicleId}/restore`

**Authorization:** Admin only

**Response:**
```json
{
  "message": "Vehicle restored successfully",
  "vehicle": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "plateNumber": "GR-1234-20",
    "make": "Toyota",
    "model": "Corolla",
    "year": 2020,
    "status": "pending_review",
    "restoredAt": "2026-01-05T15:00:00Z",
    "owner": {
      "id": "...",
      "email": "owner@example.com",
      "firstName": "John",
      "lastName": "Doe"
    }
  }
}
```

**Behavior:**
- Sets `DeletedAt = NULL`
- Resets status to `pending_review` (requires re-approval)
- Vehicle becomes visible again

---

## Frontend Integration

### Admin Dashboard - Deleted Vehicles Page

```jsx
// Fetch deleted vehicles
const response = await fetch('/api/v1/admin/vehicles/deleted', {
  headers: {
    'Authorization': `Bearer ${adminToken}`
  }
});
const { data } = await response.json();

// Display deleted vehicles table
<table>
  <thead>
    <tr>
      <th>Vehicle</th>
      <th>Owner</th>
      <th>Deleted</th>
      <th>Bookings</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    {data.map(vehicle => (
      <tr key={vehicle.id}>
        <td>{vehicle.make} {vehicle.model} ({vehicle.year})</td>
        <td>{vehicle.owner.name}</td>
        <td>{vehicle.deletedDaysAgo} days ago</td>
        <td>{vehicle.bookingCount}</td>
        <td>
          <button onClick={() => restoreVehicle(vehicle.id)}>
            Restore
          </button>
        </td>
      </tr>
    ))}
  </tbody>
</table>

// Restore vehicle function
const restoreVehicle = async (vehicleId) => {
  const response = await fetch(`/api/v1/admin/vehicles/${vehicleId}/restore`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`
    }
  });
  
  if (response.ok) {
    alert('Vehicle restored successfully!');
    // Refresh list
  }
};
```

---

## Benefits

### For Accounting
✅ **Revenue history preserved** - All booking payments remain in database  
✅ **Payout tracking maintained** - Historical owner payouts intact  
✅ **Tax reporting accurate** - Complete transaction history  

### For Audit/Compliance
✅ **Complete audit trail** - Who deleted what and when  
✅ **Restore capability** - Undo accidental deletions  
✅ **Booking history** - Customer service can reference past rentals  

### For Analytics
✅ **Vehicle performance analysis** - Compare active vs deleted vehicles  
✅ **Owner churn analysis** - Track why vehicles are removed  
✅ **Seasonal patterns** - Identify deletion trends  

### For Owners
✅ **Mistake protection** - Admin can restore accidentally deleted vehicles  
✅ **Historical data** - Past bookings remain accessible for references  
✅ **Clean interface** - Deleted vehicles don't clutter their dashboard  

---

## Testing Checklist

### Owner Operations
- [ ] Delete a vehicle with no bookings
- [ ] Try to delete vehicle with active booking (should fail)
- [ ] Verify deleted vehicle doesn't appear in owner's list
- [ ] Verify deleted vehicle doesn't appear in search
- [ ] Try to update a deleted vehicle (should fail)
- [ ] Try to upload photos to deleted vehicle (should fail)

### Admin Operations
- [ ] View list of deleted vehicles
- [ ] Filter deleted vehicles by owner
- [ ] Verify booking count is correct
- [ ] Restore a deleted vehicle
- [ ] Verify restored vehicle appears with status "pending_review"
- [ ] Verify restored vehicle appears in admin list again

### Data Integrity
- [ ] Delete vehicle, verify all bookings still exist in database
- [ ] Verify payment transactions still exist
- [ ] Verify reviews still exist
- [ ] Run force delete, verify CASCADE still works for actual deletion

### Performance
- [ ] Search vehicles (should be fast with index)
- [ ] Owner vehicle list (should be fast)
- [ ] Admin view all vehicles (should exclude deleted)

---

## Migration Path

1. **Database:** ✅ Migration already run
2. **Backend:** Deploy v1.223 (zero downtime)
3. **Frontend:** No changes required (optional: add admin deleted vehicles page)

---

## Rollback Plan

If issues arise:

```sql
-- Remove soft delete column (destructive!)
ALTER TABLE "Vehicles" DROP COLUMN "DeletedAt";

-- Deploy previous version (v1.222)
```

⚠️ **Note:** This will lose soft delete data. Better to fix forward.

---

## Future Enhancements

1. **Auto-purge** - Permanently delete vehicles after 90 days
2. **Owner self-restore** - Let owners restore their own vehicles
3. **Bulk operations** - Admin bulk restore/permanent delete
4. **Deletion reasons** - Track why vehicles were deleted
5. **Email notifications** - Notify owner when vehicle deleted/restored

---

## Version Information

- **Version:** 1.223
- **Deployed:** 2026-01-05
- **Breaking Changes:** None (fully backward compatible)
- **Database Changes:** 1 new column + 1 index
- **API Changes:** 2 new admin endpoints, behavior change in delete

---

## Documentation

This feature is part of the ongoing effort to preserve historical data for accounting and audit purposes.

Related features:
- Deposit refunds (already implemented with audit logs)
- Payout history (already implemented with tracking)
- Booking history (never deleted, now vehicle delete won't remove them)
