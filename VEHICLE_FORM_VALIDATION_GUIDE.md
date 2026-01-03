# Vehicle Form Validation Guide - Inline Validation for Mileage Settings

## Overview

This guide helps you implement inline validation for vehicle creation/update forms to prevent submission errors. The validation enforces admin-configured constraints for:

1. **Included Kilometers** - Must meet minimum set by admin
2. **Price Per Extra Km** - Must be within min/max range set by admin

---

## Step 1: Fetch Mileage Settings on Page Load

### Endpoint
```
GET /api/v1/owner/settings/mileage
```

### Authentication
Requires owner role. Include JWT token in Authorization header.

### Sample Request

```javascript
async function fetchMileageSettings() {
  const token = localStorage.getItem('ownerToken');
  
  const response = await fetch(
    'https://ryverental.info/api/v1/owner/settings/mileage',
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error('Failed to fetch mileage settings');
  }

  return await response.json();
}
```

### Sample Response (200 OK)

```json
{
  "mileageChargingEnabled": true,
  "minimumIncludedKilometers": 100,
  "minPricePerExtraKm": 0.30,
  "maxPricePerExtraKm": 1.00,
  "defaultPricePerExtraKm": 0.50
}
```

---

## Step 2: React/TypeScript Implementation with Inline Validation

### TypeScript Interface

```typescript
interface MileageSettings {
  mileageChargingEnabled: boolean;
  minimumIncludedKilometers: number;
  minPricePerExtraKm: number;
  maxPricePerExtraKm: number;
  defaultPricePerExtraKm: number;
}

interface VehicleFormData {
  // ... other fields
  includedKilometers?: number;
  pricePerExtraKm?: number;
  mileageChargingEnabled?: boolean;
}

interface ValidationErrors {
  includedKilometers?: string;
  pricePerExtraKm?: string;
}
```

### Complete React Component Example

```typescript
import React, { useState, useEffect } from 'react';

const VehicleForm: React.FC = () => {
  const [mileageSettings, setMileageSettings] = useState<MileageSettings | null>(null);
  const [formData, setFormData] = useState<VehicleFormData>({
    includedKilometers: undefined,
    pricePerExtraKm: undefined,
    mileageChargingEnabled: false
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [loading, setLoading] = useState(true);

  // Fetch mileage settings on component mount
  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await fetchMileageSettings();
        setMileageSettings(settings);
        
        // Set default values from admin settings
        setFormData(prev => ({
          ...prev,
          includedKilometers: settings.minimumIncludedKilometers,
          pricePerExtraKm: settings.defaultPricePerExtraKm,
          mileageChargingEnabled: settings.mileageChargingEnabled
        }));
      } catch (error) {
        console.error('Failed to load mileage settings:', error);
      } finally {
        setLoading(false);
      }
    };

    loadSettings();
  }, []);

  // Inline validation for Included Kilometers
  const validateIncludedKilometers = (value: number | undefined): string | undefined => {
    if (!mileageSettings) return undefined;
    
    if (value === undefined || value === null) {
      return 'Included kilometers is required';
    }
    
    if (value < mileageSettings.minimumIncludedKilometers) {
      return `Must be at least ${mileageSettings.minimumIncludedKilometers} km`;
    }
    
    return undefined;
  };

  // Inline validation for Price Per Extra Km
  const validatePricePerExtraKm = (value: number | undefined): string | undefined => {
    if (!mileageSettings) return undefined;
    
    if (value === undefined || value === null) {
      return 'Price per extra km is required';
    }
    
    if (value < mileageSettings.minPricePerExtraKm) {
      return `Must be at least ${mileageSettings.minPricePerExtraKm} GHS/km`;
    }
    
    if (value > mileageSettings.maxPricePerExtraKm) {
      return `Must not exceed ${mileageSettings.maxPricePerExtraKm} GHS/km`;
    }
    
    return undefined;
  };

  // Handle field change with immediate validation
  const handleIncludedKilometersChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value ? parseInt(e.target.value) : undefined;
    
    setFormData(prev => ({ ...prev, includedKilometers: value }));
    
    // Validate immediately
    const error = validateIncludedKilometers(value);
    setErrors(prev => ({ ...prev, includedKilometers: error }));
  };

  const handlePricePerExtraKmChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value ? parseFloat(e.target.value) : undefined;
    
    setFormData(prev => ({ ...prev, pricePerExtraKm: value }));
    
    // Validate immediately
    const error = validatePricePerExtraKm(value);
    setErrors(prev => ({ ...prev, pricePerExtraKm: error }));
  };

  // Validate all fields before submission
  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {};
    
    if (formData.mileageChargingEnabled) {
      newErrors.includedKilometers = validateIncludedKilometers(formData.includedKilometers);
      newErrors.pricePerExtraKm = validatePricePerExtraKm(formData.pricePerExtraKm);
    }
    
    setErrors(newErrors);
    
    // Return true if no errors
    return !Object.values(newErrors).some(error => error !== undefined);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      alert('Please fix validation errors before submitting');
      return;
    }
    
    // Submit form...
    try {
      // Your submission logic here
      console.log('Form is valid, submitting...', formData);
    } catch (error) {
      console.error('Submission error:', error);
    }
  };

  if (loading) {
    return <div>Loading settings...</div>;
  }

  return (
    <form onSubmit={handleSubmit}>
      {/* ... other form fields ... */}

      {mileageSettings?.mileageChargingEnabled && (
        <>
          <div className="form-group">
            <label htmlFor="includedKilometers">
              Included Kilometers per Day *
              <span className="text-muted">
                (Minimum: {mileageSettings.minimumIncludedKilometers} km)
              </span>
            </label>
            <input
              type="number"
              id="includedKilometers"
              name="includedKilometers"
              value={formData.includedKilometers || ''}
              onChange={handleIncludedKilometersChange}
              min={mileageSettings.minimumIncludedKilometers}
              className={errors.includedKilometers ? 'error' : ''}
              required
            />
            {errors.includedKilometers && (
              <span className="error-message">{errors.includedKilometers}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="pricePerExtraKm">
              Price Per Extra Km (GHS) *
              <span className="text-muted">
                (Range: {mileageSettings.minPricePerExtraKm} - {mileageSettings.maxPricePerExtraKm} GHS)
              </span>
            </label>
            <input
              type="number"
              id="pricePerExtraKm"
              name="pricePerExtraKm"
              value={formData.pricePerExtraKm || ''}
              onChange={handlePricePerExtraKmChange}
              min={mileageSettings.minPricePerExtraKm}
              max={mileageSettings.maxPricePerExtraKm}
              step="0.01"
              className={errors.pricePerExtraKm ? 'error' : ''}
              required
            />
            {errors.pricePerExtraKm && (
              <span className="error-message">{errors.pricePerExtraKm}</span>
            )}
          </div>
        </>
      )}

      <button type="submit" disabled={Object.values(errors).some(e => e !== undefined)}>
        Submit Vehicle
      </button>
    </form>
  );
};

export default VehicleForm;
```

