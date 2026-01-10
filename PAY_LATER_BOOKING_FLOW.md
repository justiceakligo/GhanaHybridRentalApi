# Pay Later Booking Flow - API Documentation

## Overview
This feature allows renters to retrieve unpaid bookings, sign rental agreements, and initiate payment at a later time. This is useful for bookings that were created but not immediately paid.

## Database Changes

Run the migration script to add agreement signing fields:
```bash
psql -h <host> -U <user> -d <database> -f add-agreement-signing-fields.sql
```

## New Endpoints

### 1. Get Booking by Reference (Public)
Retrieve booking details using the booking reference number.

**Endpoint:** `GET /api/v1/bookings/reference/{bookingReference}`

**Access:** Public endpoint with authorization checks
- **Guests** (no authentication): Can access any booking by reference
- **Renters** (authenticated): Can access only their own bookings
- **Admins** (authenticated): Can access any booking
- **Owners** (authenticated): Can access bookings for their vehicles

**Parameters:**
- `bookingReference` (path): Booking reference (e.g., "RV-2026-ECB675")

**Response:**
```json
{
  "id": "uuid",
  "bookingReference": "RV-2026-ECB675",
  "status": "pending_payment",
  "paymentStatus": "unpaid",
  "pickupDateTime": "2026-01-15T10:00:00Z",
  "returnDateTime": "2026-01-20T10:00:00Z",
  "pickupLocation": "Accra",
  "returnLocation": "Accra",
  "withDriver": false,
  "currency": "GHS",
  "rentalAmount": 500.00,
  "depositAmount": 200.00,
  "totalAmount": 700.00,
  "paymentMethod": "momo",
  "createdAt": "2026-01-06T16:42:00Z",
  "agreementSigned": false,
  "agreementSignedAt": null,
  "vehicle": {
    "id": "uuid",
    "make": "Toyota",
    "model": "Corolla",
    "year": 2023,
    "plateNumber": "GR-123-45",
    "transmission": "Automatic",
    "fuelType": "Petrol",
    "seatingCapacity": 5,
    "hasAC": true,
    "dailyRate": 100.00,
    "photos": ["url1", "url2"],
    "category": {
      "name": "Economy",
      "defaultDailyRate": 100.00,
      "defaultDepositAmount": 200.00
    }
  },
  "renter": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+233501234567"
  },
  "guestEmail": null,
  "guestPhone": null,
  "owner": {
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "owner@example.com",
    "phone": "+233501234568",
    "businessPhone": "+233501234569",
    "businessAddress": "123 Main St, Accra",
    "pickupInstructions": "Call when you arrive"
  }
}
```

**Use Case:**
- Renter receives booking confirmation email with reference
- Renter visits website and enters reference to view booking details
- System displays full booking information including vehicle, dates, and pricing

---

### 2. Sign Rental Agreement (Public)
Accept and digitally sign the rental agreement before payment.

**Endpoint:** `POST /api/v1/bookings/{bookingId}/sign-agreement`

**Access:** Public endpoint with authorization checks
- **Guests** (no authentication): Can sign agreements for guest bookings
- **Renters** (authenticated): Can sign agreements only for their own bookings
- **Admins** (authenticated): Can sign agreements for any booking (on behalf of customer)

**Parameters:**
- `bookingId` (path): Booking UUID

**Request Body:**
```json
{
  "signedByName": "John Doe",
  "signatureData": "base64_encoded_signature_image_or_acceptance_token"
}
```

**Response:**
```json
{
  "message": "Agreement signed successfully",
  "bookingReference": "RV-2026-ECB675",
  "agreementSigned": true,
  "agreementSignedAt": "2026-01-07T10:30:00Z",
  "signedBy": "John Doe"
}
```

**Validation:**
- Booking must exist
- Payment status must be "unpaid"
- Agreement cannot be signed twice
- IP address is automatically recorded

**Use Case:**
- After viewing booking details, renter is shown rental agreement
- Renter reads and accepts terms
- Renter provides name and optional signature (canvas drawing or checkbox acceptance)
- System records signature with timestamp and IP address

---

### 3. Initiate Payment for Existing Booking (Public)
Generate payment link for an unpaid booking.

**Endpoint:** `POST /api/v1/bookings/{bookingId}/initiate-payment`

**Access:** Public endpoint with authorization checks
- **Guests** (no authentication): Can initiate payment for guest bookings
- **Renters** (authenticated): Can initiate payment only for their own bookings
- **Admins** (authenticated): Can initiate payment for any booking (to help customers)

**Parameters:**
- `bookingId` (path): Booking UUID

**Request Body:**
```json
{
  "email": "john@example.com",
  "paymentMethod": "momo"
}
```

