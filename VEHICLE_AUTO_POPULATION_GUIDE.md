# Vehicle Auto-Population API Guide

## 🚗 Overview

The Vehicle Auto-Population feature allows owners and admins to automatically fetch vehicle specifications, features, and standard inclusions by simply providing the year, make, and model. This eliminates manual data entry and ensures consistent, accurate vehicle information.

## 📡 API Endpoint

### **Lookup Vehicle Data**

```http
GET /api/v1/owner/vehicles/lookup/{year}/{make}/{model}?trim={trim}
```

**Authorization:** Owner or Admin token required

**Path Parameters:**
- `year` (required): Vehicle year (e.g., 2020)
- `make` (required): Manufacturer name (e.g., "Toyota")
- `model` (required): Model name (e.g., "Camry")

**Query Parameters:**
- `trim` (optional): Trim level (e.g., "LE", "XLE", "Sport")

**Example Request:**
```http
GET /api/v1/owner/vehicles/lookup/2020/Toyota/Camry?trim=LE
Authorization: Bearer {owner_token}
```

**Example Response:**
```json
{
  "year": 2020,
  "make": "Toyota",
  "model": "Camry",
  "trim": "LE",
  "fuelType": "Petrol",
  "transmissionType": "Automatic (CVT)",
  "seatingCapacity": 5,
  "features": [
    "Air Conditioning",
    "Bluetooth Audio",
    "USB Charging Port",
    "Backup Camera",
    "Keyless Entry",
    "Power Windows",
    "Power Locks",
    "ABS Brakes",
    "Cruise Control"
  ],
  "specifications": {
    "Engine Size": "2.0L - 2.5L",
    "Fuel Type": "Petrol",
    "Fuel Efficiency": "12-17 km/L",
    "Transmission": "Automatic (CVT)",
    "Drivetrain": "FWD",
    "Body Style": "Sedan",
    "Seating Capacity": "5"
  },
  "inclusions": {
    "mileageAllowancePerDay": 600,
    "extraKmRate": 0.30,
    "currency": "GHS",
    "protectionPlanRequired": true,
    "roadsideAssistance": "Depends on selected protection plan",
    "cancellationPolicy": "Free cancellation with 48hrs notice"
  },
  "message": "Owner can override any of these values when creating/updating the vehicle"
}
```

## 🗄️ Database Schema

### New Vehicle Fields

```sql
-- JSON fields
FeaturesJson TEXT NULL           -- ["Air Conditioning", "Bluetooth", ...]
SpecificationsJson TEXT NULL     -- {"engineSize": "1.5L", "fuelEfficiency": "15-17 km/L"}
InclusionsJson TEXT NULL         -- Rental policies/inclusions

-- Extracted fields
TransmissionType VARCHAR(50) NULL  -- Manual, Automatic, CVT

-- Mileage override fields (NULL = use global default)
MileageAllowancePerDay INTEGER NULL
ExtraKmRate DECIMAL(10,2) NULL
```

### Global Settings (Admin Configurable)

```sql
INSERT INTO "GlobalSettings" ("Key", "ValueJson", "Description")
VALUES 
    ('Vehicle:DefaultMileageAllowancePerDay', '600', 'Default daily km allowance'),
    ('Vehicle:DefaultExtraKmRate', '0.30', 'Default rate per extra km in GHS');
```

## 🔄 Frontend Integration Workflow

### 1. **Owner Enters Basic Info**
```javascript
const vehicleForm = {
  year: 2020,
  make: "Toyota",
  model: "Camry",
  trim: "LE" // optional
};
```

### 2. **Fetch Auto-Populated Data**
```javascript
// Call API when make/model is entered
const response = await fetch(
  `/api/v1/owner/vehicles/lookup/${vehicleForm.year}/${vehicleForm.make}/${vehicleForm.model}?trim=${vehicleForm.trim}`,
  {
    headers: { 'Authorization': `Bearer ${ownerToken}` }
  }
);

const autoData = await response.json();
```

