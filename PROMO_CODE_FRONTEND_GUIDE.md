# Promo Code Frontend Integration Guide
**Version 1.211 | January 2, 2026**

## ❓ Database Updates Required?

**NO DATABASE UPDATES NEEDED!** All recent changes are code-only:
- ✅ Email HTML rendering fixes (code only)
- ✅ Booking email timing fixes (code only)  
- ✅ Email placeholder data fixes (code only)
- ✅ Promo code logic already in database (v1.210)

---

## 🎯 Promo Code Types & Application Logic

### **1. Owner-Created Promo Codes**

**Type:** `OwnerVehicleDiscount`  
**AppliesTo:** `RentalAmount`  
**Effect:** Reduces **ONLY** the owner's daily rental earnings

#### How It Works:
```
Original Calculation:
- Daily Rate: GHS 200/day × 3 days = GHS 600
- Platform Fee: 5% of GHS 600 = GHS 30
- Owner Earns: GHS 600 - GHS 30 = GHS 570

With 20% Owner Promo (OWNER20):
- Discounted Daily Rate: GHS 200 × 0.80 = GHS 160/day × 3 days = GHS 480
- Platform Fee: 5% of GHS 480 = GHS 24
- Owner Earns: GHS 480 - GHS 24 = GHS 456 (REDUCED)
- Renter Pays: GHS 480 + GHS 24 = GHS 504 (LESS)
```

**Backend Logic (BookingEndpoints.cs line 755-763):**
```csharp
if (validation.PromoCode.PromoType == "OwnerVehicleDiscount")
{
    rentalAmount -= promoDiscountAmount;
    // Recalculate subtotal and fees based on reduced rental amount
    subtotal = rentalAmount + driverAmount + insuranceAmount + protectionAmount;
    platformFee = subtotal * (platformFeePercentage / 100);
    totalAmount = rentalAmount + depositAmount + driverAmount + insuranceAmount + protectionAmount + platformFee;
}
```

---

### **2. Admin-Created Promo Codes**

Admin codes can target different parts of the booking cost:

#### **A) Total Amount Discount** (Most Common)
**Type:** `Percentage` or `FixedAmount`  
**AppliesTo:** `TotalAmount`  
**Effect:** Reduces final amount renter pays (platform absorbs cost)

```
Booking Breakdown:
- Rental Amount: GHS 600
- Driver Fee: GHS 150
- Protection Plan: GHS 80
- Platform Fee: GHS 42
- TOTAL: GHS 872

With 20% Admin Promo (NEWYEAR20):
- Discount: 20% of GHS 872 = GHS 174.40
- Renter Pays: GHS 872 - GHS 174.40 = GHS 697.60
- Owner Still Earns: GHS 600 - platform fee (calculated on GHS 600)
```

**Backend Logic (BookingEndpoints.cs line 765-768):**
```csharp
else
{
    // Regular promo - reduce total amount (platform absorbs)
    totalAmount = validation.FinalAmount;
}
```

#### **B) Platform Fee Reduction**
**Type:** `Percentage` or `FixedAmount`  
**AppliesTo:** `PlatformFee`  
**Effect:** Reduces or eliminates platform commission

```
Example: Free listing for new owners (LISTFREE)
- Rental Amount: GHS 600
- Platform Fee: GHS 30 → GHS 0 (100% reduction)
- Owner Earns: GHS 600 (instead of GHS 570)
```

#### **C) Protection Plan Discount**
**Type:** `FreeAddon`  
**AppliesTo:** `ProtectionPlan`  
**Effect:** Free or discounted protection plan

```
Example: Free protection plan (FREEPROTECTION)
- Protection Plan Cost: GHS 80 → GHS 0
- Renter saves GHS 80 on total
```

#### **D) Commission Reduction** (Owner Rewards)
**Type:** `CommissionReduction`  
**AppliesTo:** `Commission`  
**Effect:** Reduces platform commission for owner

```
Example: 50% commission reduction (OWNER50OFF)
- Platform Fee: 5% → 2.5%
- Owner keeps more earnings
```

---

## 🔧 Frontend Implementation Guide

### **Step 1: Validate Promo Code**

**Endpoint:** `POST /api/v1/promo-codes/validate`

**Request Body:**
```json
{
  "code": "NEWYEAR20",
  "bookingAmount": 872.00,
  "vehicleId": "abc123...",
  "categoryId": "def456...",
  "cityId": "ghi789...",
  "tripDuration": 3
}
```

**Response (Success):**
```json
{
  "isValid": true,
  "errorMessage": null,
  "promoCode": {
    "id": "...",
    "code": "NEWYEAR20",
    "description": "New Year 20% off total booking",
    "promoType": "Percentage",
    "discountValue": 20.00,
    "targetUserType": "Renter",
    "appliesTo": "TotalAmount",
    "minimumBookingAmount": 100.00,
    "maximumDiscountAmount": 200.00,
    "validFrom": "2026-01-01T00:00:00Z",
    "validUntil": "2026-12-31T23:59:59Z",
    "createdBy": "admin"
  },
  "originalAmount": 872.00,
  "discountAmount": 174.40,
  "finalAmount": 697.60,
  "appliesTo": "TotalAmount"
}
```

