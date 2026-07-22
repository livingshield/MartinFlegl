$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

function Get-FtpText($path) {
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        return $webClient.DownloadString("ftp://$ftpHost/$path")
    } catch {
        return "ERROR: $($_.Exception.Message)"
    }
}

Write-Host "--- Downloading www/MartinFlegl/index.html ---"
$html1 = Get-FtpText "www/MartinFlegl/index.html"
Write-Host "Length: $($html1.Length)"
Write-Host "Has admin trigger: $($html1.Contains('admin-login-trigger'))"
Write-Host "Has 160px: $($html1.Contains('160px'))"

Write-Host "`n--- Downloading www/MartinFlegl/wwwroot/index.html ---"
$html2 = Get-FtpText "www/MartinFlegl/wwwroot/index.html"
Write-Host "Length: $($html2.Length)"
Write-Host "Has admin trigger: $($html2.Contains('admin-login-trigger'))"

Write-Host "`n--- Downloading www/MartinFlegl/css/main.css ---"
$css1 = Get-FtpText "www/MartinFlegl/css/main.css"
Write-Host "Length: $($css1.Length)"
Write-Host "Has 160px: $($css1.Contains('160px'))"
