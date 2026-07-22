$ftp = [System.Net.WebRequest]::Create('ftp://windows11.aspone.cz/www/web.config')
$ftp.Credentials = New-Object System.Net.NetworkCredential('EkoBio.org_lordkikin', 'Brzsilpot7!')
$ftp.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
$response = $ftp.GetResponse()
$reader = New-Object System.IO.StreamReader($response.GetResponseStream())
$reader.ReadToEnd()
$reader.Close()
$response.Close()
