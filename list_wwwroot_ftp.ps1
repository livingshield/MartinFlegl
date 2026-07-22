$ftp = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/wwwroot/apps/')
$ftp.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
try {
    $response = $ftp.GetResponse()
    $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
    Write-Host "=== www/wwwroot/apps/ ==="
    Write-Host $reader.ReadToEnd()
    $reader.Close()
    $response.Close()
} catch {
    Write-Host "No www/wwwroot/apps/"
}

$ftp2 = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/apps/')
$ftp2.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp2.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
try {
    $response2 = $ftp2.GetResponse()
    $reader2 = New-Object System.IO.StreamReader($response2.GetResponseStream())
    Write-Host "=== www/apps/ ==="
    Write-Host $reader2.ReadToEnd()
    $reader2.Close()
    $response2.Close()
} catch {
    Write-Host "No www/apps/"
}
