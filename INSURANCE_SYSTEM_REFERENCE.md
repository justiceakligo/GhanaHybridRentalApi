# Insurance System - Quick Reference Guide

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Booking Created                          │
│              (Ghana, Nigeria, Kenya, Canada)                 │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
           ┌──────────────────────┐
           │ InsuranceOrchestrator │ (Main Coordinator)
           └──────────┬───────────┘
                      │
                      ▼
           Check CountryInsuranceConfig
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
    RequiresRealInsurance      RequiresRealInsurance
         = false                   = true
         │                         │
         ▼                         ▼
┌─────────────────┐       ┌──────────────────┐
│MockInsurance    │       │CanadaInsurance   │
│Provider         │       │Provider          │
│(Ghana/NG/KE)    │       │(Canada)          │
├─────────────────┤       ├──────────────────┤
│• No API calls   │       │• Intact Insurance│
│• $0 premium     │       │• $15-25/day      │
│• Instant policy │       │• $2M liability   │
└────────┬────────┘       └────────┬─────────┘
         │                         │
         └────────────┬────────────┘
                      ▼
              Create InsurancePolicy
              (Store in database)
                      │
                      ▼
           ┌──────────────────────┐
           │ CertificateGenerator  │
           └──────────┬───────────┘
                      │
                      ▼
              Generate Certificate PDF
              (Currently HTML, TODO: PDF)
                      │
                      ▼
              Save to wwwroot/certificates/
                      │
                      ▼
           Update Booking & Policy
           with certificate URL
                      │
                      ▼
              Customer Receives
              Booking Confirmation
              with Certificate Link
