# Country Onboarding Admin Guide

## Overview
This guide covers the complete process of onboarding a new country using the admin API endpoints.

## Prerequisites
- Admin authentication token
- Country details (code, currency, timezone, etc.)
- List of major cities
- Payment gateway credentials

## Admin Endpoints

### 1. Country Management

#### Create Country
```http
POST /api/v1/admin/countries
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "code": "NG",
  "name": "Nigeria",
  "currencyCode": "NGN",
  "currencySymbol": "₦",
  "phoneCode": "+234",
  "timezone": "Africa/Lagos",
  "defaultLanguage": "en-NG",
  "isActive": false,
  "paymentProviders": ["paystack", "flutterwave"]
}
```

#### Get Country Details
```http
GET /api/v1/admin/countries/{code}
Authorization: Bearer {admin_token}
```

#### Update Country
```http
PUT /api/v1/admin/countries/{code}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Updated Name",
  "currencySymbol": "₦"
}
```

### 2. Payment Configuration

#### Update Payment Providers
```http
PUT /api/v1/admin/countries/{code}/payment-providers
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "providers": ["paystack", "flutterwave", "stripe"]
}
```

#### Set Payment Gateway Configuration
```http
POST /api/v1/admin/countries/{code}/app-config
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "settings": {
    "Payment:Paystack:SecretKey": "sk_live_...",
    "Payment:Paystack:PublicKey": "pk_live_...",
    "Payment:Flutterwave:SecretKey": "FLWSECK-...",
    "Payment:Flutterwave:PublicKey": "FLWPUBK-..."
  }
}
```

#### Get Country App Configuration
```http
GET /api/v1/admin/countries/{code}/app-config
Authorization: Bearer {admin_token}
```

### 3. City Management

#### Bulk Create Cities
```http
POST /api/v1/admin/countries/{code}/cities/bulk
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "cities": [
    {
      "name": "Lagos",
      "region": "Lagos State",
      "isActive": true,
      "displayOrder": 1,
      "defaultDeliveryFee": 500
    },
    {
      "name": "Abuja",
      "region": "FCT",
      "isActive": true,
      "displayOrder": 2,
      "defaultDeliveryFee": 600
    },
    {
      "name": "Port Harcourt",
      "region": "Rivers State",
      "isActive": true,
      "displayOrder": 3
    }
  ]
}
```

#### Get Country Cities
```http
GET /api/v1/admin/countries/{code}/cities
Authorization: Bearer {admin_token}
```

### 4. Country Configuration

#### Update Country-Specific Config
```http
PUT /api/v1/admin/countries/{code}/config
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "config": {
    "taxRate": 0.075,
    "platformFeePercentage": 15,
    "minBookingAmount": 10000,
    "supportEmail": "support@ryverental.ng",
    "supportPhone": "+234-XXX-XXXXX"
  }
}
```

### 5. Onboarding Workflow

#### Check Onboarding Status
```http
GET /api/v1/admin/countries/{code}/onboarding-status
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "country": "Nigeria",
  "countryCode": "NG",
  "isActive": false,
  "isReady": true,
  "progress": {
    "completed": 5,
    "total": 5,
    "percentage": 100
  },
  "checklist": [
    {
      "step": "Country Created",
      "completed": true,
      "required": true
    },
    {
      "step": "Currency Configured",
      "completed": true,
      "required": true
    },
    {
      "step": "Payment Providers Set",
      "completed": true,
      "required": true
    },
    {
      "step": "Payment Configuration",
      "completed": true,
      "required": true
    },
    {
      "step": "Cities Added",
      "completed": true,
      "required": true
    }
  ],
  "stats": {
    "citiesCount": 10,
    "vehiclesCount": 0,
    "hasPaymentConfig": true
  }
}
```

#### Complete Onboarding (Activate Country)
```http
POST /api/v1/admin/countries/{code}/onboarding/complete
Authorization: Bearer {admin_token}
```

### 6. Testing & Validation

