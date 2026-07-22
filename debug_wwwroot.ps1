$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

function Get-FtpList($url) {
    Write-Host "Listing $url ..."
    try {
        $ftp = [System.Net.WebRequest]::Create($url)
        $ftp.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $ftp.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
        $response = $ftp.GetResponse()
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $output = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()
        return $output
    }
    catch {
        return "Error: $($_.Exception.Message)"
    }
}

Write-Host "--- WWWRoot Directory Content ---"
$list = Get-FtpList "ftp://$ftpHost/www/wwwroot/"
$list