---

## Step 3: CSS for Error Styling

```css
.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
}

.form-group label .text-muted {
  display: block;
  font-size: 0.875rem;
  color: #6c757d;
  font-weight: normal;
  margin-top: 0.25rem;
}

.form-group input.error {
  border-color: #dc3545;
  background-color: #fff5f5;
}

.error-message {
  display: block;
  color: #dc3545;
  font-size: 0.875rem;
  margin-top: 0.25rem;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
```

---

## Step 4: Plain JavaScript (Vanilla JS) Example

For non-React applications:

```html
<!DOCTYPE html>
<html>
<head>
  <title>Vehicle Form</title>
  <style>
    .error { border-color: red; background-color: #fff5f5; }
    .error-message { color: red; font-size: 14px; margin-top: 5px; }
  </style>
</head>
<body>
  <form id="vehicleForm">
    <div>
      <label for="includedKilometers">
        Included Kilometers *
        <span id="kmHint"></span>
      </label>
      <input 
        type="number" 
        id="includedKilometers" 
        name="includedKilometers"
        required
      />
      <span id="kmError" class="error-message"></span>
    </div>

    <div>
      <label for="pricePerExtraKm">
        Price Per Extra Km (GHS) *
        <span id="priceHint"></span>
      </label>
      <input 
        type="number" 
        id="pricePerExtraKm" 
        name="pricePerExtraKm"
        step="0.01"
        required
      />
      <span id="priceError" class="error-message"></span>
    </div>

    <button type="submit">Submit</button>
  </form>

  <script>
    let mileageSettings = null;

    // Fetch settings on page load
    async function loadMileageSettings() {
      const token = localStorage.getItem('ownerToken');
      
      try {
        const response = await fetch(
          'https://ryverental.info/api/v1/owner/settings/mileage',
          {
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            }
          }
        );
        
        mileageSettings = await response.json();
        
        // Set hints and default values
        document.getElementById('kmHint').textContent = 
          `(Minimum: ${mileageSettings.minimumIncludedKilometers} km)`;
        document.getElementById('priceHint').textContent = 
          `(Range: ${mileageSettings.minPricePerExtraKm} - ${mileageSettings.maxPricePerExtraKm} GHS)`;
        
        document.getElementById('includedKilometers').value = 
          mileageSettings.minimumIncludedKilometers;
        document.getElementById('includedKilometers').min = 
          mileageSettings.minimumIncludedKilometers;
        
        document.getElementById('pricePerExtraKm').value = 
          mileageSettings.defaultPricePerExtraKm;
        document.getElementById('pricePerExtraKm').min = 
          mileageSettings.minPricePerExtraKm;
        document.getElementById('pricePerExtraKm').max = 
          mileageSettings.maxPricePerExtraKm;
        
      } catch (error) {
        console.error('Failed to load settings:', error);
      }
    }

    // Validate included kilometers
    function validateIncludedKilometers() {
      const input = document.getElementById('includedKilometers');
      const error = document.getElementById('kmError');
      const value = parseInt(input.value);
      
      if (!mileageSettings) return false;
      
      if (isNaN(value) || value < mileageSettings.minimumIncludedKilometers) {
        input.classList.add('error');
        error.textContent = `Must be at least ${mileageSettings.minimumIncludedKilometers} km`;
        return false;
      }
      
      input.classList.remove('error');
      error.textContent = '';
      return true;
    }

    // Validate price per extra km
    function validatePricePerExtraKm() {
      const input = document.getElementById('pricePerExtraKm');
      const error = document.getElementById('priceError');
      const value = parseFloat(input.value);
      
      if (!mileageSettings) return false;
      
      if (isNaN(value) || value < mileageSettings.minPricePerExtraKm) {
        input.classList.add('error');
        error.textContent = `Must be at least ${mileageSettings.minPricePerExtraKm} GHS/km`;
        return false;
      }
      
      if (value > mileageSettings.maxPricePerExtraKm) {
        input.classList.add('error');
        error.textContent = `Must not exceed ${mileageSettings.maxPricePerExtraKm} GHS/km`;
        return false;
      }
      
      input.classList.remove('error');
      error.textContent = '';
      return true;
    }

    // Add event listeners
    document.getElementById('includedKilometers').addEventListener('input', validateIncludedKilometers);
    document.getElementById('includedKilometers').addEventListener('blur', validateIncludedKilometers);
    
    document.getElementById('pricePerExtraKm').addEventListener('input', validatePricePerExtraKm);
    document.getElementById('pricePerExtraKm').addEventListener('blur', validatePricePerExtraKm);

    // Form submission
    document.getElementById('vehicleForm').addEventListener('submit', function(e) {
      e.preventDefault();
      
      const kmValid = validateIncludedKilometers();
      const priceValid = validatePricePerExtraKm();
      
      if (kmValid && priceValid) {
        // Submit form
        console.log('Form is valid, submitting...');
        // Your submission logic here
      } else {
        alert('Please fix validation errors before submitting');
      }
    });

    // Load settings when page loads
    loadMileageSettings();
  </script>
</body>
</html>
```