#### Test Country Configuration
```http
POST /api/v1/admin/countries/{code}/test
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "country": "Nigeria",
  "countryCode": "NG",
  "overallStatus": "PASSED",
  "testsRun": 5,
  "testsPassed": 5,
  "tests": [
    {
      "test": "Basic Information",
      "passed": true,
      "details": {
        "name": "Nigeria",
        "code": "NG",
        "currencyCode": "NGN"
      }
    },
    {
      "test": "Payment Providers",
      "passed": true,
      "details": {
        "providers": ["paystack", "flutterwave"]
      }
    }
  ],
  "isReady": true
}
```

#### Get Country Statistics
```http
GET /api/v1/admin/countries/{code}/stats
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "country": "Nigeria",
  "countryCode": "NG",
  "isActive": true,
  "stats": {
    "cities": {
      "total": 10,
      "active": 10
    },
    "vehicles": {
      "total": 45,
      "active": 38
    },
    "bookings": {
      "total": 127
    },
    "owners": {
      "total": 15
    }
  }
}
```

### 7. Country Lifecycle

#### Activate Country
```http
POST /api/v1/admin/countries/{code}/activate
Authorization: Bearer {admin_token}
```

#### Deactivate Country
```http
DELETE /api/v1/admin/countries/{code}
Authorization: Bearer {admin_token}
```

## Step-by-Step Onboarding Process

### Step 1: Create Country
1. Prepare country information
2. Call `POST /api/v1/admin/countries`
3. Set `isActive: false` initially

### Step 2: Configure Payment Providers
1. Determine which payment providers are available
2. Call `PUT /api/v1/admin/countries/{code}/payment-providers`
3. Set payment gateway credentials with `POST /api/v1/admin/countries/{code}/app-config`

### Step 3: Add Cities
1. Prepare list of major cities
2. Call `POST /api/v1/admin/countries/{code}/cities/bulk`
3. Verify cities with `GET /api/v1/admin/countries/{code}/cities`

### Step 4: Configure Country Settings
1. Set country-specific configurations
2. Call `PUT /api/v1/admin/countries/{code}/config`
3. Include tax rates, fees, support contacts

### Step 5: Test Configuration
1. Call `POST /api/v1/admin/countries/{code}/test`
2. Verify all tests pass
3. Check `GET /api/v1/admin/countries/{code}/onboarding-status`

### Step 6: Complete Onboarding
1. Ensure onboarding status shows "isReady: true"
2. Call `POST /api/v1/admin/countries/{code}/onboarding/complete`
3. Country is now active and operational

### Step 7: Monitor & Verify
1. Check stats with `GET /api/v1/admin/countries/{code}/stats`
2. Test public endpoints: `GET /api/v1/{code}/cities`
3. Verify currency appears correctly in settings

## Common Configurations by Country

### Nigeria (NG)
```json
{
  "code": "NG",
  "name": "Nigeria",
  "currencyCode": "NGN",
  "currencySymbol": "₦",
  "phoneCode": "+234",
  "timezone": "Africa/Lagos",
  "defaultLanguage": "en-NG",
  "paymentProviders": ["paystack", "flutterwave"],
  "config": {
    "taxRate": 0.075,
    "supportEmail": "support@ryverental.ng",
    "supportPhone": "+234-XXX-XXXXX"
  }
}
```

**Major Cities:** Lagos, Abuja, Port Harcourt, Kano, Ibadan, Kaduna

### Kenya (KE)
```json
{
  "code": "KE",
  "name": "Kenya",
  "currencyCode": "KES",
  "currencySymbol": "KSh",
  "phoneCode": "+254",
  "timezone": "Africa/Nairobi",
  "defaultLanguage": "en-KE",
  "paymentProviders": ["mpesa", "flutterwave"],
  "config": {
    "taxRate": 0.16,
    "supportEmail": "support@ryverental.ke",
    "supportPhone": "+254-XXX-XXXXX"
  }
}
```

