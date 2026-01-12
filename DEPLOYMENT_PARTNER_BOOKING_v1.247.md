# Partner Booking System - Deployment Guide v1.247

## 🚀 Overview

Complete partner integration system for third-party bookings (hotels, OTAs, travel agencies).

## ✨ Features Added

### 1. **Partner Booking Tracking**
- Separate payment channel tracking (`direct` vs `partner`)
- Partner settlement management (who owes who)
- Auto-calculated commissions
- Settlement due dates and status tracking

### 2. **Financial Accountability**
- Partner settlements tracked separately from bookings
- Admin can see pending partner payments
- Revenue metrics exclude unpaid partner settlements
- Owner payouts only from settled partner bookings

### 3. **Auto-Cancellation Protection**
- Partner bookings exempt from 4-hour auto-cancellation
- Customer paid partner = booking confirmed immediately
- No timeout for partner settlements

### 4. **Partner-Specific Email Templates**
- Different email flows for partner bookings
- Partner name mentioned in communications
- Custom support contact info

## 📋 Pre-Deployment Checklist

### Database Migration Required ✅

Run this migration BEFORE deploying code:

```bash
az postgres flexible-server execute \
  --name "ryve-postgres-new" \
  --admin-user "ryveadmin" \
  --admin-password "RyveDb@2025!Secure#123" \
  --database-name "ghanarentaldb" \
  --file-path "add-partner-booking-tracking.sql"
```

**What it does:**
- Adds `PaymentChannel`, `PartnerSettlementStatus`, `IntegrationPartnerId` to Bookings
- Creates `PartnerSettlements` table
- Adds `CommissionPercent`, `SettlementTermDays`, `AutoConfirmBookings` to IntegrationPartners
- Updates existing bookings to `PaymentChannel = 'direct'`

### Email Templates Required 📧

After deployment, create these email templates via admin panel:

1. **booking_confirmed_partner** - For customers who booked through partners
2. **owner_new_booking_partner** - For owners receiving partner bookings

See `PARTNER_EMAIL_TEMPLATES.md` for template content.

## 🔧 Changes Made

### Models Updated
- ✅ `Booking.cs` - Added `PaymentChannel`, `PartnerSettlementStatus`, `IntegrationPartnerId`
- ✅ `IntegrationPartner.cs` - Added `CommissionPercent`, `SettlementTermDays`, `AutoConfirmBookings`
- ✅ `PartnerSettlement.cs` - NEW model for settlement tracking

### Endpoints Updated
- ✅ `WebhookEndpoints.cs` - Partner booking creation now creates settlements
- ✅ `PartnerSettlementEndpoints.cs` - NEW admin endpoints for settlement management

### Services Updated
- ✅ `UnpaidBookingCancellationService.cs` - Excludes partner bookings
- ✅ `NotificationService.cs` - Partner-specific email handling
- ✅ `AdminEndpoints.cs` - Revenue metrics include partner settlement tracking

### Configuration
- ✅ `AppDbContext.cs` - Added `PartnerSettlements` DbSet
- ✅ `Program.cs` - Registered `PartnerSettlementEndpoints`

## 📡 New API Endpoints

### Partner Settlement Management (Admin Only)

```
GET  /api/v1/admin/partner-settlements
     ?status=pending&partnerId={guid}&overdue=true&page=1&pageSize=50
     
GET  /api/v1/admin/partner-settlements/{settlementId}

GET  /api/v1/admin/partners/{partnerId}/settlements
     ?status=pending&page=1&pageSize=50
     
POST /api/v1/admin/partner-settlements/{settlementId}/mark-paid
     Body: { 
       "paymentReference": "BANK-TXN-12345",
       "paymentMethod": "bank_transfer",
       "notes": "Received via wire transfer"
     }
     
GET  /api/v1/admin/partner-settlements/summary
     ?from=2026-01-01&to=2026-01-31
```

### Updated Partner Booking Creation

