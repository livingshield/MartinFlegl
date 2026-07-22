$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$remoteFile = "www/wwwroot/index.html.gz"
$localGz = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\backup_index.html.gz"
$localDecompressed = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\restored_index.html"

# Download
$webClient = New-Object System.Net.WebClient
$webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$uri = New-Object System.Uri("ftp://$ftpHost/$remoteFile")
Write-Host "Downloading $remoteFile ..."
$webClient.DownloadFile($uri, $localGz)

# Decompress
Write-Host "Decompressing ..."
$inStream = New-Object System.IO.FileStream($localGz, [System.IO.FileMode]::Open)
$gzStream = New-Object System.IO.Compression.GZipStream($inStream, [System.IO.Compression.CompressionMode]::Decompress)
$outStream = New-Object System.IO.FileStream($localDecompressed, [System.IO.FileMode]::Create)

$gzStream.CopyTo($outStream)

$outStream.Close()
$gzStream.Close()
$inStream.Close()

# Upload
Write-Host "Uploading back to index.html ..."
$uriUpload = New-Object System.Uri("ftp://$ftpHost/www/wwwroot/index.html")
$webClient.UploadFile($uriUpload, $localDecompressed)
Write-Host "Restore complete."

# Cleanup my uploaded files just in case
function Delete-FtpFile($file) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$file")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $req.GetResponse().Close()
        Write-Host "Deleted $file"
    }
    catch {}
}

Delete-FtpFile "www/wwwroot/flegl.html"
Delete-FtpFile "www/wwwroot/css/main.css"
Delete-FtpFile "www/wwwroot/js/animations.js"
Delete-FtpFile "www/wwwroot/img/hero-bg.jpg"
Delete-FtpFile "www/wwwroot/qr_kód.png"
Delete-FtpFile "www/wwwroot/foto.png"
