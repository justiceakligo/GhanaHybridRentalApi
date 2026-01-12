# Partner Application Form Specification

This document provides the frontend specification for the RyveRental partner application form that will be displayed on the website.

---

## Overview

The partner application form allows businesses (hotels, travel agencies, OTAs, etc.) to apply for partnership with RyveRental. Once submitted, applications are reviewed by admins and approved/rejected through the admin dashboard.

---

## Form Layout

### Page Structure

```
┌──────────────────────────────────────────────────────────────┐
│                    BECOME A PARTNER                          │
│                                                              │
│  Join RyveRental's growing partner network and offer        │
│  premium vehicle rental services to your customers.         │
│                                                              │
│  ✓ Earn up to 15% commission on every booking              │
│  ✓ Access to 500+ vehicles across Ghana                    │
│  ✓ Full API integration for seamless booking               │
│  ✓ Dedicated partner support                               │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │         PARTNER APPLICATION FORM                   │    │
│  │                                                     │    │
│  │  [Form fields here]                                │    │
│  │                                                     │    │
│  │  [Submit Application Button]                       │    │
│  └────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │         HOW IT WORKS                               │    │
│  │                                                     │    │
│  │  1. Submit Application → 2. Review (1-3 days) →   │    │
│  │  3. Get API Credentials → 4. Integrate & Go Live  │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

---

## Form Fields

### Section 1: Business Information

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| **Business Name** | Text Input | Yes | Max 256 chars | Legal business name |
| **Business Type** | Dropdown | Yes | See options below | Type of business |
| **Registration Number** | Text Input | No | Max 128 chars | Business registration/license number |
| **Website URL** | URL Input | Yes | Valid URL | Business website |
| **Business Description** | Textarea | Yes | Max 1000 chars | Brief description of business |

**Business Type Options:**
- Hotel
- Travel Agency
- Online Travel Agency (OTA)
- Tour Operator
- Corporate/B2B
- Other

---

### Section 2: Contact Information

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| **Contact Person** | Text Input | Yes | Max 256 chars | Primary contact name |
| **Email Address** | Email Input | Yes | Valid email | Primary contact email |
| **Phone Number** | Tel Input | Yes | Valid phone | Primary contact phone |
| **Alternative Phone** | Tel Input | No | Valid phone | Secondary contact phone |

---

### Section 3: Business Location

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| **Country** | Dropdown | Yes | Ghana only (for now) | Country of operation |
| **City** | Dropdown | Yes | See cities list | Primary city of operation |
| **Physical Address** | Textarea | Yes | Max 512 chars | Street address |
| **Additional Cities** | Multi-select | No | - | Other cities you operate in |

**Cities List:**
- Accra
- Kumasi
- Takoradi
- Tamale
- Cape Coast
- Tema
- Other (specify)

---

### Section 4: Integration Details

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| **Expected Monthly Bookings** | Number Input | Yes | Min 1 | Estimated monthly booking volume |
| **Integration Type** | Radio Buttons | Yes | - | How you'll integrate |
| **Use Case** | Textarea | Yes | Max 1000 chars | How you plan to use the API |
| **Technical Contact Email** | Email Input | No | Valid email | Developer/tech team email |
| **Webhook URL** | URL Input | No | Valid HTTPS URL | URL for receiving event notifications |

**Integration Type Options:**
- ⚡ **API Integration** - Full programmatic integration (recommended)
- 🖥️ **Widget Embed** - Embed booking widget on your website
- 📱 **Mobile App** - Integrate into mobile application
- 📧 **Manual Coordination** - Bookings via email/phone coordination

---

### Section 5: Business Documents (Optional but Recommended)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| **Business Registration Certificate** | File Upload | No | PDF, PNG, JPG (max 5MB) |
| **Tax Identification Number** | Text Input | No | Ghana TIN |
| **Business License** | File Upload | No | PDF, PNG, JPG (max 5MB) |

---

### Section 6: Terms & Conditions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| **Accept Terms** | Checkbox | Yes | Must agree to partner terms |
| **Accept Data Policy** | Checkbox | Yes | Must agree to data processing |
| **Marketing Communications** | Checkbox | No | Opt-in for partner updates |

**Terms Text:**
> I confirm that I have read and agree to the [Partner Terms & Conditions](https://ryverental.com/partner-terms) and [Privacy Policy](https://ryverental.com/privacy).

---

## Form Validation Rules

### Client-Side Validation

```javascript
const validationRules = {
  businessName: {
    required: true,
    minLength: 3,
    maxLength: 256,
    message: "Business name must be between 3 and 256 characters"
  },
  email: {
    required: true,
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    message: "Please enter a valid email address"
  },
  phone: {
    required: true,
    pattern: /^\+?[0-9]{10,15}$/,
    message: "Please enter a valid phone number"
  },
  website: {
    required: true,
    pattern: /^https?:\/\/.+\..+/,
    message: "Please enter a valid website URL"
  },
  expectedBookings: {
    required: true,
    min: 1,
    type: "number",
    message: "Please enter expected monthly bookings (minimum 1)"
  },
  useCase: {
    required: true,
    minLength: 50,
    maxLength: 1000,
    message: "Please describe your use case (50-1000 characters)"
  },
  acceptTerms: {
    required: true,
    mustBeTrue: true,
    message: "You must accept the terms and conditions"
  }
};
```

---

## API Endpoint

### Submit Partner Application

**Endpoint:** `POST /api/v1/partner-applications`

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "businessName": "Amazing Tours Ltd",
  "businessType": "travel_agency",
  "registrationNumber": "C123456789",
  "website": "https://amazingtours.com",
  "description": "We are a leading travel agency in Accra specializing in corporate travel and tourism packages.",
  "contactPerson": "John Mensah",
  "email": "john@amazingtours.com",
  "phone": "+233 24 123 4567",
  "alternativePhone": "+233 20 987 6543",
  "country": "Ghana",
  "city": "Accra",
  "address": "123 Liberation Road, Osu, Accra",
  "additionalCities": ["Kumasi", "Takoradi"],
  "expectedMonthlyBookings": 50,
  "integrationType": "api",
  "useCase": "We want to offer vehicle rental services to our corporate clients and tourists. Our website receives 10,000+ visitors monthly and we need a seamless booking integration.",
  "technicalContactEmail": "tech@amazingtours.com",
  "webhookUrl": "https://amazingtours.com/api/webhooks/ryverental",
  "taxIdentificationNumber": "TIN-12345678",
  "acceptTerms": true,
  "acceptDataPolicy": true,
  "marketingOptIn": true
}
```

