# v1.210 Deployment Summary

## Deployment Details

**Version:** v1.210  
**Date:** January 2, 2026  
**Status:** ✅ Successfully Deployed  
**New IP Address:** 172.212.2.194  
**FQDN:** ghana-rental-api.eastus.azurecontainer.io  

---

## What's New in v1.210

### 🎯 Comprehensive Promo Code System

This release introduces a complete 3-phase promo code system for the platform:

#### Phase 1: Renter Discounts
- **Percentage Discounts:** Apply % off total booking amount
- **Fixed Amount Discounts:** Flat GHS discount
- **Free Protection Plan:** Complimentary protection coverage
- **Restrictions:** Category, city, first-time users only, minimum booking amount
- **Usage Limits:** Max total uses, max uses per user

#### Phase 2: Owner Incentives
- **Commission Reduction:** Reduce platform fees for owners
- **Vehicle-Specific Promos:** Owners can create discounts for their vehicles
  - Reduces owner's daily rate earnings
  - Attracts more bookings
- **Example:** Owner offers 10% off on Honda Civic, reducing their earnings from GHS 100/day to GHS 90/day

#### Phase 3: Referral System
- **Auto-Generated Codes:** System creates codes like "JUSTICE5847"
- **Custom Codes:** Users can create memorable referral codes
- **Reward Types:** Credit, Commission Reduction, or Cash
- **Tracking:** Full referral relationship tracking with stats

---

## Database Changes

### New Tables Created
1. **PromoCodes** (26 columns)
   - Code, PromoType, DiscountValue, TargetUserType, AppliesTo
   - MinimumBookingAmount, MaximumDiscountAmount
   - ValidFrom, ValidUntil
   - MaxTotalUses, MaxUsesPerUser, CurrentTotalUses
   - CategoryId, CityId, VehicleId (for owner vehicle promos)
   - IsReferralCode, ReferrerUserId, ReferralRewardType/Value
   - FirstTimeUsersOnly, IsActive

2. **PromoCodeUsage** (13 columns)
   - Tracks every promo code application
   - OriginalAmount, DiscountAmount, FinalAmount
   - BookingId, UsedByUserId, UserType
   - ReferrerUserId, ReferrerRewardAmount

3. **UserReferrals** (9 columns)
   - ReferrerUserId, ReferredUserId
   - TotalRewardEarned, TotalBookingsFromReferred
   - Status (active/inactive)

### Modified Tables
- **Bookings:** Added PromoCodeId and PromoDiscountAmount columns

### Sample Promo Codes
- `NEWYEAR2026`: 20% off for renters (first-time only, expires Jan 31)
- `WELCOME50`: GHS 50 off (first-time only, valid all year)
- `FREEPROTECTION`: Free protection plan (3 uses per user, expires Mar 31)
- `OWNER50OFF`: 50% commission reduction for owners (10 uses)
- `LISTFREE`: 100% commission waived for first 3 bookings

---

## API Endpoints

### Admin Endpoints (/api/v1/admin/promo-codes)
- `POST /` - Create promo code
- `GET /` - List all codes (with pagination & filters)
- `GET /{id}` - Get code details
- `PUT /{id}` - Update code
- `DELETE /{id}` - Deactivate code
- `GET /{id}/usage` - View usage history
- `GET /{id}/analytics` - Get analytics

### Owner Endpoints
- `POST /api/v1/owner/promo-codes` - Create vehicle-specific promo
- `GET /api/v1/owner/promo-codes` - List my vehicle promos
- `PUT /api/v1/owner/promo-codes/{id}` - Update vehicle promo
- `DELETE /api/v1/owner/promo-codes/{id}` - Deactivate vehicle promo
- `POST /api/v1/owner/referrals/generate` - Generate referral code
- `GET /api/v1/owner/referrals/codes` - List my referral codes
- `GET /api/v1/owner/referrals/stats` - Get referral statistics

### Renter/Public Endpoints
- `POST /api/v1/promo-codes/validate` - Validate & preview discount
- `POST /api/v1/bookings` (updated) - Create booking with promo code

---

## Code Changes

### New Files
1. **Models/PromoCode.cs**
   - PromoCode, PromoCodeUsage, UserReferral models
   - Enums: PromoCodeType, TargetUserType, DiscountAppliesTo, ReferralRewardType

2. **Services/PromoCodeService.cs**
   - ValidatePromoCodeAsync: Complete validation logic
   - ApplyPromoCodeAsync: Apply and track usage
   - GenerateReferralCodeAsync: Auto/custom code generation
   - GetUserReferralStatsAsync: Referral statistics
   - ProcessReferralRewardAsync: Reward processing