**Major Cities:** Nairobi, Mombasa, Kisumu, Nakuru, Eldoret

### South Africa (ZA)
```json
{
  "code": "ZA",
  "name": "South Africa",
  "currencyCode": "ZAR",
  "currencySymbol": "R",
  "phoneCode": "+27",
  "timezone": "Africa/Johannesburg",
  "defaultLanguage": "en-ZA",
  "paymentProviders": ["paystack", "stripe"],
  "config": {
    "taxRate": 0.15,
    "supportEmail": "support@ryverental.co.za",
    "supportPhone": "+27-XXX-XXXXX"
  }
}
```

**Major Cities:** Johannesburg, Cape Town, Durban, Pretoria, Port Elizabeth

## Payment Provider Setup

### Paystack
Required configuration keys:
- `Payment:Paystack:SecretKey`
- `Payment:Paystack:PublicKey`
- `Payment:Paystack:CallbackUrl` (optional)

Available in: Ghana, Nigeria, South Africa, Kenya

### Flutterwave
Required configuration keys:
- `Payment:Flutterwave:SecretKey`
- `Payment:Flutterwave:PublicKey`
- `Payment:Flutterwave:EncryptionKey`

Available in: Nigeria, Kenya, Ghana, South Africa

### M-Pesa (Kenya)
Required configuration keys:
- `Payment:MPesa:ConsumerKey`
- `Payment:MPesa:ConsumerSecret`
- `Payment:MPesa:ShortCode`
- `Payment:MPesa:PassKey`

Available in: Kenya, Tanzania

### Stripe
Required configuration keys:
- `Payment:Stripe:SecretKey`
- `Payment:Stripe:PublicKey`
- `Payment:Stripe:WebhookSecret`

Available in: Global

## Security Considerations

1. **Always use HTTPS** for API calls
2. **Protect admin tokens** - never commit to source control
3. **Test in staging first** before production
4. **Backup database** before major changes
5. **Monitor logs** during onboarding
6. **Validate payment credentials** in test mode first
7. **Set reasonable rate limits** for new countries

## Troubleshooting

### Issue: Onboarding status shows incomplete
**Solution:** Check the checklist to see which steps are missing
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.ryverental.com/api/v1/admin/countries/NG/onboarding-status
```

### Issue: Payment configuration not working
**Solution:** Verify AppConfig keys are prefixed correctly with country code
```bash
# Keys should be: NG:Payment:Paystack:SecretKey
curl -H "Authorization: Bearer $TOKEN" \
  https://api.ryverental.com/api/v1/admin/countries/NG/app-config
```

### Issue: Cities not showing in public API
**Solution:** Ensure cities have `isActive: true` and country is activated
```bash
curl https://api.ryverental.com/api/v1/ng/cities
```

### Issue: Country tests failing
**Solution:** Run test endpoint to see specific failures
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" \
  https://api.ryverental.com/api/v1/admin/countries/NG/test
```

## Best Practices

1. **Start with inactive countries** - Set `isActive: false` during setup
2. **Test thoroughly** - Use test endpoint before activation
3. **Add major cities first** - Can add more cities later
4. **Document payment setup** - Keep credentials secure and documented
5. **Monitor after activation** - Watch logs and stats for issues
6. **Gradual rollout** - Don't activate all countries at once
7. **Have rollback plan** - Can deactivate country if issues arise

## Example Workflow Script

See `onboard-country.ps1` for a complete automated onboarding script.

## API Response Codes

- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing/invalid auth token
- `403 Forbidden` - Not admin
- `404 Not Found` - Country doesn't exist
- `500 Internal Server Error` - Server error

## Rate Limits

Admin endpoints may have rate limits:
- Country creation: 10 per hour
- Bulk city creation: 100 cities per request
- Configuration updates: 50 per hour

## Support

For issues with country onboarding:
1. Check onboarding status endpoint
2. Run test configuration endpoint
3. Review application logs
4. Contact development team with country code and error details
