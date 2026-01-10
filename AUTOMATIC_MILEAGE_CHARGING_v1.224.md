# Automatic Mileage Overage Charging System
**Version:** 1.224  
**Date:** January 5, 2026  
**Status:** Production Ready

## Overview
The automatic mileage overage charging system calculates and applies extra kilometer charges when a vehicle is returned, deducting fees from the security deposit and updating all booking financials in real-time.

---

## How It Works

### Vehicle Configuration
Each vehicle can be configured with:
- **`IncludedKilometers`**: Free km allowance for the rental (e.g., 100 km)
- **`PricePerExtraKm`**: Rate charged per km over the limit (e.g., 1.00 GHS/km)
- **`MileageChargingEnabled`**: Toggle to enable/disable (default: true)

### Automatic Flow (on Vehicle Return)

When an owner completes a trip via `POST /api/v1/bookings/{id}/complete-trip`:

#### 1. **Odometer Calculation**
```
actualKmDriven = PostTripOdometer - PreTripOdometer
```

#### 2. **Overage Detection**
```
IF actualKmDriven > vehicle.IncludedKilometers:
    overageKm = actualKmDriven - IncludedKilometers
    overageCharge = overageKm × PricePerExtraKm
```

#### 3. **Automatic Actions** (All Happen Instantly)

##### A. Create Booking Charge Record
- **Type**: `mileage_overage`
- **Status**: `approved` (auto-approved, system-generated)
- **Amount**: Calculated overage charge
- **Evidence**: Post-trip photos attached

##### B. Update Booking Financials
```csharp
booking.RentalAmount += overageCharge
booking.PlatformFee += (overageCharge × platformFeePercentage)
booking.TotalAmount += overageCharge
```

##### C. Deduct from Security Deposit

**Scenario 1: Full deposit coverage**
```
IF deposit ≥ overageCharge:
    deposit -= overageCharge
    // Renter owes nothing extra
```

**Scenario 2: Partial deposit coverage**
```
IF 0 < deposit < overageCharge:
    remainingCharge = overageCharge - deposit
    deposit = 0
    // Create payment transaction for remainingCharge
```

**Scenario 3: No deposit**
```
IF deposit = 0:
    // Create payment transaction for full overageCharge
```

##### D. Adjust Deposit Refund
```
IF deposit > 0:
    Create DepositRefund with reduced amount
    Add note: "Mileage overage of X GHS deducted"
ELSE:
    No refund created (deposit fully consumed)
```

##### E. Notify Renter
- Email/WhatsApp notification with charge breakdown
- Included in booking completion notification

---

## Example Scenario

### Vehicle Setup
- **Included km**: 100
- **Rate per extra km**: 1.00 GHS
- **Original deposit**: 200 GHS
- **Platform fee**: 15%

### Trip Details
- **Pre-trip odometer**: 50,000 km
- **Post-trip odometer**: 50,110 km
- **Actual distance**: 110 km
- **Overage**: 10 km

### Automatic Calculations

#### 1. Mileage Overage Charge
```
10 km × 1.00 GHS = 10.00 GHS
```

#### 2. Platform Fee on Overage
```
10.00 GHS × 15% = 1.50 GHS
```

#### 3. Updated Booking Totals
```
Original Rental Amount: 150.00 GHS
+ Mileage Overage:       10.00 GHS
= New Rental Amount:    160.00 GHS

Original Platform Fee:    22.50 GHS
+ Overage Platform Fee:    1.50 GHS
= New Platform Fee:       24.00 GHS

Original Total:          172.50 GHS
+ Overage Charge:         10.00 GHS
= New Total:             182.50 GHS
```

#### 4. Deposit Deduction
```
Original Deposit:        200.00 GHS
- Mileage Overage:       -10.00 GHS
= Refundable Deposit:    190.00 GHS
```

#### 5. Financial Breakdown

**Owner Receives:**
```
Rental Amount:           160.00 GHS (includes overage)
- Platform Fee:          -24.00 GHS
= Owner Payout:          136.00 GHS
```

**Platform Receives:**
```
Platform Fee:             24.00 GHS
(15% of 160.00 GHS, including overage)
```

**Renter Pays:**
```
Original Booking Total:  172.50 GHS (paid at booking)
+ Overage from Deposit:   10.00 GHS (auto-deducted)
= Total Paid:            182.50 GHS

Deposit Refund:          190.00 GHS
```

---

## API Response Example