### 3. **Pre-Fill Form with Checkboxes**
```javascript
// Pre-fill features as checkboxes (owner can toggle)
autoData.features.forEach(feature => {
  document.getElementById(`feature-${feature}`).checked = true;
});

// Pre-fill specifications (owner can edit)
document.getElementById('engineSize').value = autoData.specifications['Engine Size'];
document.getElementById('fuelEfficiency').value = autoData.specifications['Fuel Efficiency'];

// Pre-fill mileage (owner can override)
document.getElementById('mileageAllowance').value = autoData.inclusions.mileageAllowancePerDay;
document.getElementById('extraKmRate').value = autoData.inclusions.extraKmRate;
```

### 4. **Owner Reviews and Adjusts**
- **Features:** Owner can check/uncheck features
- **Specifications:** Owner can edit any field
- **Mileage:** Owner can override the global defaults
- **Daily Rate:** Owner sets pricing

### 5. **Submit Vehicle**
```javascript
const vehicleData = {
  year: 2020,
  make: "Toyota",
  model: "Camry",
  plateNumber: "GH-1234-20",
  categoryId: "economy-sedan-guid",
  
  // Auto-populated (can be modified)
  fuelType: "Petrol",
  transmissionType: "Automatic",
  seatingCapacity: 5,
  
  // Features array (selected checkboxes)
  featuresJson: JSON.stringify([
    "Air Conditioning",
    "Bluetooth Audio",
    "USB Charging Port"
  ]),
  
  // Specifications object
  specificationsJson: JSON.stringify({
    "Engine Size": "2.5L",
    "Fuel Efficiency": "13-16 km/L",
    "Transmission": "Automatic (CVT)"
  }),
  
  // Inclusions (with owner overrides)
  inclusionsJson: JSON.stringify({
    "mileageAllowancePerDay": 800, // Owner increased from 600
    "extraKmRate": 0.25 // Owner reduced from 0.30
  }),
  
  // Pricing
  dailyRate: 150.00,
  
  // Documents
  insuranceDocumentUrl: "https://...",
  roadworthinessDocumentUrl: "https://..."
};

await fetch('/api/v1/owner/vehicles', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${ownerToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(vehicleData)
});
```

## 🎯 Data Sources

### NHTSA vPIC API (Primary)
- **URL:** `https://vpic.nhtsa.dot.gov/api/vehicles/`
- **Cost:** FREE
- **Rate Limit:** None
- **Coverage:** US market vehicles (most common in Ghana)

### Intelligent Inference (Fallback)
When API fails or vehicle not found:
- **Engine Size:** Inferred from model type (compact, SUV, truck)
- **Fuel Type:** Inferred from model name (hybrid, diesel, EV keywords)
- **Transmission:** Year-based inference (2018+ = Automatic CVT)
- **Features:** Year-based standard features (2015+ = Bluetooth)
- **Fuel Efficiency:** Estimated from body style and engine size

## ✨ Auto-Detected Features by Year

| Year Range | Standard Features |
|------------|------------------|
| 2000+ | Power Steering, Power Windows |
| 2005+ | Air Conditioning, CD Player/Radio |
| 2010+ | Power Locks, ABS Brakes |
| 2015+ | **Bluetooth Audio**, **USB Charging**, Aux Input |
| 2018+ | Backup Camera, Keyless Entry |
| 2020+ | Apple CarPlay/Android Auto, Lane Departure Warning |

**Premium Trim Bonus:** Leather Seats, Sunroof, Cruise Control

## 📝 What's Included - Standard Policies

These are automatically added to all vehicles:

1. **Mileage Allowance:** 600 km/day (globally configurable, owner can override)
2. **Extra KM Rate:** GHS 0.30/km (globally configurable, owner can override)
3. **Protection Plan:** Required (user selects during booking)
4. **Roadside Assistance:** Depends on selected protection plan
5. **Cancellation Policy:** Free cancellation with 48hrs notice

**Note:** Changed "Insurance" to "Protection Plan" as requested.

## 🔒 Owner Override Capabilities

Owners can override ANY auto-populated field:

- ✅ Add/remove features
- ✅ Edit specifications
- ✅ Change mileage allowance
- ✅ Adjust extra km rate
- ✅ Modify fuel type/transmission

