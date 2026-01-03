# Execute email template insertion SQL
$ErrorActionPreference = "Stop"

Write-Host "Inserting professional email templates into database..." -ForegroundColor Cyan

# Read the SQL file
$sqlContent = Get-Content -Path "insert-email-templates.sql" -Raw

# Database connection parameters
$server = "ryve-postgres-new.postgres.database.azure.com"
$database = "ghanarentaldb_new"
$username = "ryveadmin"
$password = 'RyveDb@2025!Secure#123'

# Create connection string
$connectionString = "Host=$server;Port=5432;Database=$database;Username=$username;Password=$password;SSL Mode=Require;"

try {
    # Try using Azure CLI with proper escaping
    Write-Host "Attempting to execute via Azure CLI..." -ForegroundColor Yellow
    
    # Save SQL to temp file for Azure CLI
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $sqlContent | Out-File -FilePath $tempFile -Encoding UTF8
    
    # Execute using Azure CLI
    az postgres flexible-server execute `
        --name "ryve-postgres-new" `
        --database-name "ghanarentaldb" `
        --admin-user "ryvepgadmin" `
        --admin-password 'Qr97@12&%' `
        --file-path $tempFile
    
    Remove-Item $tempFile -Force
    
    Write-Host "`n✅ Email templates inserted successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "`n⚠️ Azure CLI method failed: $_" -ForegroundColor Red
    Write-Host "`nPlease run this SQL manually in Azure Portal Query Editor or pgAdmin" -ForegroundColor Yellow
    Write-Host "SQL file location: insert-email-templates.sql" -ForegroundColor Cyan
}
