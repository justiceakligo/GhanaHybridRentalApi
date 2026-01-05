# Stripe Payment Bug Fix - v1.221

## 🐛 Bug Description
**CRITICAL:** Stripe payments were charging users LESS than the displayed booking total, causing revenue loss.

### Example Case
**Expected Payment:**
- Total shown to user: **GHS 94.00**
- Exchange rate: 1 USD = 11.5 GHS
- Expected Stripe charge: 94 ÷ 11.5 = **$8.17 USD** ✓

**Actual Payment (WRONG):**
- Amount charged by Stripe: **$6.06 USD**
- Equivalent in GHS: 6.06 × 11.5 = **69.69 GHS** ❌
- **Missing: 24.31 GHS per transaction!**

---

## 🔍 Root Cause
The bug was in [PaymentEndpoints.cs](Endpoints/PaymentEndpoints.cs#L761):

```csharp
// WRONG - Default rate was 15.5 instead of 11.5
decimal usdToGhsRate = 15.5m; // Default fallback
```

When the `GlobalSettings` table didn't have the `usd_to_ghs_rate` key configured, the code used this hardcoded default rate of **15.5** instead of the actual rate of **11.5**.

### Impact Calculation
With wrong rate (15.5):
```
94 GHS ÷ 15.5 = $6.06 USD ❌
```

With correct rate (11.5):
```
94 GHS ÷ 11.5 = $8.17 USD ✓
```

**Result:** Users were undercharged by ~26% on every Stripe transaction!

---

## ✅ Fix Applied
Changed the default exchange rate from **15.5** to **11.5** to match the actual USD to GHS conversion rate.

### Code Changes
**File:** [Endpoints/PaymentEndpoints.cs](Endpoints/PaymentEndpoints.cs#L761)

**Before:**
```csharp
decimal usdToGhsRate = 15.5m; // Default fallback
```

**After:**
```csharp
decimal usdToGhsRate = 11.5m; // Default fallback (1 USD = 11.5 GHS)
```

### Additional Improvements
Added debug logging to help troubleshoot future payment issues:

```csharp
Console.WriteLine($"💰 Stripe Payment Conversion: {booking.TotalAmount} GHS ÷ {usdToGhsRate} = ${amountToCharge} USD");
```

This logs the exact conversion calculation before creating the Stripe payment intent.

---

## 🎯 Verification Steps

### 1. Test Booking Creation
Create a test booking with known amounts:
- Vehicle (1 day): GHS 30.00
- Protection plan: GHS 50.00
- Platform fee: GHS 4.00
- Security deposit: GHS 10.00
- **Total: GHS 94.00**

### 2. Initialize Stripe Payment
```http
POST /api/v1/bookings/{bookingId}/payments/initialize
Content-Type: application/json

{
  "method": "stripe",
  "customerEmail": "test@example.com",
  "customerName": "Test User"
}
```

### 3. Check Server Logs
Look for the conversion log:
```
💰 Stripe Payment Conversion: 94 GHS ÷ 11.5 = $8.17 USD
```

### 4. Verify Stripe Dashboard
- Login to Stripe dashboard
- Check the payment intent amount
- Should show: **$8.17 USD** (not $6.06)

---

## 📊 Impact Assessment

### Before Fix (Using 15.5 rate)
| Booking Total (GHS) | Charged (USD) | Should Charge (USD) | Lost Revenue (GHS) |
|---------------------|---------------|---------------------|-------------------|
| 94.00              | $6.06         | $8.17               | 24.31            |
| 200.00             | $12.90        | $17.39              | 51.61            |
| 500.00             | $32.26        | $43.48              | 129.03           |

### After Fix (Using 11.5 rate)
All Stripe payments now charge the correct amount matching the total shown to users.

---

## 🔧 Long-term Solution
To prevent this issue in the future, ensure the `GlobalSettings` table has the exchange rate properly configured:

```sql
INSERT INTO "GlobalSettings" ("Id", "Key", "ValueJson", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (
    gen_random_uuid(),
    'usd_to_ghs_rate',
    '11.5',
    'USD to GHS exchange rate for Stripe payments',
    'Payment',
    true,
    NOW(),
    NOW()
);
```

This way, administrators can update the exchange rate without code changes when the market rate fluctuates.

---

## 📝 Deployment Notes
**Version:** v1.221  
**Deployment Date:** January 5, 2026  
**Priority:** CRITICAL - Revenue Impact  
**Breaking Changes:** None  
**Database Changes:** None (recommended to add GlobalSettings entry)

---

## 🚨 Post-Deployment Actions

1. ✅ Deploy v1.221 to production
2. ⚠️ **Add exchange rate to GlobalSettings table** (see SQL above)
3. ⚠️ Create test booking and verify Stripe charges correct amount
4. ⚠️ Monitor server logs for payment conversion calculations
5. ⚠️ Check Stripe dashboard for next few payments
6. ⚠️ Consider reconciliation: Review recent Stripe payments to identify undercharged transactions

---

## 💡 Related Files
- [Endpoints/PaymentEndpoints.cs](Endpoints/PaymentEndpoints.cs) - Payment initialization
- [Services/StripePaymentService.cs](Services/StripePaymentService.cs) - Stripe integration
- [deploy-v1.221.ps1](deploy-v1.221.ps1) - Deployment script

