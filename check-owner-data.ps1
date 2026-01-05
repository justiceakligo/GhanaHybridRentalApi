param(
    [string]$Server = "ryve-postgres-new.postgres.database.azure.com",
    [string]$Database = "ghanarentaldb",
    [string]$Username = "ryveadmin",
    [string]$Password = "RyveDb@2025!Secure#123"
)

Write-Host "Checking for owner@test.com and their vehicles..." -ForegroundColor Cyan

# Create connection string
$connectionString = "Host=$Server;Port=5432;Database=$Database;Username=$Username;Password=$Password;SSL Mode=Require;Trust Server Certificate=true"

# Load Npgsql
Add-Type -Path "$env:USERPROFILE\.nuget\packages\npgsql\8.0.1\lib\net8.0\Npgsql.dll" -ErrorAction SilentlyContinue

try {
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "`n1. Checking if user exists..." -ForegroundColor Yellow
    $cmd1 = $connection.CreateCommand()
    $cmd1.CommandText = "SELECT id, email, fullname, role, createdat FROM users WHERE email = 'owner@test.com'"
    $reader1 = $cmd1.ExecuteReader()
    
    $ownerId = $null
    if ($reader1.Read()) {
        $ownerId = $reader1["id"]
        Write-Host "✓ Owner found:" -ForegroundColor Green
        Write-Host "  - ID: $($reader1['id'])"
        Write-Host "  - Email: $($reader1['email'])"
        Write-Host "  - Name: $($reader1['fullname'])"
        Write-Host "  - Role: $($reader1['role'])"
        Write-Host "  - Created: $($reader1['createdat'])"
    } else {
        Write-Host "✗ No user found with email owner@test.com" -ForegroundColor Red
    }
    $reader1.Close()
    
    if ($ownerId) {
        Write-Host "`n2. Checking vehicles for this owner..." -ForegroundColor Yellow
        $cmd2 = $connection.CreateCommand()
        $cmd2.CommandText = @"
SELECT 
    v.id, 
    v.make, 
    v.model, 
    v.year, 
    v.registrationnumber, 
    v.dailyrate, 
    v.status,
    v.createdat
FROM vehicles v
WHERE v.ownerid = @ownerId
ORDER BY v.createdat DESC
"@
        $param = $cmd2.CreateParameter()
        $param.ParameterName = "ownerId"
        $param.Value = $ownerId
        $cmd2.Parameters.Add($param) | Out-Null
        
        $reader2 = $cmd2.ExecuteReader()
        $count = 0
        
        while ($reader2.Read()) {
            $count++
            if ($count -eq 1) {
                Write-Host "✓ Vehicles found:" -ForegroundColor Green
            }
            Write-Host "`nVehicle #$count:" -ForegroundColor Cyan
            Write-Host "  - ID: $($reader2['id'])"
            Write-Host "  - Make/Model: $($reader2['make']) $($reader2['model'])"
            Write-Host "  - Year: $($reader2['year'])"
            Write-Host "  - Registration: $($reader2['registrationnumber'])"
            Write-Host "  - Daily Rate: $($reader2['dailyrate'])"
            Write-Host "  - Status: $($reader2['status'])"
            Write-Host "  - Created: $($reader2['createdat'])"
        }
        $reader2.Close()
        
        if ($count -eq 0) {
            Write-Host "✗ No vehicles found for this owner" -ForegroundColor Red
        } else {
            Write-Host "`nTotal vehicles: $count" -ForegroundColor Green
        }
    }
    
    $connection.Close()
} catch {
    Write-Host "Error connecting to database: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
