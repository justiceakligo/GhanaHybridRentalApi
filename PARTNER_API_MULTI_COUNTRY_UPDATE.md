# Partner API Multi-Country Update

## Overview

The Partner API has been updated to support multi-country operations. Partners can now access vehicle inventory, create bookings, and manage rentals across multiple countries in Africa.

**Version:** 2.0.0  
**Release Date:** March 5, 2026  
**Backward Compatibility:** ✅ Yes - Existing integrations continue to work without changes

---

## What's New

### Multi-Country Routing

The Partner API now supports country-specific endpoints while maintaining backward compatibility with existing integrations.

**New Route Pattern:**
```
/api/v1/{country}/partner/{endpoint}
```

**Supported Countries:**
- `gh` - Ghana (default)
- `ng` - Nigeria
- `ke` - Kenya
- `za` - South Africa
- `tz` - Tanzania

### New Response Fields

**Vehicles:**
- `countryCode` - Two-letter country code indicating vehicle location

**Protection Plans:**
- `currencySymbol` - Currency symbol for display (e.g., "₦" for Nigeria)
- `currency` - Now returns country-specific currency code

---

## Migration Guide

### For Existing Partners (Ghana Only)

**No action required!** Your existing integration continues to work:

```bash
# This still works exactly as before
GET /api/v1/partner/vehicles
GET /api/v1/partner/protection-plans
POST /api/v1/partner/bookings
```

All existing routes default to Ghana for backward compatibility.

### For Partners Expanding to New Countries

#### Step 1: Update Your Integration

Add country selection to your UI and modify API calls to include the country code:

**Before:**
```javascript
const response = await fetch('https://ryverental.info/api/v1/partner/vehicles', {
  headers: { 'X-API-Key': apiKey }
});
```

**After:**
```javascript
const country = userSelectedCountry; // 'gh', 'ng', 'ke', etc.
const response = await fetch(`https://ryverental.info/api/v1/${country}/partner/vehicles`, {
  headers: { 'X-API-Key': apiKey }
});
```

#### Step 2: Handle Country-Specific Currency

Display the correct currency for each country:

```javascript
// Protection plans now include currency info
const plans = await fetchProtectionPlans(country);
plans.forEach(plan => {
  console.log(`${plan.name}: ${plan.currencySymbol}${plan.dailyPrice} per day`);
  // Nigeria: "Basic Protection: ₦25,000 per day"
  // Kenya: "Basic Protection: KSh2,500 per day"
});
```

#### Step 3: Update Booking Flow

Ensure bookings are created for the correct country:

```javascript
async function createBooking(country, bookingData) {
  const response = await fetch(
    `https://ryverental.info/api/v1/${country}/partner/bookings`,
    {
      method: 'POST',
      headers: {
        'X-API-Key': apiKey,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(bookingData)
    }
  );
  return response.json();
}
```

---

## API Changes

### All Endpoints Now Support Country Routing

| Endpoint | Default (Ghana) | Country-Specific |
|----------|----------------|------------------|
| Get Vehicles | `GET /api/v1/partner/vehicles` | `GET /api/v1/{country}/partner/vehicles` |
| Get Protection Plans | `GET /api/v1/partner/protection-plans` | `GET /api/v1/{country}/partner/protection-plans` |
| Validate Promo | `POST /api/v1/partner/validate-promo` | `POST /api/v1/{country}/partner/validate-promo` |
| Create Booking | `POST /api/v1/partner/bookings` | `POST /api/v1/{country}/partner/bookings` |

### Response Schema Updates

#### Vehicles Response

**Added Field:**
```json
{
  "id": "...",
  "make": "Toyota",
  "model": "Corolla",
  "countryCode": "ng",  // NEW: Country code
  "cityId": "...",
  ...
}
```

#### Protection Plans Response

**Added Fields:**
```json
{
  "id": "...",
  "name": "Basic Protection",
  "currency": "NGN",      // Now country-specific
  "currencySymbol": "₦",  // NEW: Symbol for display
  "dailyPrice": 25000.00,
  ...
}
```

---

## Examples

### Ghana (Default - No Change)

```bash
# Get vehicles in Ghana (works as before)
curl -X GET "https://ryverental.info/api/v1/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"

# Create booking in Ghana (works as before)
curl -X POST "https://ryverental.info/api/v1/partner/bookings" \
  -H "X-API-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{"vehicleId": "...", ...}'
```

### Nigeria (New)

```bash
# Get vehicles in Nigeria
curl -X GET "https://ryverental.info/api/v1/ng/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"

# Response includes countryCode
{
  "id": "...",
  "make": "Toyota",
  "model": "Camry",
  "countryCode": "ng",
  "category": {
    "name": "Business",
    "defaultDailyRate": 45000.00  // In NGN (Nigerian Naira)
  }
}

# Get protection plans with Nigerian prices
curl -X GET "https://ryverental.info/api/v1/ng/partner/protection-plans" \
  -H "X-API-Key: your_api_key_here"

# Response includes currency symbol
{
  "id": "...",
  "name": "Premium Protection",
  "currency": "NGN",
  "currencySymbol": "₦",
  "dailyPrice": 30000.00
}

# Create booking in Nigeria
curl -X POST "https://ryverental.info/api/v1/ng/partner/bookings" \
  -H "X-API-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "vehicleId": "...",
    "pickupDateTime": "2026-02-01T10:00:00Z",
    "returnDateTime": "2026-02-05T10:00:00Z",
    "withDriver": false,
    "renterEmail": "customer@example.com",
    "renterName": "John Doe",
    "paymentMethod": "card"
  }'
