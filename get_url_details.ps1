$url = "http://ekobio.org/MartinFlegl/"
$resp = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
Write-Host "Status: $($resp.StatusCode)"
Write-Host "Headers:"
$resp.Headers
Write-Host "`nPreview (First 500 chars):"
$resp.Content.Substring(0, [Math]::Min(500, $resp.Content.Length))
