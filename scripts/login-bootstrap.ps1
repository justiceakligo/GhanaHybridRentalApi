$api='http://4.149.192.12'
$email='bootstrap-admin@ryve.test'
$pass='Test12345'
$login=@{ Identifier=$email; Password=$pass } | ConvertTo-Json
try {
    $resp=Invoke-RestMethod -Uri "$api/api/v1/auth/login" -Method Post -Body $login -ContentType 'application/json' -ErrorAction Stop
    $resp | ConvertTo-Json -Depth 5
    Set-Content -Path admin_token.txt -Value $resp.token -Force
    Write-Host 'Saved admin_token.txt' -ForegroundColor Green
} catch {
    Write-Host 'Login failed:' $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.Response) { $reader=[System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream()); Write-Host $reader.ReadToEnd() }
    exit 1
}