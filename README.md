# Ghana Hybrid Rental API

**Enterprise-grade car rental platform API** with Hertz/Enterprise/Turo-style QR check-in and comprehensive rental agreements.

Built with .NET 8 Minimal APIs + PostgreSQL + Entity Framework Core.

---

## 🚀 Key Features

### ✨ Rental Agreement System (NEW)
- **Enterprise-style legal agreements** matching Hertz/Enterprise practices
- **Versioned templates** with admin management
- **Per-booking acceptance tracking** with full audit trail (IP, timestamp, agreement snapshot)
- **Mandatory clause enforcement**: No smoking, fines/tickets, accidents
- **Blocks check-in** until agreement accepted for legal protection

### 📱 QR Check-In Flow
- **Magic link generation** for pickup/return inspections (token-based, no auth required)
- **QR payload endpoint** for frontend QR code generation
- **Deep link support** (`ryverental://checkin?...`)
- **Automatic status transitions**: confirmed → ongoing → completed
- **Email notifications** with QR links and check-in instructions

### 🔐 Role-Based Authentication
- **Renters**: Phone + Password (auto-verified, instant activation)
- **Owners**: Email + Password (pending admin approval)
- **Admins**: Full platform management access
- **JWT tokens** with role-based access control

### 📊 Complete Rental Management
- **Booking system** with status tracking
- **Vehicle management** with categories and insurance plans
- **Inspection records** with photos and odometer readings
- **Global settings** and app configuration
- **Email notifications** (SMTP + fake provider for testing)
- **WhatsApp integration** (Meta Cloud API ready)

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [**RENTAL_AGREEMENT_SYSTEM.md**](RENTAL_AGREEMENT_SYSTEM.md) | Complete rental agreement documentation (600+ lines) |
| [**QR_CHECKIN_FLOW.md**](QR_CHECKIN_FLOW.md) | QR check-in flow and authentication guide |
| [**SETUP.md**](SETUP.md) | Setup, deployment, and production configuration |
| [**API_QUICK_REFERENCE.md**](API_QUICK_REFERENCE.md) | Quick reference for common API operations |
| [**IMPLEMENTATION_SUMMARY.md**](IMPLEMENTATION_SUMMARY.md) | Implementation details and architecture overview |

---

## 🏃 Quick Start

### Prerequisites
- .NET 8 SDK
- PostgreSQL 16+
- (Optional) SMTP server for email

### 1. Clone & Restore
```bash
git clone <repo-url>
cd GhanaHybridRentalApi
dotnet restore
```

### 2. Configure Database
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ghanarental;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

Create database:
```bash
psql -U postgres -c "CREATE DATABASE ghanarental;"
```

### 3. Apply Migrations
```bash
dotnet ef database update
```

### 4. Run
```bash
# Development (with hot reload)
dotnet watch run

# Production
dotnet run --configuration Release
```

**API URL:** `http://localhost:5000`  
**Swagger UI:** `http://localhost:5000/swagger`  
**Health Check:** `http://localhost:5000/health`

---

## 🔑 Quick API Examples

### Register & Login
```bash
# Register renter (phone-based)
POST /api/v1/auth/register
{
  "phone": "+233501234567",
  "password": "Pass123",
  "role": "renter"
}

# Login
POST /api/v1/auth/login
{
  "emailOrPhone": "+233501234567",
  "password": "Pass123"
}
# Returns: { "token": "jwt...", "userId": "guid" }
```

