$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$webClient = New-Object System.Net.WebClient
$webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$data = $webClient.DownloadString("ftp://$ftpHost/www/wwwroot/MartinFlegl/")
Write-Host $data
