$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

function Delete-FtpFile($path) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$path")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $response = $req.GetResponse()
        $response.Close()
        Write-Host "Deleted: $path"
    } catch {
        Write-Host "Skip/Not found: $path"
    }
}

Write-Host "=== Clearing compressed artifacts from FTP ==="
Delete-FtpFile "www/wwwroot/MartinFlegl/index.html.gz"
Delete-FtpFile "www/wwwroot/MartinFlegl/index.html.br"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/index.html.gz"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/index.html.br"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/css/main.css.gz"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/css/main.css.br"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/js/admin.js.gz"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/js/admin.js.br"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/js/animations.js.gz"
Delete-FtpFile "www/wwwroot/MartinFlegl/wwwroot/js/animations.js.br"
Write-Host "=== Cleanup complete ==="
