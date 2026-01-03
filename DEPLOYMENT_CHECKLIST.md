# Deployment Checklist & Configuration Guide

## Azure Blob Storage Configuration

### Required Environment Variables

For the Azure Container Instance (ACI) deployment, ensure the following environment variable is set:

```
AZURE_STORAGE_CONNECTION_STRING=<your-azure-storage-connection-string>
```

### How to Set in Azure Container Instance

1. **Via Azure Portal:**
   - Go to your Container Instance
   - Navigate to "Containers" → Select your container
   - Go to "Environment variables"
   - Add: `AZURE_STORAGE_CONNECTION_STRING` with your connection string value

2. **Via Azure CLI:**
   ```bash
   az container create \
     --resource-group <resource-group> \
     --name <container-name> \
     --image <image-name> \
     --environment-variables \
       AZURE_STORAGE_CONNECTION_STRING="<connection-string>"
   ```

3. **Via YAML deployment file:**
   ```yaml
   apiVersion: '2021-09-01'
   location: westus
   properties:
     containers:
     - name: ghanarentalapi
       properties:
         image: <your-image>
         environmentVariables:
         - name: 'AZURE_STORAGE_CONNECTION_STRING'
           secureValue: '<your-connection-string>'
   ```

### Finding Your Connection String

1. Go to Azure Portal
2. Navigate to your Storage Account
3. Click "Access keys" in the left menu
4. Copy "Connection string" from key1 or key2

### Verification

The application will:
- First check `appsettings.json` → `AzureStorage:ConnectionString`
- Then fallback to environment variable `AZURE_STORAGE_CONNECTION_STRING`
- Throw clear error if neither is configured

## Owner Account Verification System

### How It Works

The system has a three-tier status system for users:

1. **pending** - Newly registered, awaiting admin approval
2. **active** - Verified and allowed to operate
3. **suspended** - Blocked from all operations

### Owner Registration Flow

1. **Owner registers** via `/api/v1/auth/register` with `role: "owner"`
   - Account created with `status: "pending"`
   - Cannot login yet (will receive error message)

2. **Admin verifies owner** via `/api/v1/admin/pending-owners/{userId}/verify`
   - Admin reviews owner details
   - Approves or rejects the account
   - On approval: `user.Status` changes from `pending` → `active`

3. **Owner can now login and operate**
   - Login requires `status: "active"` for owners/admins
   - Renters can operate while `pending` (different flow)

### Admin Actions for Owner Verification

**Get Pending Owners:**
```http
GET /api/v1/admin/pending-owners
```

**Approve/Reject Owner:**
```http
POST /api/v1/admin/pending-owners/{userId}/verify
Content-Type: application/json

{
  "approve": true,  // or false to reject
  "type": "account"
}
```

**Update Any User Status:**
```http
PUT /api/v1/admin/users/{userId}/status
Content-Type: application/json

{
  "status": "active"  // or "pending", "suspended"
}
```

### Status Enforcement

- **Login:** Owners and admins with `status != "active"` cannot login
- **Operations:** Even if token exists, pending owners get 403 errors on operations
- **Renters:** Can operate while pending (simpler flow for customers)

### Error Messages

When pending owner tries to login:
```json
{
  "error": "Your account is pending verification. Please contact support."
}
```

When pending owner tries to use owner endpoints:
```json
{
  "error": "Your owner account is pending verification. Please contact support."
}
```

## Database Migration Required

After deploying this update, run the migration to add the new fields:

```bash
dotnet ef database update
```

This adds:
- `Booking.UpdatedAt` field
- `Booking.PaymentTransaction` navigation property

## Booking Response Enhancements

The booking API now includes:

1. **Owner Details:**
   ```json
   "owner": {
     "id": "...",
     "firstName": "John",
     "lastName": "Doe",
     "email": "owner@example.com",
     "phone": "233...",
     "companyName": "Rental Co.",
     "businessAddress": "123 Street"
   }
   ```

2. **Protection Plan Details:**
   - Uses snapshot data when available (preserves plan at booking time)
   - Falls back to current plan if no snapshot
   - Includes all plan details (code, name, price, deductible, etc.)

3. **Payment Information:**
   ```json
   "paymentDate": "2025-12-27T10:30:00Z",  // When payment completed
   "updatedAt": "2025-12-27T10:35:00Z"     // Last booking update
   ```

## Testing Checklist

- [ ] Azure Blob Storage uploads working for authenticated users
- [ ] Azure Blob Storage uploads working for anonymous/guest users
- [ ] Pending owner cannot login
- [ ] Active owner can login and operate
- [ ] Admin can approve pending owners
- [ ] Booking responses include owner details
- [ ] Booking responses include protection plan details
- [ ] Booking responses include payment date
- [ ] Database migration applied successfully

## Support

For issues or questions:
- Email: developers@ryverental.com
- Check application logs in Azure Container Instance for detailed errors
