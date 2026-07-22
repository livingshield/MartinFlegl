$urls = @(
    "http://ekobio.org/MartinFlegl/index.html",
    "http://ekobio.org/flegl.html"
)

foreach ($url in $urls) {
    Write-Host "=== Checking: $url ==="
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing
        Write-Host "Status: $($r.StatusCode)"
        Write-Host "Content length: $($r.Content.Length)"
        if ($r.Content -match "case-study") {
            Write-Host "Case Study: FOUND"
        } else {
            Write-Host "Case Study: NOT FOUND"
        }
    } catch {
        Write-Host "ERROR: $($_.Exception.Message)"
    }
    Write-Host ""
}
