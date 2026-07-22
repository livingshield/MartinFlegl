$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localWwwroot = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot"
$remoteBase = "www/wwwroot/MartinFlegl/"

function Upload-File($localFile, $remoteFile) {
    Write-Host "Uploading $localFile -> $remoteFile ..."
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$remoteFile")
        $webClient.UploadFile($uri, $localFile)
        Write-Host "  OK"
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)"
    }
}

function Ensure-RemoteDir($remotePath) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$remotePath")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.GetResponse().Close()
        Write-Host "Created directory: $remotePath"
    }
    catch {}
}

function Upload-Folder($localPath, $remotePath) {
    $items = Get-ChildItem $localPath
    foreach ($item in $items) {
        $target = $remotePath + $item.Name
        if ($item.PSIsContainer) {
            Ensure-RemoteDir $target
            Upload-Folder $item.FullName ($target + "/")
        }
        else {
            Upload-File $item.FullName $target
        }
    }
}

Write-Host "=== Deploying MartinFlegl.Web to FTP ==="
Write-Host ""

# Ensure base directories exist
Ensure-RemoteDir "www/MartinFlegl"
Ensure-RemoteDir "www/MartinFlegl/css"
Ensure-RemoteDir "www/MartinFlegl/js"
Ensure-RemoteDir "www/MartinFlegl/img"

# Upload index.html
Upload-File "$localWwwroot\index.html" ($remoteBase + "index.html")

# Upload CSS
Upload-Folder "$localWwwroot\css" ($remoteBase + "css/")

# Upload JS
Upload-Folder "$localWwwroot\js" ($remoteBase + "js/")

# Upload images
Upload-Folder "$localWwwroot\img" ($remoteBase + "img/")

# Upload root-level assets (foto, qr)
Upload-File "$localWwwroot\foto.png" ($remoteBase + "foto.png")
Upload-File "$localWwwroot\qr_kod.png" ($remoteBase + "qr_kod.png")

Write-Host ""
Write-Host "=== Deploy complete! ==="
Write-Host "URL: https://ekobio.org/MartinFlegl/"