**Success Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "applicationReference": "PA-2026-001234",
  "status": "pending",
  "submittedAt": "2026-01-11T14:30:00Z",
  "message": "Application submitted successfully. You will receive an email notification within 1-3 business days.",
  "nextSteps": [
    "Our team will review your application",
    "You'll receive an email notification of approval/rejection",
    "Upon approval, you'll receive API credentials and onboarding instructions"
  ]
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "Validation failed",
  "details": [
    {
      "field": "email",
      "message": "Email address is already registered"
    },
    {
      "field": "website",
      "message": "Please enter a valid website URL"
    }
  ]
}
```

---

## UI/UX Guidelines

### Form Styling

**Design Principles:**
- Clean, professional appearance
- Mobile-responsive (works on all devices)
- Progressive disclosure (show fields in logical sections)
- Inline validation with helpful error messages
- Loading states during submission
- Success/error notifications

**Color Scheme:**
```css
:root {
  --primary-color: #2563eb;      /* Blue for CTAs */
  --success-color: #10b981;      /* Green for success messages */
  --error-color: #ef4444;        /* Red for errors */
  --text-primary: #1f2937;       /* Dark gray for text */
  --text-secondary: #6b7280;     /* Light gray for labels */
  --border-color: #e5e7eb;       /* Light border */
  --background: #ffffff;         /* White background */
}
```

### Component Examples

**Text Input:**
```html
<div class="form-group">
  <label for="businessName" class="form-label">
    Business Name <span class="required">*</span>
  </label>
  <input
    type="text"
    id="businessName"
    name="businessName"
    class="form-input"
    placeholder="Enter your business name"
    required
    maxlength="256"
  />
  <span class="form-hint">Legal name as registered</span>
  <span class="form-error" id="businessName-error"></span>
