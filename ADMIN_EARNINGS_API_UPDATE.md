# Admin Earnings API Update - Revenue Calculation Changes

## Overview
Updated admin revenue calculations to reflect that the platform fee is charged to both the renter (as "Service Fee") and the owner (as "Platform Fee"), effectively doubling the admin's platform revenue.

## Changes Made

### 1. Revenue Calculation Updates
- **Admin Platform Revenue**: Now calculated as `platformFee × 2` (charged to both renter and owner)
- **Owner Earnings**: Shows actual net amount owner receives: `rental + driver - platformFee`
- **Customer-facing terminology**: Changed "Platform Fee" to "Service Fee" in all customer emails and templates

### 2. Updated API Endpoints

#### GET `/api/v1/admin/revenue-analytics`

**Query Parameters:**
- `from` (DateTime): Start date for analytics period
- `to` (DateTime): End date for analytics period

**Sample Response:**

```json
{
  "period": {
    "from": "2026-01-01T00:00:00Z",
    "to": "2026-01-10T23:59:59Z"
  },
  "summary": {
    "totalRevenue": 682.50,
    "protectionPlanRevenue": 50.00,
    "platformFeeRevenue": 82.50,
    "adminPlatformRevenue": 165.00,
    "insuranceRevenue": 0.00,
    "ownerRevenue": 467.50,
    "totalBookings": 1,
    "avgRevenuePerBooking": 682.50
  },
  "trending": {
    "revenueChangePercent": 0.00,
    "bookingsChangePercent": 0.00,
    "previousPeriod": {
      "from": "2025-12-22T00:00:00Z",
      "to": "2025-12-31T23:59:59Z",
      "revenue": 0.00,
      "bookings": 0
    }
  },
  "forecast": {
    "expectedRevenue": 2055.00,
    "expectedPlatformRevenue": 260.00,
    "expectedOwnerRevenue": 595.00,
    "upcomingBookings": 1
  },
  "byDay": [
    {
      "date": "2026-01-10",
      "totalRevenue": 682.50,
      "protectionRevenue": 50.00,
      "platformFeeRevenue": 82.50,
      "adminPlatformRevenue": 165.00,
      "insuranceRevenue": 0.00,
      "ownerRevenue": 467.50,
      "bookings": 1
    }
  ],
  "byMonth": [
    {
      "year": 2026,
      "month": 1,
      "totalRevenue": 682.50,
      "protectionRevenue": 50.00,
      "platformFeeRevenue": 82.50,
      "adminPlatformRevenue": 165.00,
      "insuranceRevenue": 0.00,
      "ownerRevenue": 467.50,
      "bookings": 1
    }
  ]
}
```

### 3. Revenue Breakdown Explanation

For a completed booking with these details:
- **Rental Amount**: 550.00 GHS
- **Protection Plan**: 50.00 GHS
- **Platform Fee**: 82.50 GHS (15% of 550.00)
- **Driver Amount**: 0.00 GHS
- **Total Amount**: 682.50 GHS (excluding 1000 GHS refundable deposit)

#### Admin Earns:
| Component | Amount | Calculation |
|-----------|--------|-------------|
| Service Fee (from Renter) | 82.50 GHS | 15% of rental |
| Platform Fee (from Owner) | 82.50 GHS | 15% of rental |
| **Total Platform Revenue** | **165.00 GHS** | `platformFee × 2` |
| Protection Plan | 50.00 GHS | Full amount |
| Insurance | 0.00 GHS | N/A |
| **TOTAL ADMIN REVENUE** | **215.00 GHS** | Platform Revenue + Protection |

#### Owner Receives:
| Component | Amount | Calculation |
|-----------|--------|-------------|
| Rental Amount | 550.00 GHS | Base rental |
| Driver Fee | 0.00 GHS | N/A |
| **Subtotal** | 550.00 GHS | |
| Platform Fee Deduction | -82.50 GHS | -15% |
| **NET OWNER EARNINGS** | **467.50 GHS** | `rental + driver - platformFee` |

#### Customer Pays:
| Component | Amount | Display Name |
|-----------|--------|--------------|
| Vehicle Rental | 550.00 GHS | Vehicle Rental |
| Protection Plan | 50.00 GHS | Protection Plan |
| Service Fee | 82.50 GHS | **Service Fee** (not "Platform Fee") |
| Security Deposit | 1000.00 GHS | Refundable Deposit |
| **TOTAL** | **1682.50 GHS** | |

## Frontend Implementation Guide

### 1. Update Revenue Cards/Widgets