### Complete Rental Flow
```bash
# 1. Renter books vehicle
POST /api/v1/bookings
Authorization: Bearer <renter-token>

# 2. View rental agreement
GET /api/v1/bookings/{id}/rental-agreement
Authorization: Bearer <renter-token>

# 3. Accept agreement (REQUIRED ✨)
POST /api/v1/bookings/{id}/rental-agreement/accept
Authorization: Bearer <renter-token>
{
  "acceptedNoSmoking": true,
  "acceptedFinesAndTickets": true,
  "acceptedAccidentProcedure": true
}

# 4. Owner generates inspection links
POST /api/v1/bookings/{id}/inspection-links
Authorization: Bearer <owner-token>

# 5. Renter gets QR code
GET /api/v1/bookings/{id}/qr
Authorization: Bearer <renter-token>
# Returns: { "pickupUrl": "...", "deepLink": "..." }

# 6. At pickup: Owner scans QR and completes inspection
GET /inspect/{token}
POST /inspect/{token}/complete

# Status automatically transitions: confirmed → ongoing → completed
```

See [API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md) for more examples.

---

## 🏗️ Architecture

```
┌─────────────────┐
│  Client Apps    │
│ (Mobile/Web)    │
└────────┬────────┘
         │ HTTPS + JWT
┌────────▼────────┐
│  Minimal APIs   │
│  (.NET 8)       │
├─────────────────┤
│  Services       │
│  • Auth         │
│  • Email        │
│  • WhatsApp     │
├─────────────────┤
│  EF Core        │
│  • Models       │
│  • DbContext    │
└────────┬────────┘
         │
┌────────▼────────┐
│  PostgreSQL     │
│  (Database)     │
└─────────────────┘
```

### Tech Stack
- **Backend:** ASP.NET Core 8.0 Minimal APIs
- **Database:** PostgreSQL 16+ with EF Core
- **Authentication:** JWT Bearer tokens + BCrypt password hashing
- **Email:** SMTP via MailKit (configurable fake provider for testing)
- **WhatsApp:** Meta Cloud API integration (optional)
- **Documentation:** Swagger/OpenAPI

---

## 📋 API Endpoints

### Authentication
- `POST /api/v1/auth/register` - Register user (role-specific)
- `POST /api/v1/auth/login` - Login with phone/email + password
- `GET /api/v1/auth/me` - Get current user profile

### Rental Agreements ✨ NEW
- `GET /api/v1/bookings/{id}/rental-agreement` - View agreement for booking
- `POST /api/v1/bookings/{id}/rental-agreement/accept` - Accept agreement (creates legal record)
- `GET /api/v1/bookings/{id}/rental-agreement/acceptance` - Verify acceptance (owner/admin)
- `GET /api/v1/admin/rental-agreement-template` - Get current template (admin)
- `PUT /api/v1/admin/rental-agreement-template` - Update template (admin)

### Bookings & QR Check-In
- `POST /api/v1/bookings` - Create booking
- `GET /api/v1/bookings` - List bookings
- `GET /api/v1/bookings/{id}` - Get booking details
- `GET /api/v1/bookings/{id}/qr` ✨ - Get QR payload for frontend
- `POST /api/v1/bookings/{id}/inspection-links` - Generate magic links (requires agreement acceptance ✨)

### Inspections (Magic Links)
- `GET /inspect/{token}` - Access inspection (no auth required)
- `POST /inspect/{token}/complete` - Complete inspection with photos

### Vehicles & Categories
- `GET /api/v1/owner/vehicles` - List owner vehicles
- `POST /api/v1/owner/vehicles` - Add vehicle
- `GET /api/v1/admin/categories` - Manage vehicle categories

### Insurance & Settings
- `GET /api/v1/insurance-plans` - List insurance plans
- `GET /api/v1/admin/settings` - Global settings
- `GET /api/v1/admin/config` - App configuration

---

## 🔒 Security Features

- ✅ **JWT authentication** with configurable signing key
- ✅ **BCrypt password hashing** with automatic salt generation
- ✅ **Role-based access control** (Admin, Owner, Renter)
- ✅ **Magic link tokens** with expiration (1-3 days configurable)
- ✅ **Audit trails** - IP address, timestamp, user agent capture
- ✅ **Agreement snapshots** - Immutable record of accepted terms
- ✅ **CORS configuration** for production origins
- ✅ **Input validation** on all endpoints

