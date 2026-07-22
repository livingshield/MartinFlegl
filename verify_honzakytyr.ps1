$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$remoteBase = "www/wwwroot/HonzaKytyr/"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\HonzaKytyr.Web\wwwroot"

function Upload-File {
    param($lPath, $rPath)
    Write-Host "Uploading $lPath to $rPath ..."
    try {
        $wc = New-Object System.Net.WebClient
        $wc.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$rPath")
        $wc.UploadFile($uri, $lPath)
        Write-Host "OK: $rPath"
    }
    catch {
        Write-Host "FAIL: $rPath - $($_.Exception.Message)"
    }
}

Upload-File ($localPath + "\index.html") ($remoteBase + "index.html")
Upload-File ($localPath + "\qr_kod.png") ($remoteBase + "qr_kod.png")
Upload-File ($localPath + "\honzagg_foto.png") ($remoteBase + "honzagg_foto.png")