```

## Database Schema

### CountryInsuranceConfig
```sql
CREATE TABLE CountryInsuranceConfig (
    Id SERIAL PRIMARY KEY,
    CountryCode VARCHAR(2) NOT NULL REFERENCES Countries(Code),
    RequiresRealInsurance BOOLEAN NOT NULL DEFAULT FALSE,
    InsuranceProviderName VARCHAR(255),
    InsuranceProviderApiUrl VARCHAR(500),
    AutoIssuePolicy BOOLEAN DEFAULT TRUE,
    MinimumLiabilityAmount DECIMAL(18,2) DEFAULT 0,
    ConfigJson TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### InsurancePolicies
```sql
CREATE TABLE InsurancePolicies (
    Id SERIAL PRIMARY KEY,
    BookingId INT NOT NULL REFERENCES Bookings(Id),
    ProtectionPlanId GUID NOT NULL REFERENCES ProtectionPlans(Id),
    PolicyNumber VARCHAR(255) NOT NULL UNIQUE,
    InsuranceProviderName VARCHAR(255) NOT NULL,
    CoverageStartDate TIMESTAMP NOT NULL,
    CoverageEndDate TIMESTAMP NOT NULL,
    PremiumAmount DECIMAL(18,2) NOT NULL,
    LiabilityCoverage DECIMAL(18,2) NOT NULL,
    CertificateUrl VARCHAR(500),
    Status VARCHAR(50) DEFAULT 'issued',
    ProviderPolicyJson TEXT,
    IssuedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Bookings (Updated)
```sql
ALTER TABLE Bookings 
ADD COLUMN InsuranceCertificateUrl VARCHAR(500);
```

## Service Interfaces

### IInsuranceProvider
```csharp
public interface IInsuranceProvider
{
    // Create new insurance policy
    Task<PolicyResponse?> CreatePolicyAsync(PolicyRequest request);
    
    // Cancel existing policy
    Task<bool> CancelPolicyAsync(string policyNumber);
    
    // Get policy details
    Task<PolicyResponse?> GetPolicyAsync(string policyNumber);
}
```

### IInsuranceOrchestrator
```csharp
public interface IInsuranceOrchestrator
{
    // Handle insurance for a booking (main entry point)
    Task HandleBookingInsuranceAsync(int bookingId);
    
    // Get policy for a booking
    Task<InsurancePolicy?> GetBookingPolicyAsync(int bookingId);
}
```

### ICertificateGenerator
```csharp
public interface ICertificateGenerator
{
    // Generate certificate PDF for a booking
    Task<string?> GenerateCertificateAsync(int bookingId);
}
```

## Configuration by Country

### Ghana 🇬🇭
```json
{
  "CountryCode": "GH",
  "RequiresRealInsurance": false,
  "InsuranceProviderName": "Ghana Protection Plan",
  "Provider": "MockInsuranceProvider",
  "Premium": "$0",
  "Coverage": "$500,000",
  "Notes": "Uses existing protection plan system"
}
```

### Nigeria 🇳🇬
```json
{
  "CountryCode": "NG",
  "RequiresRealInsurance": false,
  "InsuranceProviderName": "Nigeria Protection Plan",
  "Provider": "MockInsuranceProvider",
  "Premium": "$0",
  "Coverage": "₦20,000,000",
  "Notes": "Ready for real provider integration"
}
```

### Kenya 🇰🇪
```json
{
  "CountryCode": "KE",
  "RequiresRealInsurance": false,
  "InsuranceProviderName": "Kenya Protection Plan",
  "Provider": "MockInsuranceProvider",
  "Premium": "$0",
  "Coverage": "KES 50,000,000",
  "Notes": "Ready for real provider integration"
}
```

### Canada 🇨🇦
```json
{
  "CountryCode": "CA",
  "RequiresRealInsurance": true,
  "InsuranceProviderName": "Intact Insurance",
  "Provider": "CanadaInsuranceProvider",
  "Premium": "$15-25 CAD per day",
  "Coverage": "$2,000,000 CAD",
  "Notes": "Currently simulated, needs real API integration",
  "Regulations": "FSRA compliant, meets Ontario minimum requirements"
}
```

## Adding a New Country with Insurance

### Step 1: Add Country to Database
```sql
INSERT INTO Countries (Code, Name, Currency, CurrencySymbol, PhoneCode, Timezone, Status)
VALUES ('ZA', 'South Africa', 'ZAR', 'R', '+27', 'Africa/Johannesburg', 'inactive');
```

### Step 2: Configure Insurance Requirements
```sql
INSERT INTO CountryInsuranceConfig 
(CountryCode, RequiresRealInsurance, InsuranceProviderName, MinimumLiabilityAmount)
VALUES 
('ZA', true, 'Santam Insurance', 1000000); -- R1M liability
```

### Step 3: Create Provider (if needed)
```csharp
// Services/Insurance/Providers/SouthAfricaInsuranceProvider.cs
public class SouthAfricaInsuranceProvider : IInsuranceProvider
{
    public async Task<PolicyResponse?> CreatePolicyAsync(PolicyRequest request)
    {
        // Call Santam API
        // ...
    }
    // ... implement other methods
}
```

### Step 4: Register Provider
```csharp
// Program.cs
builder.Services.AddHttpClient<SouthAfricaInsuranceProvider>();
```

### Step 5: Update Factory Routing
```csharp
// InsuranceProviderFactory.cs
public IInsuranceProvider GetProvider(string countryCode)
{
    return countryCode.ToUpper() switch
    {
        "GH" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
        "NG" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
        "KE" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
        "CA" => _serviceProvider.GetRequiredService<CanadaInsuranceProvider>(),
        "ZA" => _serviceProvider.GetRequiredService<SouthAfricaInsuranceProvider>(), // NEW
        _ => _serviceProvider.GetRequiredService<MockInsuranceProvider>()
    };
}
```

### Step 6: Test
1. Create test booking in new country
2. Verify correct provider used
3. Check policy created in database
4. Verify certificate generated
5. Test cancellation flow

## API Response Examples

### Successful Booking with Insurance
```json
{
  "id": 123,
  "bookingReference": "BK-2026-ABC123",
  "vehicleId": 456,
  "status": "reserved",
  "totalAmount": 350.00,
  "insuranceCertificateUrl": "https://api.ryverental.com/certificates/certificate-123-20260115.html",
  "createdAt": "2026-01-15T10:30:00Z",
  ...
}
```

### Insurance Policy Record
```json
{
  "id": 789,
  "bookingId": 123,
  "policyNumber": "IC-2026-X7Y8Z9",
  "insuranceProviderName": "Intact Insurance",
  "coverageStartDate": "2026-01-20T00:00:00Z",
  "coverageEndDate": "2026-01-25T00:00:00Z",
  "premiumAmount": 75.00,
  "liabilityCoverage": 2000000.00,
  "certificateUrl": "https://api.ryverental.com/certificates/certificate-123-20260115.html",
  "status": "issued"
}
```

## Environment Variables

### Required Configuration
```bash
# Base URL for certificate links
AppSettings__BaseUrl=https://api.ryverental.com

# Canada Insurance (Production)
Insurance__Canada__ApiKey=your_intact_api_key
Insurance__Canada__ApiUrl=https://api.intact.ca/v1
Insurance__Canada__Enabled=true

# Development/Testing
Insurance__UseMockProviders=true  # Override all to use mock
```

## Certificate Template Customization

### Current Template Structure
```
Certificate Header
├── Title: "CERTIFICATE OF INSURANCE"
├── Subtitle: "Rental Vehicle Protection Coverage"
└── Policy Number (highlighted)

Sections:
├── Booking Information
│   ├── Booking Reference
│   ├── Insurance Provider
│   └── Country
│
├── Insured Party
│   ├── Name
│   └── Email
│
├── Vehicle Information
│   ├── Vehicle Details
│   └── License Plate
│
└── Coverage Details
    ├── Protection Plan
    ├── Coverage Dates
    ├── Liability Amount
    └── Deductible

Footer
├── Issuance Date/Time
├── Validity Statement
└── Contact Information
```

### Customizing for Countries
Update `CertificateGenerator.GenerateCertificateHtml()`:
- Add country-specific branding
- Include regulatory compliance statements
- Add local language translations
- Include emergency contact numbers
- Add QR codes for verification

## Monitoring & Observability

### Key Metrics to Track
```csharp
// Insurance creation success rate
insurance_policies_created_total
insurance_policies_failed_total

// Provider-specific metrics
insurance_provider_calls_total{provider="mock"}
insurance_provider_calls_total{provider="canada"}
insurance_provider_duration_seconds

// Certificate generation
certificates_generated_total
certificates_failed_total
certificate_generation_duration_seconds

// Cost tracking
insurance_premium_total{country="CA"}
insurance_premium_total{country="GH"}
```

### Alert Conditions
- Insurance creation failure rate > 5%
- Certificate generation failure rate > 2%
- Provider API response time > 5 seconds
- Missing insurance config for active country
- Premium calculation mismatch

## Testing Scenarios

### Unit Tests
```csharp
// Mock Provider Tests
[Fact]
public async Task MockProvider_CreatesPolicy_WithZeroPremium()
[Fact]
public async Task MockProvider_GeneratesPolicyNumber_InCorrectFormat()

// Canada Provider Tests
[Fact]
public async Task CanadaProvider_CalculatesPremium_ForBasicPlan()
[Fact]
public async Task CanadaProvider_MeetsMinimumLiability_Requirement()

// Orchestrator Tests
[Fact]
public async Task Orchestrator_UsesCorrectProvider_ForCountry()
[Fact]
public async Task Orchestrator_GeneratesCertificate_AfterPolicyCreation()

// Factory Tests
[Fact]
public void Factory_ReturnsCanadaProvider_ForCanadaBooking()
[Fact]
public void Factory_ReturnsMockProvider_ForGhanaBooking()
```

### Integration Tests
```csharp
[Fact]
public async Task CreateBooking_GeneratesInsurancePolicy_ForGhana()
[Fact]
public async Task CreateBooking_GeneratesInsurancePolicy_ForCanada()
[Fact]
public async Task CancelBooking_CancelsInsurancePolicy()
```

## Troubleshooting Guide

### Problem: No certificate URL in booking
**Diagnosis**:
```sql
SELECT Id, BookingReference, InsuranceCertificateUrl 
FROM Bookings 
WHERE Id = [bookingId];

SELECT * FROM InsurancePolicies WHERE BookingId = [bookingId];
```
**Solutions**:
1. Check logs for CertificateGenerator errors
2. Verify wwwroot/certificates folder exists
3. Check file permissions
4. Verify AppSettings:BaseUrl configured

### Problem: Wrong insurance provider used
**Diagnosis**:
```sql
SELECT b.Id, b.BookingReference, c.Code, cic.RequiresRealInsurance
FROM Bookings b
JOIN Cities ct ON b.CityId = ct.Id
JOIN Countries c ON ct.CountryCode = c.Code
LEFT JOIN CountryInsuranceConfig cic ON c.Code = cic.CountryCode
WHERE b.Id = [bookingId];
```
**Solutions**:
1. Verify CountryInsuranceConfig exists for country
2. Check InsuranceProviderFactory routing logic
3. Verify country code in booking

### Problem: Premium not calculated
**Diagnosis**: Check provider implementation
**Solutions**:
1. Verify provider's CreatePolicyAsync calculates premium
2. Check protection plan pricing
3. Verify currency conversion if needed

## Cost Breakdown

### Ghana Operations (No Change)
```
Booking Total: $100
├── Vehicle Rental: $80
├── Protection Plan: $20 (included in rental)
└── Insurance Premium: $0 (mock provider)

Certificate Cost: ~$0.01 (storage)
Total Additional Cost: $0.01 per booking
```

### Canada Operations (Estimated)
```
Booking Total: $250 CAD
├── Vehicle Rental: $150
├── Protection Plan: $50
└── Insurance Premium: $50 (real insurance)
    ├── Base Premium: $15/day × 3 days = $45
    └── Coverage Enhancement: $5

Certificate Cost: ~$0.01 (storage)
API Call Cost: ~$0.10
Total Additional Cost: $50.11 per booking

Revenue Model:
├── Pass-through: Charge customer exactly $50
├── Markup 10%: Charge customer $55, profit $5
└── Bundled: Include in protection plan price
```

## Security Considerations

### API Keys
- Store in Azure Key Vault or AWS Secrets Manager
- Never commit to source control
- Rotate regularly (quarterly recommended)

### Certificate Access
- Certificates contain sensitive renter information
- Consider adding authentication for certificate URLs
- Set expiration dates (auto-delete after 30 days)

### Provider APIs
- Use HTTPS only
- Implement rate limiting
- Add request signing if supported
- Log all API calls for audit

### PII Handling
- Certificate contains: Name, Email, License Plate
- Ensure GDPR/privacy compliance
- Add data retention policy
- Implement "right to erasure"

## Performance Optimization

### Async Processing
- Insurance processing runs in background (non-blocking)
- Certificate generation doesn't delay booking response
- Failures logged but don't fail booking

### Caching
```csharp
// Cache insurance config per country (1 hour)
var config = _cache.GetOrCreateAsync(
    $"insurance_config_{countryCode}",
    async entry => {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await _db.CountryInsuranceConfigs.FirstOrDefaultAsync(...);
    }
);
```

### Database Indexes
```sql
-- Optimize insurance policy lookups
CREATE INDEX idx_insurance_policies_booking 
ON InsurancePolicies(BookingId);

CREATE INDEX idx_insurance_policies_policy_number 
ON InsurancePolicies(PolicyNumber);

CREATE INDEX idx_country_insurance_config_country 
ON CountryInsuranceConfig(CountryCode);
```

---

## Quick Commands

### Check Insurance Status
```sql
-- Bookings with insurance in last 24 hours
SELECT b.Id, b.BookingReference, ip.PolicyNumber, ip.PremiumAmount
FROM Bookings b
JOIN InsurancePolicies ip ON b.Id = ip.BookingId
WHERE b.CreatedAt > NOW() - INTERVAL '24 hours'
ORDER BY b.CreatedAt DESC;
```

### Regenerate Certificate (Manual)
```csharp
// In admin panel or API endpoint
await _certificateGenerator.GenerateCertificateAsync(bookingId);
```

### Switch Provider for Testing
```sql
-- Temporarily use mock for Canada (testing)
UPDATE CountryInsuranceConfig 
SET RequiresRealInsurance = false 
WHERE CountryCode = 'CA';

-- Restore to real provider
UPDATE CountryInsuranceConfig 
SET RequiresRealInsurance = true 
WHERE CountryCode = 'CA';
```

---

**Last Updated**: January 2026  
**Version**: 1.0  
**Status**: Production Ready (Ghana), Integration Needed (Canada)
