$api='http://4.149.192.12'
$login=@{ Identifier='cyriladmin@ryverental.com'; Password='CAdd@123' } | ConvertTo-Json
try {
    $resp=Invoke-RestMethod -Uri "$api/api/v1/auth/login" -Method Post -Body $login -ContentType 'application/json' -ErrorAction Stop
    $resp | ConvertTo-Json -Depth 5
    Set-Content -Path admin_token_cyril.txt -Value $resp.token -Force
    Write-Host 'Saved admin_token_cyril.txt' -ForegroundColor Green
} catch {
    Write-Host 'Login failed for cyriladmin: ' $_.Exception.Message -ForegroundColor Yellow
    if ($_.Exception.Response) { $reader=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $reader.ReadToEnd() }
    exit 1
}