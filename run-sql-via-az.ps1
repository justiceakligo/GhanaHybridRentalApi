Write-Host "Connecting to PostgreSQL and executing SQL..." -ForegroundColor Cyan

$Server = "ryve-postgres-new.postgres.database.azure.com"
$Database = "ghanarentaldb"
$Username = "ryveadmin"
$Password = "RyveDb@2025!Secure#123"

# Read SQL file
$sqlFile = "fix-booking-reserved-template.sql"
$sqlContent = Get-Content $sqlFile -Raw

# Escape single quotes for PostgreSQL
$sqlEscaped = $sqlContent -replace "'", "''"

# Create a temp SQL file for execution
$tempSql = "temp-update.sql"
Set-Content -Path $tempSql -Value $sqlContent

# Try using Azure CLI postgres commands
Write-Host "Attempting to connect via Azure CLI..." -ForegroundColor Yellow

try {
    # Check if az is available
    $azCheck = az --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Azure CLI found" -ForegroundColor Green
        
        # Execute using az postgres
        Write-Host "Executing SQL script..." -ForegroundColor Yellow
        $result = az postgres flexible-server execute `
            --name "ryve-postgres-new" `
            --admin-user $Username `
            --admin-password $Password `
            --database-name $Database `
            --file-path $tempSql `
            2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SQL executed successfully!" -ForegroundColor Green
            Write-Host $result
        } else {
            Write-Host "Error executing SQL:" -ForegroundColor Red
            Write-Host $result
        }
    }
} catch {
    Write-Host "Azure CLI method failed: $_" -ForegroundColor Yellow
}

# Cleanup
if (Test-Path $tempSql) {
    Remove-Item $tempSql
}