**Example:**
```javascript
// API suggests: 600 km/day @ GHS 0.30/km
// Owner can change to: 800 km/day @ GHS 0.25/km

mileageAllowancePerDay: 800,
extraKmRate: 0.25
```

## 🛠️ Admin Global Settings

Admins can configure global defaults via `GlobalSettings` table:

```sql
-- Update global mileage default
UPDATE "GlobalSettings"
SET "ValueJson" = '800'
WHERE "Key" = 'Vehicle:DefaultMileageAllowancePerDay';

-- Update global extra km rate
UPDATE "GlobalSettings"
SET "ValueJson" = '0.25'
WHERE "Key" = 'Vehicle:DefaultExtraKmRate';
```

## 🚀 Deployment Steps

### 1. Run Database Migration
```bash
# Apply the migration
dotnet ef database update --context AppDbContext
```

Or run the SQL script directly:
```bash
psql -h ryve-postgres-new.postgres.database.azure.com \
     -U ryveadmin \
     -d ghanarentaldb \
     -f add-vehicle-auto-population-fields.sql
```

### 2. Build and Deploy
```bash
# Build new version
dotnet build
docker build -t ghanarentalapi:1.202 .

# Deploy to Azure
docker tag ghanarentalapi:1.202 ryveacrnewawjs.azurecr.io/ghanarentalapi:1.202
docker push ryveacrnewawjs.azurecr.io/ghanarentalapi:1.202

# Update container
az container delete --name ghanarentalapi --resource-group ryve-prod-new --yes
az container create [... with version 1.202 ...]
```

## 📊 Example Use Cases

### Use Case 1: Owner Adds Toyota Camry 2020
1. Owner enters: Year=2020, Make=Toyota, Model=Camry
2. Frontend calls lookup API
3. System returns:
   - 9 standard features
   - Specifications (2.5L, 13-16 km/L, Automatic)
   - 600 km/day allowance
4. Owner reviews, adds "Sunroof", changes to 700 km/day
5. Saves vehicle

### Use Case 2: Admin Updates Global Mileage
1. Admin changes global default from 600 to 800 km/day
2. All NEW vehicles get 800 km/day
3. Existing vehicles keep their saved values
4. Owners can still override per vehicle

### Use Case 3: API Fails - Fallback
1. Owner enters: Year=2021, Make=BYD, Model=Song (Chinese brand)
2. NHTSA API has no data
3. System uses intelligent inference:
   - SUV body style → 2.0L-2.5L engine
   - 2021 → Modern features (Bluetooth, Backup Camera)
   - Estimates fuel efficiency: 12-15 km/L
4. Owner reviews and confirms/edits

## 🎨 Frontend UI Recommendations

### Features Section (Checkboxes)
```html
<div class="features-grid">
  <label><input type="checkbox" checked> Air Conditioning</label>
  <label><input type="checkbox" checked> Bluetooth Audio</label>
  <label><input type="checkbox" checked> USB Charging Port</label>
  <label><input type="checkbox"> Sunroof (owner added)</label>
</div>
```

### Specifications Section (Editable)
```html
<div class="specs-fields">
  <input type="text" value="2.5L" placeholder="Engine Size" />
  <input type="text" value="13-16 km/L" placeholder="Fuel Efficiency" />
  <select><option selected>Automatic (CVT)</option></select>
</div>
```

### Mileage Section (Override Defaults)
```html
<div class="mileage-override">
  <label>
    Daily Allowance:
    <input type="number" value="600" min="100" max="2000" />
    <small>Global default: 600 km</small>
  </label>
  <label>
    Extra KM Rate (GHS):
    <input type="number" step="0.01" value="0.30" />
    <small>Global default: GHS 0.30/km</small>
  </label>
</div>
```

## ✅ Summary

This feature provides:
- ✅ Auto-fetch vehicle data from NHTSA API
- ✅ Intelligent fallback with inference
- ✅ 45+ features auto-detected by year/trim
- ✅ Owner can override ALL values
- ✅ Global admin control of mileage defaults
- ✅ Per-vehicle mileage override capability
- ✅ Uses "Protection Plan" terminology
- ✅ Checkbox-based feature selection
- ✅ Zero API keys required (NHTSA is free)

Ready to test! 🚀