**Response (Error):**
```json
{
  "isValid": false,
  "errorMessage": "This promo code is only for renters",
  "promoCode": null,
  "originalAmount": 872.00,
  "discountAmount": 0,
  "finalAmount": 872.00,
  "appliesTo": ""
}
```

---

### **Step 2: Display Promo Code Impact**

Based on `appliesTo` field, show different UI messages:

#### **Frontend Logic:**
```typescript
interface PromoCodeValidation {
  isValid: boolean;
  errorMessage?: string;
  promoCode?: {
    code: string;
    description: string;
    promoType: 'Percentage' | 'FixedAmount' | 'FreeAddon' | 'CommissionReduction' | 'OwnerVehicleDiscount';
    appliesTo: 'TotalAmount' | 'PlatformFee' | 'ProtectionPlan' | 'RentalAmount' | 'Commission';
    discountValue: number;
  };
  originalAmount: number;
  discountAmount: number;
  finalAmount: number;
  appliesTo: string;
}

function displayPromoDiscount(validation: PromoCodeValidation) {
  if (!validation.isValid) {
    return `❌ ${validation.errorMessage}`;
  }

  const { promoCode, discountAmount, finalAmount } = validation;
  
  switch (promoCode.appliesTo) {
    case 'TotalAmount':
      return `✅ ${promoCode.code} applied! Save GHS ${discountAmount.toFixed(2)} on total. New total: GHS ${finalAmount.toFixed(2)}`;
    
    case 'RentalAmount':
      return `✅ Owner discount applied! Daily rate reduced. New rental amount: GHS ${finalAmount.toFixed(2)}`;
    
    case 'ProtectionPlan':
      return `✅ ${promoCode.description}! Protection plan ${promoCode.promoType === 'FreeAddon' ? 'FREE' : `GHS ${discountAmount.toFixed(2)} off`}`;
    
    case 'PlatformFee':
      return `✅ Platform fee reduced by GHS ${discountAmount.toFixed(2)}`;
    
    case 'Commission':
      return `✅ Commission reduction of ${promoCode.discountValue}% applied (for owners)`;
    
    default:
      return `✅ Promo code applied! Save GHS ${discountAmount.toFixed(2)}`;
  }
}
```

---

### **Step 3: Apply Promo During Booking**

**Endpoint:** `POST /api/v1/bookings`

**Request Body (with promo):**
```json
{
  "vehicleId": "abc123...",
  "pickupDateTime": "2026-01-05T10:00:00Z",
  "returnDateTime": "2026-01-08T10:00:00Z",
  "pickupLocation": {
    "type": "city",
    "cityId": "...",
    "cityName": "Accra"
  },
  "returnLocation": { ... },
  "withDriver": false,
  "paymentMethod": "card",
  "promoCode": "NEWYEAR20",  // ← Include validated code
  "insuranceAccepted": true,
  "protectionPlanId": "..."
}
```

**Backend automatically:**
1. ✅ Re-validates promo code
2. ✅ Calculates correct discount based on `appliesTo`
3. ✅ Applies discount to correct amount (rental vs total)
4. ✅ Stores `promoCodeId` and `promoDiscountAmount` in booking
5. ✅ Records usage in `PromoCodeUsage` table

---

## 📊 Owner Promo Code Creation (Frontend)

**Endpoint:** `POST /api/v1/owner/promo-codes/vehicle`

**Request Body:**
```json
{
  "code": "MYCAR50",
  "description": "50% off my Honda Civic - limited time!",
  "vehicleId": "abc123...",
  "promoType": "Percentage",
  "discountValue": 50.0,
  "appliesTo": "RentalAmount",  // ← MUST be RentalAmount for owner codes
  "minimumBookingAmount": 200.0,
  "maximumDiscountAmount": 500.0,
  "validFrom": "2026-01-03T00:00:00Z",
  "validUntil": "2026-01-31T23:59:59Z",
  "maxTotalUses": 100,
  "maxUsesPerUser": 1
}
```

**Validation Rules for Owner Codes:**
- ✅ `appliesTo` MUST be `"RentalAmount"`
- ✅ `targetUserType` automatically set to `"Renter"`
- ✅ `createdBy` automatically set to `"owner"`
- ✅ `vehicleId` MUST belong to the owner
- ❌ Cannot set `appliesTo` to `PlatformFee`, `Commission`, or `TotalAmount` (admin only)

---

## 🎨 UI/UX Recommendations

### **1. Promo Code Input Field**
```typescript
// Show validation in real-time
<input 
  type="text" 
  placeholder="Enter promo code"
  onChange={handlePromoCodeChange}
  className={isValid ? 'success' : hasError ? 'error' : ''}
/>
<button onClick={validatePromoCode}>Apply</button>

{isValid && (
  <div className="promo-success">
    ✅ {displayPromoDiscount(validation)}
  </div>
)}

{hasError && (
  <div className="promo-error">
    ❌ {validation.errorMessage}
  </div>
)}
```