```

### Kenya

```bash
# Get vehicles in Kenya
curl -X GET "https://ryverental.info/api/v1/ke/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"

# Get protection plans with Kenyan prices
curl -X GET "https://ryverental.info/api/v1/ke/partner/protection-plans" \
  -H "X-API-Key: your_api_key_here"

# Response example
{
  "currency": "KES",
  "currencySymbol": "KSh",
  "dailyPrice": 2500.00  // In KES (Kenyan Shillings)
}
```

### South Africa

```bash
# Get vehicles in South Africa
curl -X GET "https://ryverental.info/api/v1/za/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"

# Currency: ZAR (South African Rand), Symbol: R
```

### Tanzania

```bash
# Get vehicles in Tanzania
curl -X GET "https://ryverental.info/api/v1/tz/partner/vehicles" \
  -H "X-API-Key: your_api_key_here"

# Currency: TZS (Tanzanian Shilling), Symbol: TSh
```

---

## Currency Reference

| Country | Code | Currency Code | Symbol | Example Price |
|---------|------|---------------|--------|---------------|
| Ghana | `gh` | GHS | GHS | GHS 150.00 |
| Nigeria | `ng` | NGN | ₦ | ₦45,000 |
| Kenya | `ke` | KES | KSh | KSh 5,000 |
| South Africa | `za` | ZAR | R | R800 |
| Tanzania | `tz` | TZS | TSh | TSh 120,000 |

---

## Settlement & Payments

### Multi-Currency Settlements

When you operate in multiple countries, your settlement invoices will be grouped by currency:

**Monthly Invoice Example:**
```
RyveRental Partner Settlement - February 2026
Partner: YourBusiness Ltd

Ghana Operations (GHS):
- Total Bookings: GHS 10,000
- Commission (15%): GHS 1,500
- Settlement Due: GHS 8,500

Nigeria Operations (NGN):
- Total Bookings: NGN 2,450,000
- Commission (15%): NGN 367,500
- Settlement Due: NGN 2,082,500

Payment Instructions:
- GHS payments: [Ghana bank details]
- NGN payments: [Nigeria bank details]
```

### Commission Rates

Commission rates are consistent across countries (default 15%, negotiable) but applied to local currency amounts.

---

## Best Practices

### 1. Country Selection UI

Provide clear country selection in your UI:

```javascript
const countries = [
  { code: 'gh', name: 'Ghana', currency: 'GHS', symbol: 'GHS' },
  { code: 'ng', name: 'Nigeria', currency: 'NGN', symbol: '₦' },
  { code: 'ke', name: 'Kenya', currency: 'KES', symbol: 'KSh' },
  { code: 'za', name: 'South Africa', currency: 'ZAR', symbol: 'R' },
  { code: 'tz', name: 'Tanzania', currency: 'TZS', symbol: 'TSh' }
];
```

### 2. Display Prices Correctly

Always show prices in the local currency with the correct symbol:

```javascript
function formatPrice(amount, currencySymbol) {
  return `${currencySymbol}${amount.toLocaleString()}`;
}