3. **Dtos/PromoCodeDtos.cs**
   - CreatePromoCodeDto, UpdatePromoCodeDto
   - ValidatePromoCodeDto, PromoCodeValidationResult
   - PromoCodeUsageDto, ReferralCodeDto
   - UserReferralStatsDto, PromoCodeAnalyticsDto

4. **Endpoints/PromoCodeEndpoints.cs**
   - All admin, owner, and public promo endpoints

5. **PROMO_CODE_API_DOCUMENTATION.md**
   - Complete API documentation for frontend teams
   - Sample requests/responses
   - React/TypeScript integration examples

### Modified Files
1. **Models/Booking.cs**
   - Added PromoCodeId and PromoDiscountAmount fields
   - Added PromoCode navigation property

2. **Endpoints/BookingEndpoints.cs**
   - Integrated promo validation before booking creation
   - Apply discount to totalAmount
   - Record promo usage after booking saved

3. **Dtos/BookingDtos.cs**
   - Added PromoCode field to request DTOs
   - Added promo details to BookingResponse

4. **Data/AppDbContext.cs**
   - Added DbSets: PromoCodes, PromoCodeUsage, UserReferrals

5. **Program.cs**
   - Registered PromoCodeService in DI
   - Mapped PromoCodeEndpoints

---

## Build & Deployment Issues Resolved

### Issue 1: Category Type Not Found
**Error:** `error CS0246: The type or namespace name 'Category' could not be found`  
**Fix:** Changed `Category` to `CarCategory` in PromoCode model

### Issue 2: Missing Booking Properties
**Error:** `'Booking' does not contain a definition for 'PromoCodeId'`  
**Fix:** Added PromoCodeId and PromoDiscountAmount properties to Booking model

### Issue 3: Type Conversion Error
**Error:** `Cannot implicitly convert type 'decimal?' to 'decimal'`  
**Fix:** Made PromoDiscountAmount nullable in BookingResponse DTO

---

## Testing

### Health Check
✅ Container running and healthy  
✅ API responding at http://172.212.2.194

### Next Steps for Testing
1. Test admin promo code creation
2. Test promo validation endpoint
3. Create booking with promo code
4. Test owner vehicle promo creation
5. Test referral code generation

---

## Important Notes

### IP Address Change
**Old IP:** 20.75.250.225  
**New IP:** 172.212.2.194  

⚠️ **ACTION REQUIRED:** Update Cloudflare DNS A record for ryverental.info to point to new IP: 172.212.2.194

### Configuration
- JWT Secret: 8kP9mN2vB5xR7wQ1tY4uE6sA3hG0fJ9lZ
- Database: ghanarentaldb on ryve-postgres-new
- Azure Storage: ryverentalstorage (corrected from previous misconfiguration)
- Cloudflare Turnstile: Enabled

### Sample Promo Codes
All sample codes are active in the database and ready for testing:
- **NEWYEAR2026** - Renter 20% discount (first-time, expires Jan 31)
- **WELCOME50** - Renter GHS 50 off (first-time, valid 2026)
- **FREEPROTECTION** - Free protection plan (3 uses, expires Mar 31)
- **OWNER50OFF** - Owner 50% commission reduction (10 uses)
- **LISTFREE** - Owner 100% commission waived first 3 bookings

---

## Frontend Integration

See [PROMO_CODE_API_DOCUMENTATION.md](PROMO_CODE_API_DOCUMENTATION.md) for:
- Complete endpoint documentation
- Request/response examples
- React hooks for promo validation
- Booking form integration example
- Error handling patterns

---

## Deployment Artifacts

- Docker Image: `ryveacrnewawjs.azurecr.io/ghana-rental-api:1.210`
- Deployment Script: `deploy-v1.210.ps1`
- Database Migration: `create-promo-code-tables.sql` (already executed)
- API Documentation: `PROMO_CODE_API_DOCUMENTATION.md`

---

## Success Metrics to Monitor

1. **Promo Code Usage**
   - Total codes created
   - Codes used vs. available
   - Discount amount distributed

2. **Booking Conversion**
   - Bookings with promo codes vs. without
   - Average discount per booking
   - New customer acquisition via promos

3. **Referral Performance**
   - Active referral codes
   - Referrals per user
   - Rewards distributed

4. **Owner Engagement**
   - Vehicle-specific promos created
   - Bookings generated from owner promos
   - Owner earnings impact

---

## Support

For issues or questions:
- Check logs: `az container logs --resource-group ryve-prod-new --name ghana-rental-api`
- Health endpoint: http://172.212.2.194/health
- Database queries via Azure PostgreSQL

---

**Deployment completed successfully on January 2, 2026 at 19:19 UTC**
