# Partner API - Quick Reference: Multi-Country Changes

**Version:** 2.0.0 | **Date:** March 5, 2026 | **Breaking Changes:** None ✅

---

## TL;DR

✅ **Existing integrations continue to work without changes**  
✅ **Add `/{country}` to URL path to access other countries**  
✅ **Response includes `countryCode` and `currencySymbol` fields**

---

## Quick Comparison

### Before (Still Works)

```bash
GET /api/v1/partner/vehicles
GET /api/v1/partner/protection-plans
POST /api/v1/partner/validate-promo
POST /api/v1/partner/bookings
```

### After (New - Multi-Country)

```bash
GET /api/v1/{country}/partner/vehicles
GET /api/v1/{country}/partner/protection-plans
POST /api/v1/{country}/partner/validate-promo
POST /api/v1/{country}/partner/bookings
```

**Countries:** `gh`, `ng`, `ke`, `za`, `tz`

---

## Code Examples

### JavaScript/TypeScript

```typescript
// Before (Ghana only) - Still works
const vehicles = await fetch('https://ryverental.info/api/v1/partner/vehicles', {
  headers: { 'X-API-Key': apiKey }
});

// After (Multi-country) - New
const country = 'ng'; // User selected
const vehicles = await fetch(`https://ryverental.info/api/v1/${country}/partner/vehicles`, {
  headers: { 'X-API-Key': apiKey }
});
```

### Python

```python
# Before (Ghana only) - Still works
response = requests.get(
    'https://ryverental.info/api/v1/partner/vehicles',
    headers={'X-API-Key': api_key}
)

# After (Multi-country) - New
country = 'ng'  # User selected
response = requests.get(
    f'https://ryverental.info/api/v1/{country}/partner/vehicles',
    headers={'X-API-Key': api_key}
)
```

### PHP

```php
// Before (Ghana only) - Still works
$response = $client->get('https://ryverental.info/api/v1/partner/vehicles', [
    'headers' => ['X-API-Key' => $apiKey]
]);

// After (Multi-country) - New
$country = 'ng';  // User selected
$response = $client->get("https://ryverental.info/api/v1/{$country}/partner/vehicles", [
    'headers' => ['X-API-Key' => $apiKey]
]);
```

### C#

```csharp
// Before (Ghana only) - Still works
var response = await httpClient.GetAsync("https://ryverental.info/api/v1/partner/vehicles");

// After (Multi-country) - New
var country = "ng";  // User selected
var response = await httpClient.GetAsync($"https://ryverental.info/api/v1/{country}/partner/vehicles");
```

---

## Response Changes

### Vehicles API - Added `countryCode`

```json
{
  "id": "...",
  "make": "Toyota",
  "model": "Corolla",
  "countryCode": "ng",  // ← NEW
  "cityId": "...",
  ...
}
```

### Protection Plans API - Added `currencySymbol`

```json
{
  "id": "...",
  "name": "Basic Protection",
  "currency": "NGN",       // Now country-specific
  "currencySymbol": "₦",   // ← NEW
  "dailyPrice": 25000.00,
  ...
}
```

---

## Country Codes & Currencies

| Code | Country | Currency | Symbol |
|------|---------|----------|--------|
| `gh` | Ghana | GHS | GHS |
| `ng` | Nigeria | NGN | ₦ |
| `ke` | Kenya | KES | KSh |
| `za` | South Africa | ZAR | R |
| `tz` | Tanzania | TZS | TSh |

---

## Migration Checklist

- [ ] **Read this document** (you're doing it!)
- [ ] **Test existing integration** - Should still work for Ghana
- [ ] **Add country selector** to your UI (if supporting multiple countries)
- [ ] **Update API calls** to include `/{country}` path parameter
- [ ] **Display currency symbols** correctly (use `currencySymbol` field)
- [ ] **Test each country** separately
- [ ] **Update settlement handling** for multi-currency invoices

---

## Common Patterns

### Simple Country Selector

```javascript
const countries = {
  gh: { name: 'Ghana', currency: 'GHS', symbol: 'GHS' },
  ng: { name: 'Nigeria', currency: 'NGN', symbol: '₦' },
  ke: { name: 'Kenya', currency: 'KES', symbol: 'KSh' },
  za: { name: 'South Africa', currency: 'ZAR', symbol: 'R' },
  tz: { name: 'Tanzania', currency: 'TZS', symbol: 'TSh' }
};

function renderCountrySelector() {
  return Object.entries(countries).map(([code, data]) => 
    `<option value="${code}">${data.name} (${data.symbol})</option>`
  );
}
```

### API Helper Function

```javascript
class RyveRentalAPI {
  constructor(apiKey, country = 'gh') {
    this.apiKey = apiKey;
    this.country = country;
    this.baseUrl = 'https://ryverental.info/api/v1';
  }
  
  setCountry(country) {
    this.country = country;
  }
  
  async getVehicles(params = {}) {
    const url = `${this.baseUrl}/${this.country}/partner/vehicles`;
    const response = await fetch(url + this.buildQuery(params), {
      headers: { 'X-API-Key': this.apiKey }
    });
    return response.json();
  }
  
  async getProtectionPlans() {
    const url = `${this.baseUrl}/${this.country}/partner/protection-plans`;
    const response = await fetch(url, {
      headers: { 'X-API-Key': this.apiKey }
    });
    return response.json();
  }
  
  async createBooking(bookingData) {
    const url = `${this.baseUrl}/${this.country}/partner/bookings`;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'X-API-Key': this.apiKey,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(bookingData)
    });
    return response.json();
  }
  
  buildQuery(params) {
    const query = new URLSearchParams(params).toString();
    return query ? '?' + query : '';
  }
}

// Usage
const api = new RyveRentalAPI('your_api_key');

// Ghana (default)
const ghVehicles = await api.getVehicles();

// Switch to Nigeria
api.setCountry('ng');
const ngVehicles = await api.getVehicles({ startDate: '2026-02-01T10:00:00Z' });
```

---

## Testing Commands

```bash
# Test all countries quickly
API_KEY="your_key_here"

# Ghana (default)
curl -H "X-API-Key: $API_KEY" https://ryverental.info/api/v1/partner/vehicles

# Nigeria
curl -H "X-API-Key: $API_KEY" https://ryverental.info/api/v1/ng/partner/vehicles

# Kenya
curl -H "X-API-Key: $API_KEY" https://ryverental.info/api/v1/ke/partner/vehicles

# South Africa
curl -H "X-API-Key: $API_KEY" https://ryverental.info/api/v1/za/partner/vehicles

# Tanzania
curl -H "X-API-Key: $API_KEY" https://ryverental.info/api/v1/tz/partner/vehicles
```

---

## Need Help?

📧 **Email:** partners@ryverental.com  
📞 **Phone:** +233 XX XXX XXXX  
📚 **Full Docs:** [PARTNER_INTEGRATION_GUIDE.md](PARTNER_INTEGRATION_GUIDE.md)  
🔧 **OpenAPI Spec:** [partner-api-openapi.yaml](partner-api-openapi.yaml)  
📖 **Detailed Guide:** [PARTNER_API_MULTI_COUNTRY_UPDATE.md](PARTNER_API_MULTI_COUNTRY_UPDATE.md)

---

**Last Updated:** March 5, 2026
