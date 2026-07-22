# Check what's at www/MartinFlegl/wwwroot/ on FTP
$ftp = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/MartinFlegl/wwwroot/')
$ftp.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
$response = $ftp.GetResponse()
$reader = New-Object System.IO.StreamReader($response.GetResponseStream())
$reader.ReadToEnd()
$reader.Close()
$response.Close()

Write-Host "---"

# Check file size of index.html on FTP  
$ftp2 = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/MartinFlegl/wwwroot/index.html')
$ftp2.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp2.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
$response2 = $ftp2.GetResponse()
Write-Host "FTP index.html size: $($response2.ContentLength) bytes"
$response2.Close()

# Also check old location
$ftp3 = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/MartinFlegl/index.html')
$ftp3.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp3.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
$response3 = $ftp3.GetResponse()
Write-Host "FTP root index.html size: $($response3.ContentLength) bytes"
$response3.Close()
