$r = Invoke-WebRequest -Uri 'http://ekobio.org/MartinFlegl/' -UseBasicParsing
Write-Host "Content length: $($r.Content.Length)"
if ($r.Content -match "case-study") {
    Write-Host "Case Study: FOUND"
} else {
    Write-Host "Case Study: NOT FOUND (serving old cached version)"
}
