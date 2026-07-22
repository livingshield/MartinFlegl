$r = Invoke-WebRequest -Uri 'http://ekobio.org/MartinFlegl/' -UseBasicParsing
$r.Content.Substring(0, 500)
Write-Host "---"
if ($r.Content -match "case-study") {
    Write-Host "Case Study section FOUND"
} else {
    Write-Host "Case Study section NOT FOUND"
}
Write-Host "Content length: $($r.Content.Length)"
