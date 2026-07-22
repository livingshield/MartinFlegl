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
        Write-Host "Skip/Not found: $path - $($_.Exception.Message)"
    }
}

# Delete the file in the "wrong" directory that might be taking precedence
Delete-FtpFile "www/MartinFlegl/index.html"
Delete-FtpFile "www/flegl.html"
