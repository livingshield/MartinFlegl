$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$remoteBase = "www/wwwroot/MartinFlegl/"

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

Upload-File "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\qr_kód.png" ($remoteBase + "qr_kód.png")
Upload-File "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\foto.png" ($remoteBase + "foto.png")