```
POST /api/v1/partner/bookings
Headers: X-API-Key: partner_api_key
Body: {
  "vehicleId": "guid",
  "pickupDateTime": "2026-01-15T10:00:00Z",
  "returnDateTime": "2026-01-20T10:00:00Z",
  "withDriver": false,
  "renterEmail": "customer@example.com",
  "renterPhone": "+233242841000",
  "renterName": "John Doe",
  "paymentMethod": "card"
}

Response: {
  "id": "booking-guid",
  "bookingReference": "RV-2026-ABC12345",
  "status": "confirmed",
  "paymentStatus": "paid",
  "totalAmount": 500.00,
  "partner": {
    "commissionPercent": 15.00,
    "commissionAmount": 75.00,
    "settlementAmount": 425.00,
    "settlementDueDate": "2026-02-14T10:00:00Z"
  }
}
```

## 💡 How Partner Payments Work

### Immediate (When Booking Created):
1. Customer pays partner → $100
2. Partner calls API with booking details
3. Backend:
   - ✅ Creates booking (status: confirmed, paymentStatus: paid)
   - ✅ Calculates commission (15% = $15)
   - ✅ Records settlement owed ($85)
   - ✅ Marks settlement as "pending"
   - ✅ Sends confirmation emails

### Monthly Settlement:
1. Admin views pending settlements
2. Partner sends payment ($85)
3. Admin marks settlement as "paid" via API
4. Owner payout can now be processed

## 📊 Admin Dashboard Views

### Revenue Metrics Changes

`GET /api/v1/admin/metrics/revenue` now includes:

```json
{
  "summary": {
    "totalRevenue": 50000.00,
    "ownerRevenue": 35000.00,
    "adminPlatformRevenue": 10000.00,
    "pendingPartnerRevenue": 5000.00  // NEW: Money owed by partners
  }
}
```

### Partner Settlement Summary

```json
{
  "summary": {
    "totalBookings": 100,
    "totalAmount": 10000.00,
    "totalCommission": 1500.00,
    "totalSettlement": 8500.00,
    "pendingAmount": 3200.00,  // Partners owe us
    "paidAmount": 5300.00,     // Already settled
    "overdueAmount": 800.00,   // Past due date
    "overdueCount": 8
  },
  "byPartner": [...]
}
```

## ⚙️ Partner Configuration

Set commission and payment terms per partner:

```json
{
  "name": "Hotel ABC",
  "type": "hotel",
  "commissionPercent": 15.00,      // Partner keeps 15%
  "settlementTermDays": 30,        // Payment due in 30 days
  "autoConfirmBookings": true      // Auto-confirm or require approval
}
```

## 🔐 Security Notes

- Partner bookings marked as "paid" immediately (customer paid partner)
- Settlement status tracked separately (partner paid us)
- Revenue metrics only count settled partner bookings
- Owner payouts protected until partner settles

## 🐛 Testing Checklist

After deployment:

1. ✅ Create integration partner via admin panel
2. ✅ Test partner booking creation via API
3. ✅ Verify booking shows as "confirmed" and "paid"
4. ✅ Check settlement record created
5. ✅ Verify booking NOT auto-cancelled after 4 hours
6. ✅ Check customer email (should mention partner)
7. ✅ Check owner email (should mention pending settlement)
8. ✅ Verify revenue metrics show pending partner amount
9. ✅ Mark settlement as paid
10. ✅ Verify revenue metrics updated

## 📝 Configuration Settings (Optional)

These can be configured via admin config endpoint if needed:

- `Partner:DefaultCommissionPercent` - Default commission (15%)
- `Partner:DefaultSettlementTermDays` - Default payment terms (30 days)
- `Partner:RequireManualApproval` - Require admin approval for partner bookings

## 🚨 Important Notes

- **LIVE SYSTEM**: Changes are minimal and isolated to partner bookings only
- **Backward Compatible**: Existing direct bookings unaffected
- **No Breaking Changes**: All existing APIs work as before
- **Email Templates**: Partner templates fallback to regular templates if not created

## 📞 Support

For issues during deployment:
- Check database migration completed successfully
- Verify email templates created
- Check partner API keys are active
- Review settlement status logic

---

**Version:** 1.247  
**Date:** January 11, 2026  
**Migration Required:** YES - Run `add-partner-booking-tracking.sql`  
**Breaking Changes:** None  
**Rollback Plan:** Database migration can be reverted if needed
