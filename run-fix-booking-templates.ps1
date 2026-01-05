Write-Host "Updating booking email templates to show 'Reserved' instead of 'Confirmed'" -ForegroundColor Cyan

$Server = "ryve-postgres-new.postgres.database.azure.com"
$Database = "ghanarentaldb"
$Username = "ryveadmin"
$Password = "RyveDb@2025!Secure#123"
$sqlFile = "fix-booking-reserved-template.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: $sqlFile not found!" -ForegroundColor Red
    exit 1
}

$connectionString = "Host=$Server;Port=5432;Database=$Database;Username=$Username;Password=$Password;SSL Mode=Require;Trust Server Certificate=true"

# Try to load Npgsql
$npgsqlPath = "$env:USERPROFILE\.nuget\packages\npgsql\8.0.1\lib\net8.0\Npgsql.dll"
if (Test-Path $npgsqlPath) {
    Add-Type -Path $npgsqlPath
} else {
    # Try newer version
    $npgsqlDir = "$env:USERPROFILE\.nuget\packages\npgsql"
    if (Test-Path $npgsqlDir) {
        $latestVersion = Get-ChildItem $npgsqlDir | Sort-Object Name -Descending | Select-Object -First 1
        $dllPath = Join-Path $latestVersion.FullName "lib\net8.0\Npgsql.dll"
        if (Test-Path $dllPath) {
            Add-Type -Path $dllPath
        }
    }
}

try {
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    Write-Host "Connected to database" -ForegroundColor Green
    
    $sqlContent = Get-Content $sqlFile -Raw
    
    # Execute customer template update
    Write-Host "`nUpdating customer template..." -ForegroundColor Yellow
    $cmd1 = $connection.CreateCommand()
    $cmd1.CommandText = $sqlContent.Split("-- 2.")[0]
    $rows1 = $cmd1.ExecuteNonQuery()
    Write-Host "Customer template updated: $rows1 row(s)" -ForegroundColor Green
    
    # Execute owner template updates
    Write-Host "`nUpdating owner template..." -ForegroundColor Yellow
    $ownerUpdates = $sqlContent.Split("-- 2.")[1].Split("-- Verify")[0]
    $ownerStatements = $ownerUpdates -split ";\s*UPDATE" | Where-Object { $_.Trim() -ne "" }
    
    foreach ($stmt in $ownerStatements) {
        $sql = if ($stmt.StartsWith("UPDATE")) { $stmt } else { "UPDATE" + $stmt }
        if ($sql.Trim() -eq "" -or $sql -match "^--") { continue }
        
        $cmd = $connection.CreateCommand()
        $cmd.CommandText = $sql.Trim() + ";"
        $rows = $cmd.ExecuteNonQuery()
        Write-Host "Owner template updated: $rows row(s)" -ForegroundColor Green
    }
    
    # Verify updates
    Write-Host "`nVerifying updates..." -ForegroundColor Yellow
    $verifyCmd = $connection.CreateCommand()
    $verifyCmd.CommandText = "SELECT `"TemplateName`", `"Subject`", CASE WHEN `"BodyTemplate`" LIKE '%RESERVED%' THEN 'Updated' ELSE 'Not updated' END as status FROM `"EmailTemplates`" WHERE `"TemplateName`" IN ('booking_confirmation_customer', 'booking_confirmation_owner') ORDER BY `"TemplateName`""
    $reader = $verifyCmd.ExecuteReader()
    
    while ($reader.Read()) {
        Write-Host "$($reader[0]): $($reader[1]) - $($reader[2])" -ForegroundColor Cyan
    }
    $reader.Close()
    
    $connection.Close()
    Write-Host "`nEmail templates updated successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