```javascript
// Example React/Vue component
const AdminRevenueCard = ({ summary }) => {
  return (
    <div className="revenue-summary">
      <div className="revenue-item">
        <label>Total Revenue</label>
        <value>{summary.totalRevenue} GHS</value>
      </div>
      
      <div className="revenue-item highlight">
        <label>Admin Platform Revenue</label>
        <value>{summary.adminPlatformRevenue} GHS</value>
        <small>Service fee from renters + Platform fee from owners</small>
      </div>
      
      <div className="revenue-item">
        <label>Protection Plans</label>
        <value>{summary.protectionPlanRevenue} GHS</value>
      </div>
      
      <div className="revenue-item">
        <label>Insurance</label>
        <value>{summary.insuranceRevenue} GHS</value>
      </div>
      
      <div className="revenue-item owner">
        <label>Owner Payouts</label>
        <value>{summary.ownerRevenue} GHS</value>
        <small>Rental + Driver - Platform fee</small>
      </div>
    </div>
  );
};
```

### 2. Update Charts/Graphs

```javascript
// Revenue breakdown chart data
const revenueChartData = {
  labels: ['Platform Revenue', 'Protection', 'Insurance', 'Owner Payouts'],
  datasets: [{
    data: [
      summary.adminPlatformRevenue,  // Use adminPlatformRevenue (double fee)
      summary.protectionPlanRevenue,
      summary.insuranceRevenue,
      summary.ownerRevenue  // Shows correct net owner earnings
    ],
    backgroundColor: ['#667eea', '#2d7d5d', '#f39c12', '#3498db']
  }]
};
```

### 3. Time Series Charts

```javascript
// Daily/Monthly revenue trends
const timeSeriesData = {
  labels: response.byDay.map(d => d.date),
  datasets: [
    {
      label: 'Admin Platform Revenue',
      data: response.byDay.map(d => d.adminPlatformRevenue), // Use new field
      borderColor: '#667eea',
      fill: false
    },
    {
      label: 'Protection Revenue',
      data: response.byDay.map(d => d.protectionRevenue),
      borderColor: '#2d7d5d',
      fill: false
    },
    {
      label: 'Owner Payouts',
      data: response.byDay.map(d => d.ownerRevenue),
      borderColor: '#3498db',
      fill: false
    }
  ]
};
```

### 4. Calculate Total Admin Earnings

```javascript
const calculateTotalAdminEarnings = (summary) => {
  return summary.adminPlatformRevenue + 
         summary.protectionPlanRevenue + 
         summary.insuranceRevenue;
};

// For the example booking:
// Total Admin = 165.00 + 50.00 + 0.00 = 215.00 GHS ✓
```

## Key Points for Frontend

1. **Use `adminPlatformRevenue`** instead of `platformFeeRevenue` for admin earnings display
2. **`platformFeeRevenue`** is still available and shows the single fee value (for reference/debugging)
3. **`ownerRevenue`** now correctly shows net owner earnings (rental + driver - platform fee)
4. **Customer-facing labels**: Use "Service Fee" not "Platform Fee"
5. **Admin-facing labels**: Can use "Platform Revenue" or "Service Fee Revenue"

## Migration Notes

### Before (Old Calculation)
```json
{
  "platformFeeRevenue": 82.50,  // Only single fee
  "ownerRevenue": 500.00        // Incorrect (didn't subtract platform fee)
}
```

### After (New Calculation)
```json
{
  "platformFeeRevenue": 82.50,        // Reference value
  "adminPlatformRevenue": 165.00,     // Actual admin earnings (double)
  "ownerRevenue": 467.50              // Correct net owner earnings
}
```

## Testing

### Test Case 1: Booking RV-2026-327D91
- Rental: 550.00 GHS
- Protection: 50.00 GHS
- Platform Fee: 82.50 GHS

**Expected Results:**
- `adminPlatformRevenue`: 165.00 GHS ✓
- `protectionPlanRevenue`: 50.00 GHS ✓
- `ownerRevenue`: 467.50 GHS ✓
- Total Admin Earnings: 215.00 GHS ✓

### Verification Query
```sql
SELECT 
    "BookingReference",
    "RentalAmount",
    "DriverAmount",
    "ProtectionAmount",
    "PlatformFee",
    "RentalAmount" + COALESCE("DriverAmount", 0) - "PlatformFee" as "OwnerNetEarnings",
    "PlatformFee" * 2 as "AdminPlatformRevenue",
    "PlatformFee" * 2 + COALESCE("ProtectionAmount", 0) as "TotalAdminEarnings"
FROM "Bookings"
WHERE "Status" = 'completed'
ORDER BY "CreatedAt" DESC;
```

## Questions?

For any issues or clarifications, contact the backend team or check the implementation in:
- `Endpoints/AdminEndpoints.cs` (lines 1890-2150)
- Admin revenue analytics endpoint