</div>
```

**Dropdown:**
```html
<div class="form-group">
  <label for="businessType" class="form-label">
    Business Type <span class="required">*</span>
  </label>
  <select id="businessType" name="businessType" class="form-select" required>
    <option value="">Select business type</option>
    <option value="hotel">Hotel</option>
    <option value="travel_agency">Travel Agency</option>
    <option value="ota">Online Travel Agency (OTA)</option>
    <option value="tour_operator">Tour Operator</option>
    <option value="corporate">Corporate/B2B</option>
    <option value="other">Other</option>
  </select>
  <span class="form-error" id="businessType-error"></span>
</div>
```

**Textarea:**
```html
<div class="form-group">
  <label for="useCase" class="form-label">
    Use Case <span class="required">*</span>
  </label>
  <textarea
    id="useCase"
    name="useCase"
    class="form-textarea"
    rows="4"
    placeholder="Describe how you plan to integrate our services..."
    required
    minlength="50"
    maxlength="1000"
  ></textarea>
  <span class="form-hint">
    <span id="useCase-count">0</span>/1000 characters (minimum 50)
  </span>
  <span class="form-error" id="useCase-error"></span>
</div>
```

**Submit Button:**
```html
<button type="submit" class="btn btn-primary" id="submitBtn">
  <span class="btn-text">Submit Application</span>
  <span class="btn-loader" style="display: none;">
    <svg class="spinner" viewBox="0 0 24 24">
      <circle cx="12" cy="12" r="10" />
    </svg>
    Submitting...
  </span>
</button>
```

---

## Success Flow

### After Successful Submission

**Success Message:**
```
┌────────────────────────────────────────────────────┐
│            ✓ APPLICATION SUBMITTED                 │
│                                                    │
│  Thank you for your interest in partnering        │
│  with RyveRental!                                 │
│                                                    │
│  Application Reference: PA-2026-001234            │
│                                                    │
│  What happens next?                               │
│  1. Our team reviews your application (1-3 days)  │
│  2. You'll receive an email notification          │
│  3. Upon approval, get API credentials            │
│  4. Start integrating and earning!                │
│                                                    │
│  You can check your application status at:        │
│  https://dashboard.ryverental.com/partners        │
│                                                    │
│  Questions? Contact partners@ryverental.com       │
└────────────────────────────────────────────────────┘
```

**Confirmation Email (Auto-sent):**

```
Subject: Partner Application Received - PA-2026-001234

Dear John Mensah,

Thank you for applying to become a RyveRental partner!

We have received your application for Amazing Tours Ltd.

Application Details:
- Reference: PA-2026-001234
- Business Type: Travel Agency
- Expected Monthly Bookings: 50
- Submitted: January 11, 2026 at 2:30 PM

Next Steps:
Our partnerships team will review your application within 1-3 
business days. You will receive an email notification once a 
decision has been made.

In the meantime, you can:
✓ Read our Partner API Documentation: https://docs.ryverental.com/partners
✓ Explore integration examples: https://github.com/ryverental/examples
✓ Join our partner community: https://community.ryverental.com

Questions? Reply to this email or contact:
partnerships@ryverental.com
+233 XX XXX XXXX

Best regards,
The RyveRental Partnerships Team
```

---

## Form State Management (React Example)

```javascript
import { useState } from 'react';

