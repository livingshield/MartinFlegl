$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$webClient = New-Object System.Net.WebClient
$webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$webClient.DownloadFile("ftp://$ftpHost/www/MartinFlegl/index.html", "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\ftp_index.html")
Write-Host "Downloaded FTP index.html"