**Fields:**
- `email` (optional): Email for payment receipt (defaults to booking email)
- `paymentMethod` (optional): "momo" or "card" (defaults to booking's payment method)

**Response for Mobile Money (Paystack):**
```json
{
  "paymentProvider": "paystack",
  "paymentReference": "PAY-RV-2026-ECB675-A1B2C3D4",
  "authorizationUrl": "https://checkout.paystack.com/...",
  "amount": 700.00,
  "currency": "GHS",
  "bookingReference": "RV-2026-ECB675"
}
```

**Response for Card Payment (Stripe):**
```json
{
  "paymentProvider": "stripe",
  "paymentReference": "PAY-RV-2026-ECB675-A1B2C3D4",
  "clientSecret": "pi_xxx_secret_yyy",
  "paymentIntentId": "pi_xxx",
  "amount": 700.00,
  "currency": "GHS",
  "bookingReference": "RV-2026-ECB675"
}
```

**Validation:**
- Booking must exist and not be cancelled
- Payment status must be "unpaid"
- Agreement must be signed (if `Payment:RequireAgreementBeforePayment` config is enabled)
- Email is required (from request, renter, or guest)

**Use Case:**
- After signing agreement, renter clicks "Pay Now"
- System generates payment link/session
- For mobile money: redirect to Paystack checkout
- For card: use Stripe Elements with clientSecret
- Payment webhook updates booking status when complete

---

## Complete Flow Example

### Scenario: Renter wants to pay for booking later

1. **Create Booking** (existing functionality)
   ```
   POST /api/v1/bookings
   ```
   - Booking created with status "pending_payment" and paymentStatus "unpaid"
   - Confirmation email sent with booking reference: RV-2026-ECB675

2. **Retrieve Booking Later**
   ```
   GET /api/v1/bookings/reference/RV-2026-ECB675
   ```
   - Renter enters reference on website
   - System displays booking details, vehicle info, pricing

3. **Sign Agreement**
   ```
   POST /api/v1/bookings/{bookingId}/sign-agreement
   {
     "signedByName": "John Doe",
     "signatureData": "data:image/png;base64,..."
   }
   ```
   - Renter reviews terms and conditions
   - Signs digitally (canvas signature or checkbox)
   - Agreement recorded with timestamp

4. **Initiate Payment**
   ```
   POST /api/v1/bookings/{bookingId}/initiate-payment
   {
     "email": "john@example.com",
     "paymentMethod": "momo"
   }
   ```
   - System generates payment link
   - Renter redirected to payment provider
   - Completes payment

5. **Payment Webhook** (existing functionality)
   - Paystack/Stripe webhook fires
   - Booking updated to status "confirmed", paymentStatus "paid"
   - Confirmation email sent to renter and owner

---

## Configuration Options

Add to `AppConfigs` table or `appsettings.json`:

```sql
INSERT INTO "AppConfigs" ("ConfigKey", "ConfigValue", "Description")
VALUES 
  ('Payment:RequireAgreementBeforePayment', 'true', 'Require agreement signature before payment initiation'),
  ('Payment:AgreementExtendedWindow', '72', 'Hours to extend cancellation if agreement is signed (default: 72)');
```

---

## Security Considerations

1. **Authorization Model**: 
   - Endpoints use `.AllowAnonymous()` to permit guest access
   - Authorization logic inside endpoints checks user role and ownership
   - Admins can access/modify any booking
   - Renters can only access their own bookings
   - Guests (unauthenticated) can access bookings by reference
   - Owners can view bookings for their vehicles

2. **Rate Limiting**: Consider adding rate limiting to public endpoints to prevent abuse

3. **Reference Validation**: Booking references should be unpredictable (uses GUID segments)

4. **IP Recording**: Agreement signing records IP address for audit trail

5. **Email Verification**: Consider sending verification code to email before payment

6. **HTTPS Only**: All endpoints should be accessed over HTTPS in production

---

## Future Enhancements

1. **Auto-cancellation Extension**: Modify `UnpaidBookingCancellationService` to extend 24-hour window to 72 hours if agreement is signed
2. **SMS Verification**: Send OTP to phone number before allowing payment
3. **Agreement Templates**: Store different agreement versions and track which version was signed
4. **Partial Payments**: Allow deposit-only payment with balance due later
5. **Payment Plans**: Split total into installments

---

## Error Handling

All endpoints return standard error responses:

```json
{
  "error": "Error message description"
}
```

Common error cases:
- `404`: Booking not found
- `400`: Invalid request (already signed, already paid, booking cancelled, etc.)
- `500`: Payment provider errors (not configured, API error)