function PartnerApplicationForm() {
  const [formData, setFormData] = useState({
    businessName: '',
    businessType: '',
    email: '',
    phone: '',
    website: '',
    city: '',
    expectedMonthlyBookings: '',
    integrationType: 'api',
    useCase: '',
    acceptTerms: false,
    acceptDataPolicy: false
  });

  const [errors, setErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitSuccess, setSubmitSuccess] = useState(false);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
    // Clear error when user starts typing
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
  };

  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.businessName || formData.businessName.length < 3) {
      newErrors.businessName = 'Business name must be at least 3 characters';
    }
    
    if (!formData.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    
    if (!formData.useCase || formData.useCase.length < 50) {
      newErrors.useCase = 'Please provide at least 50 characters describing your use case';
    }
    
    if (!formData.acceptTerms) {
      newErrors.acceptTerms = 'You must accept the terms and conditions';
    }
    
    return newErrors;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    const newErrors = validateForm();
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }
    
    setIsSubmitting(true);
    
    try {
      const response = await fetch('https://ryverental.info/api/v1/partner-applications', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      });
      
      if (response.ok) {
        const result = await response.json();
        setSubmitSuccess(true);
        // Show success message with application reference
        console.log('Application submitted:', result.applicationReference);
      } else {
        const error = await response.json();
        setErrors(error.details || { submit: error.error });
      }
    } catch (error) {
      setErrors({ submit: 'Network error. Please try again.' });
    } finally {
      setIsSubmitting(false);
    }
  };

  if (submitSuccess) {
    return <SuccessMessage />;
  }

  return (
    <form onSubmit={handleSubmit} className="partner-application-form">
      {/* Form fields here */}
    </form>
  );
}
```

---

## Mobile Responsiveness

**Breakpoints:**
```css
/* Mobile First Approach */

/* Small devices (phones, 0-640px) */
@media (max-width: 640px) {
  .form-group {
    margin-bottom: 1.5rem;
  }
  
  .form-input,
  .form-select,
  .form-textarea {
    font-size: 16px; /* Prevent zoom on iOS */
  }
}

/* Medium devices (tablets, 641px-1024px) */
@media (min-width: 641px) {
  .form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }
}

/* Large devices (desktops, 1025px+) */
@media (min-width: 1025px) {
  .partner-application-form {
    max-width: 800px;
    margin: 0 auto;
  }
}
```

---

## Accessibility (WCAG 2.1 AA Compliant)

**Requirements:**
- All form fields must have associated `<label>` elements
- Error messages must be announced to screen readers
- Form must be keyboard navigable
- Focus states clearly visible
- Color contrast ratio ≥ 4.5:1
- Required fields marked with asterisk and aria-required
- Success/error messages have appropriate ARIA roles

**Example:**
```html
<div class="form-group" role="group" aria-labelledby="businessName-label">
  <label id="businessName-label" for="businessName" class="form-label">
    Business Name <span class="required" aria-label="required">*</span>
  </label>
  <input
    type="text"
    id="businessName"
    name="businessName"
    class="form-input"
    aria-required="true"
    aria-invalid="false"
    aria-describedby="businessName-hint businessName-error"
  />
  <span id="businessName-hint" class="form-hint">Legal name as registered</span>
  <span id="businessName-error" class="form-error" role="alert"></span>
</div>
```

---

## Analytics & Tracking

**Track These Events:**

```javascript
// Form started
gtag('event', 'partner_application_started', {
  event_category: 'Partner',
  event_label: 'Form Started'
});

// Field completed
gtag('event', 'partner_application_field_completed', {
  event_category: 'Partner',
  event_label: fieldName
});

// Form submitted
gtag('event', 'partner_application_submitted', {
  event_category: 'Partner',
  event_label: 'Form Submitted',
  value: formData.expectedMonthlyBookings
});

// Submission success
gtag('event', 'partner_application_success', {
  event_category: 'Partner',
  event_label: applicationReference
});

// Submission error
gtag('event', 'partner_application_error', {
  event_category: 'Partner',
  event_label: errorMessage
});
```

---

**Document Version:** 1.0  
**Last Updated:** January 11, 2026  
**Frontend Team Contact:** dev@ryverental.com
