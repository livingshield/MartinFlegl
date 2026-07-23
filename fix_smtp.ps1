$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$ftpHost = "windows11.aspone.cz"

function Upload-FtpFile($localFile, $remotePath) {
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$remotePath")
        $webClient.UploadFile($uri, $localFile)
        Write-Host "Uploaded $localFile -> $remotePath"
    } catch {
        Write-Host "Error uploading $remotePath : $($_.Exception.Message)"
    }
}

$envFile = "C:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\.env"
$appsettingsFile = "C:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\appsettings.json"
$webConfigFile = "C:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\web.config"
$dllFile = "C:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\publish\MartinFlegl.Web.dll"

Upload-FtpFile $envFile "www/MartinFlegl/.env"
Upload-FtpFile $envFile "www/wwwroot/MartinFlegl/.env"

Upload-FtpFile $appsettingsFile "www/MartinFlegl/appsettings.json"
Upload-FtpFile $appsettingsFile "www/wwwroot/MartinFlegl/appsettings.json"

Upload-FtpFile $webConfigFile "www/MartinFlegl/web.config"
Upload-FtpFile $webConfigFile "www/wwwroot/MartinFlegl/web.config"

Upload-FtpFile $dllFile "www/MartinFlegl/MartinFlegl.Web.dll"
Upload-FtpFile $dllFile "www/wwwroot/MartinFlegl/MartinFlegl.Web.dll"
