$api='http://4.149.192.12'
$email='bootstrap-admin@ryve.test'
$pass='Test12345'
$body=@{ Email=$email; Password=$pass; Role='admin' } | ConvertTo-Json
try {
    $r=Invoke-RestMethod -Uri "$api/api/v1/auth/register" -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop
    Write-Host 'Registered admin' -ForegroundColor Green
    $r
} catch {
    Write-Host 'Register failed: ' $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.Response) { $reader=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $reader.ReadToEnd() }
    exit 1
}