$ftpHost = "windows12.aspone.cz"
$user = "martinflegl.cz"
$pass = "Trutnov-2026"

try {
    $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/")
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $resp = $req.GetResponse()
    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $content = $reader.ReadToEnd()
    $reader.Close()
    $resp.Close()
    Write-Host "Root FTP directory listing for windows12.aspone.cz:"
    Write-Host $content

    # Also check www folder if it exists
    $req2 = [System.Net.WebRequest]::Create("ftp://$ftpHost/www/")
    $req2.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req2.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $resp2 = $req2.GetResponse()
    $reader2 = New-Object System.IO.StreamReader($resp2.GetResponseStream())
    $content2 = $reader2.ReadToEnd()
    $reader2.Close()
    $resp2.Close()
    Write-Host "www/ FTP directory listing for windows12.aspone.cz:"
    Write-Host $content2
} catch {
    Write-Host "FTP Error:" $_.Exception.Message
}
