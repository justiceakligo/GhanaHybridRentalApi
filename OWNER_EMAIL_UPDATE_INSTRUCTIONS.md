# CODE UPDATE NEEDED - NotificationService.cs

## Location: Services/NotificationService.cs, Line 325

### Change Required:
Add `owner_total` placeholder calculation for booking confirmation email to owner.

### FIND (around line 310-328):
```csharp
            { "vehicle_type", booking.WithDriver ? "With Driver" : "Self-Drive" },
            { "currency", booking.Currency },
            { "rental_amount", booking.RentalAmount.ToString("F2") },
            { "driver_amount", (booking.DriverAmount ?? 0).ToString("F2") },
            { "total_amount", booking.TotalAmount.ToString("F2") },
            { "support_email", "support@ryverental.com" }
        };
```

### REPLACE WITH:
```csharp
            { "vehicle_type", booking.WithDriver ? "With Driver" : "Self-Drive" },
            { "currency", booking.Currency },
            { "rental_amount", booking.RentalAmount.ToString("F2") },
            { "driver_amount", (booking.DriverAmount ?? 0).ToString("F2") },
            { "owner_total", (booking.RentalAmount + (booking.DriverAmount ?? 0)).ToString("F2") },
            { "support_email", "support@ryverental.com" }
        };
```

### Explanation:
- Removed `total_amount` (confusing - includes platform fees, insurance, etc.)
- Added `owner_total` = rental_amount + driver_amount (what owner actually receives)
- Owner gets rental earnings + driver fees (and pays driver from driver fees)
- Makes earnings breakdown crystal clear in email

---

## STEPS TO APPLY:

1. Run SQL script to update email templates:
   ```powershell
   az postgres flexible-server execute --name ryve-postgres-new --database-name ghanarentaldb --admin-user ryveadmin --admin-password 'RyveDb@2025!Secure#123' --file-path "update-owner-email-templates.sql"
   ```

2. Update NotificationService.cs with the code change above

3. Commit, build, and deploy:
   ```powershell
   git add Services/NotificationService.cs
   git commit -m "Update owner email placeholders for clear earnings breakdown"
   docker build -t ryveacrnewawjs.azurecr.io/ghana-rental-api:1.209 .
   docker push ryveacrnewawjs.azurecr.io/ghana-rental-api:1.209
   # Update deploy script to use 1.209 and run
   ```