See [SETUP.md - Security Checklist](SETUP.md#security-checklist) for production hardening.

---

## 🗄️ Database Schema

### Core Tables
- **Users** - Authentication and profiles
- **OwnerProfiles** / **RenterProfiles** - Role-specific data
- **Vehicles** - Vehicle inventory
- **CarCategories** - Vehicle classifications
- **Bookings** - Rental bookings with status tracking
- **Inspections** - Pickup/return inspection records
- **InsurancePlans** - Insurance options
- **GlobalSettings** - Platform configuration

### Rental Agreement System ✨ NEW
- **RentalAgreementTemplates** - Versioned legal agreements
- **RentalAgreementAcceptances** - Per-booking acceptance records with audit trail

---

## 🚀 Deployment

### Supported Platforms
- **Railway** (Recommended - auto PostgreSQL)
- **Render** (Free PostgreSQL included)
- **Heroku** (PostgreSQL add-on)
- **Docker** (docker-compose included in SETUP.md)
- **Self-hosted** (Linux/Windows Server)

### Environment Variables
```bash
ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
Jwt__SigningKey="YOUR_SECURE_RANDOM_KEY"
Email__SmtpPassword="your-smtp-password"
WhatsApp__CloudApi__AccessToken="your-whatsapp-token"
```

### Quick Deploy (Railway)
```bash
railway init
railway add postgresql
railway up
railway run dotnet ef database update
```

See [SETUP.md](SETUP.md) for complete deployment guides.

---

## 🧪 Testing

### Manual Testing
```bash
# Health check
curl http://localhost:5000/health

# Register renter
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"phone":"+233501234567","password":"Pass123","role":"renter"}'

# View rental agreement
curl -X GET http://localhost:5000/api/v1/bookings/{id}/rental-agreement \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Swagger UI
Navigate to `http://localhost:5000/swagger` for interactive API testing.

---

## 📝 Development Workflow

### Add Migration
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Check Errors
```bash
dotnet build
# Should show: 0 errors, 0 warnings ✅
```

### Watch Mode (Hot Reload)
```bash
dotnet watch run
```

---

## 🆕 What's New (December 2025)

### ✨ Rental Agreement System
- Enterprise-grade legal framework matching Hertz/Enterprise practices
- Versioned templates with mandatory clause enforcement (no smoking, fines/tickets, accidents)
- Per-booking acceptance with full audit trail (IP, timestamp, agreement snapshot)
- Integration with QR check-in flow (blocks until agreement accepted)
- Admin template management endpoints

### 🔄 QR Check-In Enhancements
- New `/bookings/{id}/qr` endpoint returns data for frontend QR generation
- Agreement acceptance now required before generating inspection links
- Email notifications include rental agreement reminders
- Automatic status transitions on inspection completion

### 📧 Email Notifications
- Booking confirmation includes rental agreement section
- Clear instructions for renters about acceptance requirement
- QR links and deep links included in email

---

## 🤝 Contributing

This is a production-ready API built for the Ghana Hybrid Rental platform. For modifications:

1. Create feature branch
2. Update relevant documentation
3. Add tests if applicable
4. Ensure `dotnet build` succeeds with 0 warnings
5. Update CHANGELOG with changes

---

## 📄 License

[Your License Here]

---

## 🆘 Support

**Documentation:** See `/docs` folder or markdown files in root:
- [RENTAL_AGREEMENT_SYSTEM.md](RENTAL_AGREEMENT_SYSTEM.md) - Rental agreement details
- [QR_CHECKIN_FLOW.md](QR_CHECKIN_FLOW.md) - QR check-in guide
- [SETUP.md](SETUP.md) - Setup and deployment
- [API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md) - Quick API reference

**Troubleshooting:** See [SETUP.md - Troubleshooting](SETUP.md#troubleshooting)

**Build Status:** ✅ 0 errors, 0 warnings  
**Production Ready:** ✅ YES

---

*Last Updated: December 10, 2025*  
*Version: 2.0 (Rental Agreement System Update)*