### **2. Booking Summary with Promo**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━
BOOKING SUMMARY
━━━━━━━━━━━━━━━━━━━━━━━━━━
Rental Amount        GHS 600.00
Driver Fee          GHS 150.00
Protection Plan      GHS  80.00
Platform Fee         GHS  42.00
                    ──────────
Subtotal            GHS 872.00

Promo (NEWYEAR20)  -GHS 174.40 ✨
                    ──────────
TOTAL               GHS 697.60
━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### **3. Owner Dashboard - Create Promo**
```
┌─────────────────────────────────────┐
│ Create Vehicle Discount             │
├─────────────────────────────────────┤
│ Vehicle: Honda Civic (GR-222-22)    │
│ Current Daily Rate: GHS 200         │
│                                     │
│ Discount Type: [Percentage ▼]      │
│ Discount Value: [20] %              │
│                                     │
│ ⚠️ This will reduce YOUR earnings   │
│    New rate: GHS 160/day            │
│    You earn less, renters pay less  │
│                                     │
│ [Create Discount Code]              │
└─────────────────────────────────────┘
```

---

## 🔍 Determining Code Type (Owner vs Admin)

### **Method 1: Check `createdBy` field**
```typescript
if (promoCode.createdBy === 'owner') {
  // Owner-created code
  // Only affects rental amount
  // Owner earns less, renter pays less
} else if (promoCode.createdBy === 'admin') {
  // Admin code
  // Can affect platform fee, total, protection, etc.
  // Platform may absorb cost
}
```

### **Method 2: Check `appliesTo` field**
```typescript
if (promoCode.appliesTo === 'RentalAmount') {
  // Owner vehicle discount
  showMessage('💡 Owner is offering a special discount!');
} else if (promoCode.appliesTo === 'TotalAmount') {
  // Platform-wide discount
  showMessage('🎉 Platform discount applied!');
} else if (promoCode.appliesTo === 'Commission') {
  // Owner reward (reduces their commission)
  showMessage('🎁 Reduced platform fee for this owner');
}
```

### **Method 3: Check `vehicleId` field**
```typescript
if (promoCode.vehicleId !== null) {
  // Vehicle-specific code (always owner-created)
  showMessage(`✅ Valid for ${vehicle.make} ${vehicle.model} only`);
} else {
  // Platform-wide code (admin)
  showMessage('✅ Valid for all vehicles');
}
```

---

## 📋 Error Handling

### **Common Validation Errors:**

| Error Message | Reason | Solution |
|--------------|--------|----------|
| "Promo code not found" | Code doesn't exist | Check spelling |
| "This promo code has expired" | Past `validUntil` date | Use a different code |
| "This promo code is only for renters" | User role mismatch | Login as renter |
| "This promo code only applies to Landcruiser vehicles" | Category restriction | Choose correct vehicle |
| "Minimum booking amount of GHS 500 required" | Booking too small | Increase booking duration |
| "You have already used this promo code 1 time(s)" | Exceeded `maxUsesPerUser` | Code used up |
| "This promo code has reached its maximum usage limit" | Exceeded `maxTotalUses` | Code expired |

---

## 🚀 Quick Reference

### **Renter Flow:**
1. Add vehicle to cart / start booking
2. Calculate booking total (rental + fees + protection + driver)
3. Enter promo code in input field
4. Click "Apply Code"
5. Call `POST /api/v1/promo-codes/validate`
6. Display discount amount and new total
7. Submit booking with `promoCode` field
8. Backend applies discount automatically

### **Owner Flow:**
1. Go to "My Vehicles" → Select vehicle
2. Click "Create Discount Code"
3. Choose discount percentage (e.g., 20%)
4. Set validity dates and usage limits
5. Submit `POST /api/v1/owner/promo-codes/vehicle`
6. Share code with potential renters
7. Track usage in dashboard
8. Receive reduced earnings on discounted bookings

---

## ✅ Testing Checklist

- [ ] Admin promo reduces total amount (platform absorbs)
- [ ] Owner promo reduces rental amount (owner earns less)
- [ ] Platform fee calculated on correct base amount
- [ ] Owner cannot create codes that reduce platform fee
- [ ] Referral codes work for both parties
- [ ] Maximum discount cap enforced
- [ ] Usage limits enforced (per user and total)
- [ ] Expired codes rejected
- [ ] Category/city restrictions work
- [ ] First-time user restriction works

---

## 📞 Support

For issues or questions:
- **Email:** support@ryverental.com
- **Documentation:** /docs/promo-codes
- **API Testing:** Use Postman with bearer token authentication

---

**Last Updated:** January 2, 2026  
**API Version:** 1.211  
**Author:** Ryve Rental Development Team
