# Email Template Database Update Instructions

## ✅ v1.239 Deployed Successfully
**New IP:** `48.200.18.172`

## 📧 Database Update Required

The API code has been updated to provide all the new placeholders (`protection_amount`, `platform_fee`, `deposit_amount`, `promo_discount`, `promo_display`), but the email templates in the database still need to be updated.

## Option 1: Using VS Code PostgreSQL Extension (RECOMMENDED)

1. Open the file: **`template1.sql`**
2. Click on the PostgreSQL icon in the left sidebar
3. Right-click on the connection: **`ryve-postgres-new.postgres.database.azure.com, ghanarentaldb (ryveadmin)`**
4. Select **"New Query"**
5. Copy the entire contents of `template1.sql` and paste into the query window
6. Click **"Execute"** or press **F5**
7. Repeat steps 1-6 for **`template2.sql`**

## Option 2: Using Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **PostgreSQL flexible servers** > **ryve-postgres-new**
3. Click **"Query editor (preview)"** in the left menu
4. Login with:
   - **Username:** `ryveadmin`
   - **Password:** `RyveDb@2025!Secure#123`
   - **Database:** `ghanarentaldb`
5. Open **`update-email-breakdown-fixed.sql`** in a text editor
6. Copy the entire contents
7. Paste into Azure Query Editor
8. Click **"Run"**

## Option 3: Using PowerShell with Docker PostgreSQL Client

```powershell
# Ensure Docker is running
docker run --rm -i -v "${PWD}:/sql" postgres:15 psql "postgresql://ryveadmin:RyveDb@2025!Secure#123@ryve-postgres-new.postgres.database.azure.com:5432/ghanarentaldb?sslmode=require" -f /sql/update-email-breakdown-fixed.sql
```

## ✅ Verify Updates

After running the SQL, verify with this query:

```sql
SELECT 
    "TemplateName",
    "Subject",
    "UpdatedAt",
    LENGTH("BodyTemplate") as "TemplateSize"
FROM "EmailTemplates"
WHERE "TemplateName" IN ('booking_confirmation_customer', 'booking_confirmed')
ORDER BY "TemplateName";
```

Expected results:
- `booking_confirmation_customer` - TemplateSize should be ~14,000+ characters
- `booking_confirmed` - TemplateSize should be ~14,000+ characters
- Both `UpdatedAt` timestamps should be recent (today)

## 📋 What Gets Updated

### Template 1: `booking_confirmation_customer` (Booking Reserved)
- Complete pricing breakdown with all line items
- Protection Plan display
- Platform Fee (15% of rental + driver)
- Security Deposit (refundable) highlighted in blue
- Promo Discount (conditional display when applicable)
- Professional responsive HTML design

### Template 2: `booking_confirmed` (Payment Confirmed)  
- Same complete breakdown as above
- QR code for quick check-in
- Owner contact details
- Pickup instructions

## 🎯 Next Steps After Update

1. **Update Cloudflare DNS** to new IP: `48.200.18.172`
2. **Test** by creating a new booking
3. **Verify** rental agreement PDF opens correctly
4. **Check** QR code displays in booking confirmed email
5. **Confirm** email templates show complete pricing breakdown

## 📁 Files Reference

- `template1.sql` - Updates booking_confirmation_customer template
- `template2.sql` - Updates booking_confirmed template
- `update-email-breakdown-fixed.sql` - Combined file with both updates
- Original file with wrong column names: `update-email-breakdown.sql` (don't use this)

## 🔍 Troubleshooting

**If you see errors about column names:**
- Make sure you're using `update-email-breakdown-fixed.sql` (with correct column names)
- The fixed version uses `TemplateName` and `BodyTemplate` (not `TemplateCode` and `HtmlTemplate`)

**If execution hangs or times out:**
- The SQL file is ~28KB due to HTML templates
- Try executing `template1.sql` and `template2.sql` separately
- Increase timeout in your SQL client if available

## ✅ Success Indicators

After successful update, booking emails will show:

```
💰 Pricing Breakdown
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Vehicle (2 days)              GHS 2,000.00
Driver Service                GHS 0.00
🛡️ Protection Plan            GHS 100.00
📊 Platform Fee               GHS 300.00
🔒 Security Deposit (Refundable) GHS 1,600.00
🎁 Promo Discount            -GHS 50.00    (if applicable)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL                         GHS 3,950.00
```

✓ Includes GHS 1,600.00 refundable security deposit