### Request
```http
POST /api/v1/bookings/abc123/complete-trip
Authorization: Bearer {owner_token}
Content-Type: application/json

{
  "odometer": 50110,
  "fuelLevel": 0.8,
  "notes": "Vehicle returned in good condition",
  "photoUrls": ["https://..."]
}
```

### Response
```json
{
  "bookingId": "abc-123-def-456",
  "status": "completed",
  "preTripOdometer": 50000,
  "postTripOdometer": 50110,
  "distanceTraveled": 110,
  "completedAt": "2026-01-05T18:30:00Z",
  
  "mileageCharge": {
    "amount": 10.00,
    "actualKmDriven": 110,
    "allowedKm": 100,
    "overageKm": 10,
    "ratePerKm": 1.00,
    "deductedFromDeposit": 10.00,
    "remainingDeposit": 190.00,
    "message": "Mileage overage charge of GHS 10.00 applied (10 km @ 1.00/km). Deducted from security deposit."
  },
  
  "updatedBookingTotals": {
    "rentalAmount": 160.00,
    "platformFee": 24.00,
    "totalAmount": 182.50,
    "depositAmount": 190.00
  },
  
  "message": "Trip completed. Mileage overage charge of GHS 10.00 applied."
}
```

---

## Database Changes

### New `BookingCharge` Record
```sql
INSERT INTO "BookingCharges" (
  "BookingId",
  "ChargeTypeId", -- mileage_overage
  "Amount",
  "Status",
  "Label",
  "Notes",
  "CreatedAt",
  "SettledAt"
) VALUES (
  'abc-123-def-456',
  'charge-type-guid',
  10.00,
  'approved',
  'Mileage Overage (10 km)',
  'Automatic charge: 110 km driven, 100 km included, 10 km overage @ 1.00/km',
  NOW(),
  NOW()
);
```

### Updated `Booking` Record
```sql
UPDATE "Bookings" SET
  "RentalAmount" = 160.00,      -- was 150.00
  "PlatformFee" = 24.00,         -- was 22.50
  "TotalAmount" = 182.50,        -- was 172.50
  "DepositAmount" = 190.00,      -- was 200.00
  "Status" = 'completed',
  "PostTripOdometer" = 50110,
  "PostTripRecordedAt" = NOW(),
  "UpdatedAt" = NOW()
WHERE "Id" = 'abc-123-def-456';
```

### New `DepositRefund` Record
```sql
INSERT INTO "DepositRefunds" (
  "BookingId",
  "Amount",
  "Status",
  "Notes",
  "DueDate"
) VALUES (
  'abc-123-def-456',
  190.00,  -- Reduced by 10.00 overage
  'pending',
  'Auto-created deposit refund after trip completion\nMileage overage charge of GHS 10.00 deducted from deposit',
  NOW() + INTERVAL '2 days'
);
```

---

## Edge Cases Handled

### 1. **No Overage**
If `actualKm ≤ includedKm`:
- No mileage charge created
- Full deposit refunded
- Normal completion flow

### 2. **Mileage Charging Disabled**
If `vehicle.MileageChargingEnabled = false`:
- Overage calculation skipped
- Full deposit refunded
- Charge must be manually added by admin if needed

### 3. **Insufficient Deposit**

**Example: Overage = 50 GHS, Deposit = 30 GHS**

```json
{
  "mileageCharge": {
    "amount": 50.00,
    "deductedFromDeposit": 30.00,
    "remainingDeposit": 0.00,
    "message": "Partial deposit deduction. Remaining 20.00 GHS balance due."
  },
  "additionalCharge": {
    "transactionId": "txn-123",
    "type": "payment",
    "amount": 20.00,
    "status": "pending",
    "reference": "MILEAGE-RV-2026-001234-20260105",
    "message": "Additional mileage charge of 20.00 GHS pending payment (deposit insufficient)"
  }
}
```

### 4. **No Deposit**
If `booking.DepositAmount = 0`:
- Full overage charge creates payment transaction
- Status: `pending`
- Renter must pay before final settlement

### 5. **Missing Odometer Data**
If `PreTripOdometer` or `PostTripOdometer` is null:
- No automatic charge
- Admin can manually add charge using existing endpoint:
  `POST /api/v1/admin/bookings/{id}/mileage-charge`

---

## Financial Reconciliation

### Owner Settlement
```
Gross Rental (with overage):  160.00 GHS
- Platform Fee (15%):         -24.00 GHS
= Net Payout:                 136.00 GHS
```

### Platform Revenue
```
Platform Fee:                  24.00 GHS
(Includes 1.50 GHS from overage)
```