// Nigeria: "₦45,000"
// Kenya: "KSh5,000"
// South Africa: "R800"
```

### 3. Cache Country Data

Cache vehicle and protection plan data per country to reduce API calls:

```javascript
const cache = {
  gh: { vehicles: [], plans: [], timestamp: null },
  ng: { vehicles: [], plans: [], timestamp: null },
  // ...
};

async function getVehicles(country) {
  const cacheEntry = cache[country];
  const now = Date.now();
  
  // Cache for 5 minutes
  if (cacheEntry.timestamp && now - cacheEntry.timestamp < 300000) {
    return cacheEntry.vehicles;
  }
  
  const vehicles = await fetchVehicles(country);
  cache[country] = { ...cacheEntry, vehicles, timestamp: now };
  return vehicles;
}
```

### 4. Error Handling

Handle country-specific errors gracefully:

```javascript
try {
  const vehicles = await getVehicles(selectedCountry);
} catch (error) {
  if (error.status === 404) {
    // Country not found or not yet active
    showMessage(`Service not yet available in ${countryName}`);
  } else {
    showMessage('Unable to fetch vehicles. Please try again.');
  }
}
```

### 5. Testing

Test your integration for each country separately:

```bash
# Test each country
for country in gh ng ke za tz; do
  echo "Testing $country..."
  curl -X GET "https://ryverental.info/api/v1/$country/partner/vehicles" \
    -H "X-API-Key: $API_KEY"
done
```

---

## Documentation Updates

### Updated Resources

1. **OpenAPI Specification:** [partner-api-openapi.yaml](partner-api-openapi.yaml)
   - Version updated to 2.0.0
   - Added country parameter definitions
   - Added multi-country endpoint paths

2. **Partner Integration Guide:** [PARTNER_INTEGRATION_GUIDE.md](PARTNER_INTEGRATION_GUIDE.md)
   - Added multi-country support section
   - Updated all endpoint examples
   - Added currency reference table

3. **API Endpoints:**
   - All partner endpoints updated with country context support
   - Backward compatible with existing Ghana-only integrations

---

## Support

### Questions?

**Partner Support:**
- Email: partners@ryverental.com
- Phone: +233 XX XXX XXXX
- Documentation: https://docs.ryverental.com/partners

### Reporting Issues

If you encounter any issues with the multi-country update:

1. Check the [PARTNER_INTEGRATION_GUIDE.md](PARTNER_INTEGRATION_GUIDE.md)
2. Review the [OpenAPI spec](partner-api-openapi.yaml)
3. Contact partner support with:
   - Your partner ID
   - Country code you're testing
   - API endpoint and request details
   - Error message or unexpected response

---

## Timeline

- **March 5, 2026:** Multi-country API released
- **March - April 2026:** Partner migration period (optional)
- **Ongoing:** Ghana-only integrations continue to work without changes

---

## FAQ

**Q: Do I need to update my integration?**  
A: No, if you only operate in Ghana. Your existing integration continues to work without changes.

**Q: How do I start serving customers in Nigeria?**  
A: Update your integration to support country selection and use the `/api/v1/ng/partner/...` endpoints.

**Q: Will my API key work for all countries?**  
A: Yes, your API key works across all countries. Use the same key with country-specific endpoints.

**Q: How are prices converted between currencies?**  
A: Prices are not converted. Each country has its own inventory with local currency pricing.

**Q: Can I create a booking in Nigeria for a vehicle in Ghana?**  
A: No, vehicles can only be booked within their own country. Use the country-specific endpoint that matches the vehicle's location.

**Q: How do settlements work across multiple countries?**  
A: You'll receive separate settlement amounts for each currency. Payment instructions will be provided per currency.

**Q: What if a country is not yet active?**  
A: Inactive countries will return empty results. Check the country status with your partner account manager.

---

**Last Updated:** March 5, 2026  
**API Version:** 2.0.0  
**Document Version:** 1.0.0