---

## Backend API Response Codes

### Success Responses

| Code | Scenario |
|------|----------|
| 200 OK | Settings fetched successfully |

### Error Responses

| Code | Scenario | Response |
|------|----------|----------|
| 400 Bad Request | Validation failed on submit | `{ "error": "Included kilometers must be at least 100 km" }` |
| 401 Unauthorized | Invalid/missing token | - |
| 500 Internal Server Error | Server error | Error details in response |

---

## Testing Checklist

- [ ] Settings load correctly on page load
- [ ] Default values populate from admin settings
- [ ] Included Kilometers shows error when below minimum
- [ ] Price Per Extra Km shows error when below minimum
- [ ] Price Per Extra Km shows error when above maximum
- [ ] Error messages clear when valid values entered
- [ ] Submit button disabled when errors present
- [ ] Form submits successfully with valid data
- [ ] HTML5 validation attributes (`min`, `max`) work correctly
- [ ] Validation works on both `input` and `blur` events

---

## Notes

- **Always fetch settings before showing the form** to ensure you have latest constraints
- **Use default values from admin settings** to help owners
- **Validate on input AND blur** for best user experience
- **Show helpful hints** with the min/max values visible
- **Disable submit button** when validation errors exist
- **HTML5 attributes** (`min`, `max`) provide basic browser validation but JavaScript validation is still required for complete UX

---

## Admin Settings Management

Admins can update these settings via:
- **GET** `/api/v1/admin/settings/mileage` - View current settings
- **PUT** `/api/v1/admin/settings/mileage` - Update settings

When admin changes settings, existing vehicles are not affected, but new vehicles or updates must comply with new constraints.
