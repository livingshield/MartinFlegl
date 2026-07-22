$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

function Get-FtpList($path) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$path")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
        $response = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $data = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()
        return $data
    } catch {
        return "ERROR: $($_.Exception.Message)"
    }
}

Write-Host "--- www/ ---"
Get-FtpList "www/"
Write-Host "`n--- www/wwwroot/ ---"
Get-FtpList "www/wwwroot/"
