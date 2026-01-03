# Vehicle Auto-Population & Enhanced Endpoints - Frontend Integration Guide

## 📋 Table of Contents
1. [Overview](#overview)
2. [Vehicle Data Lookup API](#vehicle-data-lookup-api)
3. [Enhanced Vehicle Search API](#enhanced-vehicle-search-api)
4. [Enhanced Vehicle Details API](#enhanced-vehicle-details-api)
5. [Admin Vehicle Approval API](#admin-vehicle-approval-api)
6. [Frontend Implementation Examples](#frontend-implementation-examples)
7. [Data Structures](#data-structures)

---

## Overview

The vehicle management system now includes automatic population of vehicle features, specifications, and inclusions based on year, make, and model. All vehicle endpoints have been enhanced to return comprehensive data including:

- ✅ **Auto-populated features** (e.g., Air Conditioning, Bluetooth)
- ✅ **Technical specifications** (engine size, fuel efficiency, etc.)
- ✅ **Rental inclusions** (mileage allowances, policies)
- ✅ **All vehicle photos** (absolute URLs)
- ✅ **Mileage settings** (per-vehicle overrides or global defaults)

---

## Vehicle Data Lookup API

### **Endpoint**: Auto-Populate Vehicle Data

Fetch vehicle specifications, features, and inclusions based on year/make/model.

```http
GET /api/v1/owner/vehicles/lookup/{year}/{make}/{model}?trim={trim}
Authorization: Bearer {owner_token}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `year` | integer | Yes | Vehicle year (1900-2027) |
| `make` | string | Yes | Manufacturer (e.g., "Toyota") |
| `model` | string | Yes | Model name (e.g., "Camry") |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `trim` | string | No | Trim level (e.g., "LE", "XLE") |

### **Sample Request**

```javascript
// JavaScript/TypeScript
const response = await fetch(
  '/api/v1/owner/vehicles/lookup/2020/Toyota/Camry?trim=LE',
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${ownerToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const vehicleData = await response.json();
```

### **Sample Response**

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

### **Frontend Usage**

```javascript
// 1. User enters year, make, model
const vehicleForm = {
  year: 2020,
  make: "Toyota",
  model: "Camry",
  trim: "LE"
};

// 2. Fetch auto-populated data
const lookupData = await fetchVehicleLookup(
  vehicleForm.year,
  vehicleForm.make,
  vehicleForm.model,
  vehicleForm.trim
);

// 3. Pre-fill form with checkboxes for features
lookupData.features.forEach(feature => {
  const checkbox = document.getElementById(`feature-${feature}`);
  if (checkbox) checkbox.checked = true;
});

// 4. Pre-fill specifications (editable)
document.getElementById('engineSize').value = 
  lookupData.specifications['Engine Size'];
document.getElementById('fuelEfficiency').value = 
  lookupData.specifications['Fuel Efficiency'];

// 5. Pre-fill mileage (owner can override)
document.getElementById('mileageAllowance').value = 
  lookupData.inclusions.mileageAllowancePerDay;
document.getElementById('extraKmRate').value = 
  lookupData.inclusions.extraKmRate;

// 6. Owner reviews and can modify any field before saving
```

---

## Enhanced Vehicle Search API

### **Endpoint**: Search Available Vehicles

Public endpoint to search and browse available vehicles with full details.

```http
GET /api/v1/vehicles
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `categoryId` | UUID | No | Filter by category |
| `cityId` | UUID | No | Filter by city |
| `transmission` | string | No | manual/automatic |
| `minSeats` | integer | No | Minimum seating capacity |
| `hasAC` | boolean | No | Air conditioning required |
| `maxDailyRate` | decimal | No | Maximum daily rate (GHS) |
| `availableFrom` | datetime | No | Check availability from |
| `availableUntil` | datetime | No | Check availability until |
| `withDriver` | boolean | No | Requires driver |
| `page` | integer | No | Page number (default: 1) |
| `pageSize` | integer | No | Items per page (default: 50) |

### **Sample Request**

```javascript
// Search for available SUVs in Accra
const searchParams = new URLSearchParams({
  cityId: 'accra-city-id',
  categoryId: 'suv-category-id',
  minSeats: 5,
  hasAC: true,
  maxDailyRate: 200,
  availableFrom: '2025-01-05T10:00:00Z',
  availableUntil: '2025-01-10T10:00:00Z',
  page: 1,
  pageSize: 20
});

const response = await fetch(`/api/v1/vehicles?${searchParams}`);
const vehicles = await response.json();
```

### **Sample Response**

```json
{
  "success": true,
  "total": 15,
  "page": 1,
  "pageSize": 20,
  "hasResults": true,
  "message": null,
  "searchDates": {
    "from": "2025-01-05T10:00:00Z",
    "until": "2025-01-10T10:00:00Z"
  },
  "data": [
    {
      "id": "2fb6ef89-3f28-42bf-a98f-0476916f658f",
      "plateNumber": "GH-1234-20",
      "make": "Toyota",
      "model": "RAV4",
      "year": 2021,
      "transmission": "automatic",
      "fuelType": "Petrol",
      "seatingCapacity": 5,
      "hasAC": true,
      "status": "active",
      "isAvailable": true,
      
      "transmissionType": "Automatic",
      "features": [
        "Air Conditioning",
        "Bluetooth Audio",
        "USB Charging Port",
        "Backup Camera",
        "Keyless Entry",
        "Apple CarPlay/Android Auto",
        "Lane Departure Warning",
        "Power Windows",
        "Power Locks",
        "ABS Brakes"
      ],
      "specifications": {
        "Engine Size": "2.0L - 2.5L",
        "Fuel Type": "Petrol",
        "Fuel Efficiency": "12-17 km/L",
        "Transmission": "Automatic",
        "Drivetrain": "FWD",
        "Body Style": "SUV",
        "Seating Capacity": "5"
      },
      "inclusions": {
        "mileageAllowancePerDay": 700,
        "extraKmRate": 0.28,
        "currency": "GHS",
        "protectionPlanRequired": true,
        "roadsideAssistance": "Depends on selected protection plan",
        "cancellationPolicy": "Free cancellation with 48hrs notice"
      },
      "mileageAllowancePerDay": 700,
      "extraKmRate": 0.28,
      
      "cityId": "accra-city-id",
      "city": {
        "id": "accra-city-id",
        "name": "Accra"
      },
      "category": {
        "id": "suv-category-id",
        "name": "SUV",
        "description": "Sport Utility Vehicles",
        "defaultDailyRate": 180.00,
        "defaultDepositAmount": 500.00,
        "requiresDriver": false
      },
      "mileageTerms": {
        "enabled": true,
        "includedKilometers": 700,
        "pricePerExtraKm": 0.28,
        "currency": "GHS"
      },
      "photos": [
        "https://api.ryverental.com/uploads/rav4-front.jpg",
        "https://api.ryverental.com/uploads/rav4-interior.jpg",
        "https://api.ryverental.com/uploads/rav4-back.jpg",
        "https://api.ryverental.com/uploads/rav4-side.jpg"
      ],
      "insuranceDocumentUrl": "https://api.ryverental.com/uploads/rav4-insurance.pdf",
      "roadworthinessDocumentUrl": "https://api.ryverental.com/uploads/rav4-roadworthy.pdf",
      "dailyRate": 180.00,
      "vehicleDailyRate": null
    }
  ]
}
```

### **Frontend Usage - Vehicle Cards**

```jsx
// React Component Example
function VehicleCard({ vehicle }) {
  return (
    <div className="vehicle-card">
      {/* Image Gallery */}
      <div className="vehicle-images">
        <img 
          src={vehicle.photos[0]} 
          alt={`${vehicle.make} ${vehicle.model}`}
          className="main-image"
        />
        <div className="thumbnail-strip">
          {vehicle.photos.slice(1, 4).map((photo, i) => (
            <img key={i} src={photo} alt={`View ${i + 2}`} />
          ))}
        </div>
      </div>
      
      {/* Vehicle Info */}
      <div className="vehicle-info">
        <h3>{vehicle.year} {vehicle.make} {vehicle.model}</h3>
        
        {/* Key Features */}
        <div className="features-icons">
          {vehicle.features.includes("Air Conditioning") && 
            <span title="Air Conditioning">❄️</span>}
          {vehicle.features.includes("Bluetooth Audio") && 
            <span title="Bluetooth">📻</span>}
          {vehicle.features.includes("Backup Camera") && 
            <span title="Backup Camera">📷</span>}
          {vehicle.features.includes("Apple CarPlay/Android Auto") && 
            <span title="CarPlay/Android Auto">📱</span>}
        </div>
        
        {/* Quick Specs */}
        <div className="quick-specs">
          <span>🚗 {vehicle.specifications["Seating Capacity"]} seats</span>
          <span>⚡ {vehicle.specifications["Fuel Efficiency"]}</span>
          <span>⚙️ {vehicle.transmissionType}</span>
        </div>
        
        {/* Pricing */}
        <div className="pricing">
          <div className="daily-rate">
            <strong>GHS {vehicle.dailyRate}</strong>
            <small>/day</small>
          </div>
          <div className="mileage-info">
            <small>
              {vehicle.mileageAllowancePerDay} km included 
              (+GHS {vehicle.extraKmRate}/km)
            </small>
          </div>
        </div>
        
        <button className="btn-view-details">View Details</button>
      </div>
    </div>
  );
}
```

---

## Enhanced Vehicle Details API

### **Endpoint**: Get Vehicle Details

Public endpoint to get complete vehicle information.

```http
GET /api/v1/vehicles/{vehicleId}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `vehicleId` | UUID | Yes | Vehicle ID |

### **Sample Request**

```javascript
const vehicleId = '2fb6ef89-3f28-42bf-a98f-0476916f658f';
const response = await fetch(`/api/v1/vehicles/${vehicleId}`);
const vehicle = await response.json();
```

### **Sample Response**

```json
{
  "id": "2fb6ef89-3f28-42bf-a98f-0476916f658f",
  "plateNumber": "GH-1234-20",
  "make": "Toyota",
  "model": "Camry",
  "year": 2020,
  "transmission": "automatic",
  "fuelType": "Petrol",
  "seatingCapacity": 5,
  "hasAC": true,
  "status": "active",
  "cityId": "accra-city-id",
  
  "transmissionType": "Automatic (CVT)",
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
    "Engine Size": "2.5L",
    "Fuel Type": "Petrol",
    "Fuel Efficiency": "13-16 km/L",
    "Transmission": "Automatic (CVT)",
    "Drivetrain": "FWD",
    "Body Style": "Sedan",
    "Seating Capacity": "5"
  },
  "inclusions": {
    "mileageAllowancePerDay": 800,
    "extraKmRate": 0.25,
    "currency": "GHS",
    "protectionPlanRequired": true,
    "roadsideAssistance": "Depends on selected protection plan",
    "cancellationPolicy": "Free cancellation with 48hrs notice"
  },
  "mileageAllowancePerDay": 800,
  "extraKmRate": 0.25,
  
  "city": {
    "id": "accra-city-id",
    "name": "Accra"
  },
  "category": {
    "id": "sedan-category-id",
    "name": "Sedan",
    "description": "Comfortable mid-size sedans",
    "defaultDailyRate": 150.00,
    "defaultDepositAmount": 400.00,
    "requiresDriver": false
  },
  "dailyRate": 150.00,
  "vehicleDailyRate": null,
  "mileageTerms": {
    "enabled": true,
    "includedKilometers": 800,
    "pricePerExtraKm": 0.25,
    "currency": "GHS"
  },
  "owner": {
    "displayName": "Accra Premium Rentals",
    "ownerType": "business"
  },
  "completedBookingsCount": 42,
  "photos": [
    "https://api.ryverental.com/uploads/camry-front.jpg",
    "https://api.ryverental.com/uploads/camry-interior.jpg",
    "https://api.ryverental.com/uploads/camry-dashboard.jpg",
    "https://api.ryverental.com/uploads/camry-trunk.jpg",
    "https://api.ryverental.com/uploads/camry-side.jpg"
  ],
  "insuranceDocumentUrl": "https://api.ryverental.com/uploads/camry-insurance.pdf",
  "roadworthinessDocumentUrl": "https://api.ryverental.com/uploads/camry-roadworthy.pdf"
}
```

### **Frontend Usage - Vehicle Details Page**

```jsx
// React Component Example
function VehicleDetailsPage({ vehicleId }) {
  const [vehicle, setVehicle] = useState(null);
  
  useEffect(() => {
    fetch(`/api/v1/vehicles/${vehicleId}`)
      .then(res => res.json())
      .then(setVehicle);
  }, [vehicleId]);
  
  if (!vehicle) return <Loading />;
  
  return (
    <div className="vehicle-details-page">
      {/* Photo Gallery */}
      <section className="photo-gallery">
        <ImageGallery images={vehicle.photos} />
      </section>
      
      {/* Vehicle Header */}
      <section className="vehicle-header">
        <h1>{vehicle.year} {vehicle.make} {vehicle.model}</h1>
        <div className="owner-badge">
          <span>{vehicle.owner.displayName}</span>
          <span className="verified">✓ Verified</span>
          <span className="trips">{vehicle.completedBookingsCount} trips</span>
        </div>
      </section>
      
      {/* Features Section */}
      <section className="features-section">
        <h2>Features</h2>
        <div className="features-grid">
          {vehicle.features.map(feature => (
            <div key={feature} className="feature-item">
              <span className="checkmark">✅</span>
              <span>{feature}</span>
            </div>
          ))}
        </div>
      </section>
      
      {/* Specifications Section */}
      <section className="specifications-section">
        <h2>Specifications</h2>
        <table className="specs-table">
          <tbody>
            {Object.entries(vehicle.specifications).map(([key, value]) => (
              <tr key={key}>
                <td className="spec-label">{key}</td>
                <td className="spec-value">{value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      
      {/* What's Included Section */}
      <section className="inclusions-section">
        <h2>What's Included</h2>
        <ul className="inclusions-list">
          <li>
            <strong>{vehicle.inclusions.mileageAllowancePerDay} km</strong> 
            included per day 
            <span className="extra-rate">
              (+GHS {vehicle.inclusions.extraKmRate}/km extra)
            </span>
          </li>
          <li>Protection Plan Coverage (select during booking)</li>
          <li>{vehicle.inclusions.roadsideAssistance}</li>
          <li>{vehicle.inclusions.cancellationPolicy}</li>
        </ul>
      </section>
      
      {/* Pricing & Booking */}
      <section className="pricing-booking">
        <div className="price-card">
          <div className="daily-rate">
            <span className="amount">GHS {vehicle.dailyRate}</span>
            <span className="period">/day</span>
          </div>
          <button className="btn-book-now">Book Now</button>
        </div>
      </section>
    </div>
  );
}
```

---

## Admin Vehicle Approval API

### **Endpoint**: Get Pending Vehicles

Admin endpoint to review vehicles pending approval.

```http
GET /api/v1/admin/vehicles/pending
Authorization: Bearer {admin_token}
```

### **Sample Request**

```javascript
const response = await fetch('/api/v1/admin/vehicles/pending', {
  headers: {
    'Authorization': `Bearer ${adminToken}`
  }
});

const pendingVehicles = await response.json();
```

### **Sample Response**

```json
{
  "total": 3,
  "data": [
    {
      "id": "vehicle-id-123",
      "plateNumber": "GH-5678-21",
      "make": "Honda",
      "model": "Civic",
      "year": 2021,
      "transmission": "automatic",
      "fuelType": "Petrol",
      "seatingCapacity": 5,
      "hasAC": true,
      "status": "pending_review",
      
      "transmissionType": "Automatic",
      "features": [
        "Air Conditioning",
        "Bluetooth Audio",
        "USB Charging Port",
        "Backup Camera",
        "Keyless Entry",
        "Apple CarPlay/Android Auto",
        "Lane Departure Warning"
      ],
      "specifications": {
        "Engine Size": "1.5L - 1.8L",
        "Fuel Type": "Petrol",
        "Fuel Efficiency": "15-20 km/L",
        "Transmission": "Automatic",
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
      "mileageAllowancePerDay": null,
      "extraKmRate": null,
      
      "cityId": "kumasi-city-id",
      "city": {
        "id": "kumasi-city-id",
        "name": "Kumasi"
      },
      "category": {
        "id": "compact-category-id",
        "name": "Compact",
        "defaultDailyRate": 120.00
      },
      "dailyRate": 120.00,
      "vehicleDailyRate": null,
      "owner": {
        "id": "owner-id-456",
        "email": "owner@example.com",
        "phone": "+233244567890"
      },
      "photos": [
        "https://api.ryverental.com/uploads/civic-front.jpg",
        "https://api.ryverental.com/uploads/civic-interior.jpg",
        "https://api.ryverental.com/uploads/civic-back.jpg",
        "https://api.ryverental.com/uploads/civic-dashboard.jpg"
      ],
      "insuranceDocumentUrl": "https://api.ryverental.com/uploads/civic-insurance.pdf",
      "roadworthinessDocumentUrl": "https://api.ryverental.com/uploads/civic-roadworthy.pdf"
    }
  ]
}
```

### **Approve/Reject Vehicle**

```http
PUT /api/v1/admin/vehicles/{vehicleId}/status
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "status": "active"
}
```

**Valid statuses:** `"active"`, `"inactive"`, `"suspended"`, `"rejected"`

### **Frontend Usage - Admin Review Interface**

```jsx
// React Component Example
function AdminVehicleReview() {
  const [pendingVehicles, setPendingVehicles] = useState([]);
  
  useEffect(() => {
    fetchPendingVehicles();
  }, []);
  
  const fetchPendingVehicles = async () => {
    const response = await fetch('/api/v1/admin/vehicles/pending', {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });
    const data = await response.json();
    setPendingVehicles(data.data);
  };
  
  const approveVehicle = async (vehicleId) => {
    await fetch(`/api/v1/admin/vehicles/${vehicleId}/status`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status: 'active' })
    });
    
    fetchPendingVehicles(); // Refresh list
  };
  
  const rejectVehicle = async (vehicleId) => {
    await fetch(`/api/v1/admin/vehicles/${vehicleId}/status`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status: 'rejected' })
    });
    
    fetchPendingVehicles(); // Refresh list
  };
  
  return (
    <div className="admin-review-panel">
      <h1>Pending Vehicles ({pendingVehicles.length})</h1>
      
      {pendingVehicles.map(vehicle => (
        <div key={vehicle.id} className="vehicle-review-card">
          {/* Photo Grid */}
          <div className="photo-grid">
            {vehicle.photos.map((photo, i) => (
              <img key={i} src={photo} alt={`Photo ${i + 1}`} />
            ))}
          </div>
          
          {/* Vehicle Info */}
          <div className="vehicle-info">
            <h3>
              {vehicle.year} {vehicle.make} {vehicle.model}
              <span className="plate">{vehicle.plateNumber}</span>
            </h3>
            
            {/* Owner Contact */}
            <div className="owner-contact">
              <strong>Owner:</strong>
              <a href={`mailto:${vehicle.owner.email}`}>
                {vehicle.owner.email}
              </a>
              <a href={`tel:${vehicle.owner.phone}`}>
                {vehicle.owner.phone}
              </a>
            </div>
            
            {/* Documents */}
            <div className="documents">
              <h4>Documents</h4>
              <a 
                href={vehicle.insuranceDocumentUrl} 
                target="_blank"
                className="doc-link"
              >
                📄 Insurance Document
              </a>
              <a 
                href={vehicle.roadworthinessDocumentUrl} 
                target="_blank"
                className="doc-link"
              >
                📄 Roadworthiness Certificate
              </a>
            </div>
            
            {/* Features */}
            <div className="features-review">
              <h4>Features ({vehicle.features.length})</h4>
              <div className="feature-tags">
                {vehicle.features.map(f => (
                  <span key={f} className="tag">{f}</span>
                ))}
              </div>
            </div>
            
            {/* Specifications */}
            <div className="specs-review">
              <h4>Specifications</h4>
              <table>
                <tbody>
                  {Object.entries(vehicle.specifications).map(([key, value]) => (
                    <tr key={key}>
                      <td>{key}</td>
                      <td>{value}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            
            {/* Mileage Settings */}
            <div className="mileage-review">
              <h4>Mileage Settings</h4>
              <p>
                Daily Allowance: 
                <strong>
                  {vehicle.mileageAllowancePerDay ?? 
                    `${vehicle.inclusions.mileageAllowancePerDay} (global default)`}
                </strong> km
              </p>
              <p>
                Extra Rate: 
                <strong>
                  GHS {vehicle.extraKmRate ?? 
                    `${vehicle.inclusions.extraKmRate} (global default)`}
                </strong>/km
              </p>
            </div>
          </div>
          
          {/* Actions */}
          <div className="review-actions">
            <button 
              className="btn-approve"
              onClick={() => approveVehicle(vehicle.id)}
            >
              ✅ Approve & Activate
            </button>
            <button 
              className="btn-reject"
              onClick={() => rejectVehicle(vehicle.id)}
            >
              ❌ Reject
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## Frontend Implementation Examples

### **Complete Vehicle Creation Flow**

```jsx
function VehicleCreationForm() {
  const [formData, setFormData] = useState({
    year: '',
    make: '',
    model: '',
    trim: '',
    plateNumber: '',
    categoryId: '',
    // Auto-populated fields (can be edited)
    features: [],
    specifications: {},
    inclusions: {},
    mileageAllowancePerDay: null,
    extraKmRate: null
  });
  
  const [autoData, setAutoData] = useState(null);
  
  // Step 1: Fetch auto-populated data when make/model entered
  const handleLookup = async () => {
    if (!formData.year || !formData.make || !formData.model) return;
    
    const response = await fetch(
      `/api/v1/owner/vehicles/lookup/${formData.year}/${formData.make}/${formData.model}?trim=${formData.trim}`,
      {
        headers: { 'Authorization': `Bearer ${ownerToken}` }
      }
    );
    
    const data = await response.json();
    setAutoData(data);
    
    // Pre-fill form
    setFormData(prev => ({
      ...prev,
      features: data.features,
      specifications: data.specifications,
      inclusions: data.inclusions,
      fuelType: data.fuelType,
      transmissionType: data.transmissionType,
      seatingCapacity: data.seatingCapacity
    }));
  };
  
  // Step 2: Toggle features (checkboxes)
  const toggleFeature = (feature) => {
    setFormData(prev => ({
      ...prev,
      features: prev.features.includes(feature)
        ? prev.features.filter(f => f !== feature)
        : [...prev.features, feature]
    }));
  };
  
  // Step 3: Submit vehicle
  const handleSubmit = async (e) => {
    e.preventDefault();
    
    const vehicleData = {
      plateNumber: formData.plateNumber,
      make: formData.make,
      model: formData.model,
      year: formData.year,
      categoryId: formData.categoryId,
      
      // Auto-populated (can be modified by owner)
      fuelType: formData.fuelType,
      transmissionType: formData.transmissionType,
      seatingCapacity: formData.seatingCapacity,
      
      // JSON fields
      featuresJson: JSON.stringify(formData.features),
      specificationsJson: JSON.stringify(formData.specifications),
      inclusionsJson: JSON.stringify(formData.inclusions),
      
      // Mileage overrides (null = use global defaults)
      mileageAllowancePerDay: formData.mileageAllowancePerDay,
      extraKmRate: formData.extraKmRate,
      
      // Pricing
      dailyRate: formData.dailyRate
    };
    
    await fetch('/api/v1/owner/vehicles', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${ownerToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(vehicleData)
    });
  };
  
  return (
    <form onSubmit={handleSubmit}>
      {/* Basic Info */}
      <input 
        type="number" 
        value={formData.year}
        onChange={e => setFormData({...formData, year: e.target.value})}
        placeholder="Year"
      />
      <input 
        value={formData.make}
        onChange={e => setFormData({...formData, make: e.target.value})}
        onBlur={handleLookup} // Trigger lookup
        placeholder="Make"
      />
      <input 
        value={formData.model}
        onChange={e => setFormData({...formData, model: e.target.value})}
        onBlur={handleLookup} // Trigger lookup
        placeholder="Model"
      />
      
      {/* Auto-populated Features (Checkboxes) */}
      {autoData && (
        <div className="features-section">
          <h3>Features (auto-detected)</h3>
          {autoData.features.map(feature => (
            <label key={feature}>
              <input
                type="checkbox"
                checked={formData.features.includes(feature)}
                onChange={() => toggleFeature(feature)}
              />
              {feature}
            </label>
          ))}
        </div>
      )}
      
      {/* Mileage Override */}
      {autoData && (
        <div className="mileage-section">
          <h3>Mileage Settings</h3>
          <label>
            Daily Allowance (km):
            <input
              type="number"
              value={formData.mileageAllowancePerDay ?? autoData.inclusions.mileageAllowancePerDay}
              onChange={e => setFormData({...formData, mileageAllowancePerDay: parseInt(e.target.value)})}
            />
            <small>Global default: {autoData.inclusions.mileageAllowancePerDay} km</small>
          </label>
          
          <label>
            Extra KM Rate (GHS):
            <input
              type="number"
              step="0.01"
              value={formData.extraKmRate ?? autoData.inclusions.extraKmRate}
              onChange={e => setFormData({...formData, extraKmRate: parseFloat(e.target.value)})}
            />
            <small>Global default: GHS {autoData.inclusions.extraKmRate}/km</small>
          </label>
        </div>
      )}
      
      <button type="submit">Create Vehicle</button>
    </form>
  );
}
```

---

## Data Structures

### **Features Array**
```json
[
  "Air Conditioning",
  "Bluetooth Audio",
  "USB Charging Port",
  "Backup Camera",
  "Keyless Entry",
  "Apple CarPlay/Android Auto",
  "Power Windows",
  "Power Locks",
  "ABS Brakes"
]
```

### **Specifications Object**
```json
{
  "Engine Size": "2.5L",
  "Fuel Type": "Petrol",
  "Fuel Efficiency": "13-16 km/L",
  "Transmission": "Automatic (CVT)",
  "Drivetrain": "FWD",
  "Body Style": "Sedan",
  "Seating Capacity": "5"
}
```

### **Inclusions Object**
```json
{
  "mileageAllowancePerDay": 800,
  "extraKmRate": 0.25,
  "currency": "GHS",
  "protectionPlanRequired": true,
  "roadsideAssistance": "Depends on selected protection plan",
  "cancellationPolicy": "Free cancellation with 48hrs notice"
}
```

### **Mileage Override Logic**
```javascript
// If vehicle has custom mileage settings:
const dailyAllowance = vehicle.mileageAllowancePerDay ?? globalDefault; // 800 or 600
const extraRate = vehicle.extraKmRate ?? globalDefault; // 0.25 or 0.30

// Display in UI:
{vehicle.mileageAllowancePerDay 
  ? `${vehicle.mileageAllowancePerDay} km (custom)` 
  : `${globalDefault} km (standard)`}
```

---

## Summary Checklist

### ✅ **For Vehicle Listings:**
- [ ] Display all photos from `photos` array
- [ ] Show features as icons or badges
- [ ] Display key specifications (seats, fuel efficiency)
- [ ] Show mileage allowance and extra rate
- [ ] Display daily rate prominently

### ✅ **For Vehicle Details Page:**
- [ ] Full photo gallery with all images
- [ ] Complete feature list with checkmarks
- [ ] Specifications table
- [ ] "What's Included" section
- [ ] Mileage terms clearly stated
- [ ] Owner information (name, rating, trips)

### ✅ **For Owner Vehicle Form:**
- [ ] Call lookup API on make/model entry
- [ ] Pre-fill features as checkboxes
- [ ] Pre-fill specifications (editable)
- [ ] Show global defaults for mileage
- [ ] Allow mileage overrides
- [ ] Submit all JSON fields properly

### ✅ **For Admin Approval:**
- [ ] Display all photos in grid
- [ ] Show document links (insurance, roadworthy)
- [ ] List all features for verification
- [ ] Display specifications table
- [ ] Show mileage settings (custom or default)
- [ ] Owner contact information
- [ ] Approve/Reject buttons

---

## 🚀 Ready to Implement!

All endpoints are live and backward compatible. Start building your frontend features using these comprehensive examples! 🎉