### Renter Statement
```
Booking Total:                172.50 GHS (paid)
Mileage Overage:               10.00 GHS (from deposit)
Total Cost:                   182.50 GHS

Deposit Paid:                 200.00 GHS
- Overage Charge:             -10.00 GHS
= Refund Due:                 190.00 GHS
```

---

## Notifications

### Renter Notification (Email/WhatsApp)
```
🎉 Rental Completed - Ryve Rental

Booking Reference: RV-2026-001234
Vehicle: 2022 Toyota Camry
Return Date: Jan 5, 2026 6:30 PM

📊 Trip Summary:
Distance Traveled: 110 km
Allowed: 100 km
Overage: 10 km @ GHS 1.00/km

💰 Charges Applied:
Mileage Overage: GHS 10.00 (deducted from deposit)

🔄 Deposit Refund:
Original Deposit: GHS 200.00
- Mileage Charge: -GHS 10.00
Refund Amount: GHS 190.00
Processing within 2 business days

Thank you for choosing Ryve Rental!
```

---

## Admin Manual Override

If automatic calculation needs adjustment, admin can still use:

```http
POST /api/v1/admin/bookings/{id}/mileage-charge
{
  "overrideType": "overage",
  "actualDrivenKm": 110,
  "overrideRatePerKm": 1.50,
  "reason": "Special rate adjustment",
  "deductFromDeposit": true
}
```

---

## Configuration

### Global Settings (Optional)
```sql
-- Platform fee percentage
INSERT INTO "GlobalSettings" ("Key", "Value") 
VALUES ('PlatformFeePercentage', '15');

-- Mileage charging settings
INSERT INTO "GlobalSettings" ("Key", "ValueJson") 
VALUES ('MileageCharging', '{
  "enabled": true,
  "tamperingPenaltyAmount": 500.00,
  "missingMileagePenaltyAmount": 200.00
}');
```

### Vehicle-Level Override
```csharp
// Set vehicle-specific mileage terms
vehicle.IncludedKilometers = 150;      // 150 km free
vehicle.PricePerExtraKm = 0.75m;        // 0.75 GHS per extra km
vehicle.MileageChargingEnabled = true;
```

---

## Testing Checklist

- [x] ✅ Vehicle with overage → Charge created and applied
- [x] ✅ Vehicle within limit → No charge, full deposit refund
- [x] ✅ Disabled mileage charging → No automatic charge
- [x] ✅ Sufficient deposit → Full deduction from deposit
- [x] ✅ Partial deposit → Deduct what's available, charge remainder
- [x] ✅ No deposit → Create payment transaction for full amount
- [x] ✅ Platform fee recalculated correctly
- [x] ✅ Owner payout includes overage revenue
- [x] ✅ Deposit refund shows reduced amount
- [x] ✅ Booking totals updated in database
- [x] ✅ Response includes detailed breakdown
- [x] ✅ Notifications sent to renter
- [x] ✅ Admin can view BookingCharges

---

## Migration Notes

### Existing Bookings
- Bookings completed before v1.224 are unaffected
- No retroactive charges applied
- Deposit refunds remain as originally calculated

### Backward Compatibility
- ✅ API response structure extended (new fields optional)
- ✅ Frontend can ignore `mileageCharge` field if not needed
- ✅ Existing deposit refund logic preserved
- ✅ Manual admin override still available

---

## Troubleshooting

### Issue: No charge applied despite overage
**Check:**
1. `vehicle.MileageChargingEnabled` = true
2. `vehicle.IncludedKilometers` > 0
3. `vehicle.PricePerExtraKm` > 0
4. Both odometer readings recorded

### Issue: Wrong charge amount
**Verify:**
1. Odometer readings are correct
2. `PricePerExtraKm` is correct rate
3. No admin manual override already applied

### Issue: Deposit not deducted
**Review:**
1. Booking charge created with `status='approved'`
2. Deposit amount before completion
3. Check `PaymentTransactions` for pending balance

---

## Future Enhancements

- [ ] Support for per-day km allowance (e.g., 100 km/day × rental days)
- [ ] Progressive pricing tiers (first 20 km @ 1 GHS, next 50 @ 0.75 GHS)
- [ ] Automatic odometer validation via GPS/telematics
- [ ] Bulk overage charge processing
- [ ] Renter dispute flow for incorrect charges

---

**Version:** 1.224  
**Author:** Ghana Hybrid Rental API Team  
**Last Updated:** January 5, 2026
