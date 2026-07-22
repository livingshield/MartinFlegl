# Deploy static frontend files to the correct server path
$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localWwwroot = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot"

# The main app serves from www/wwwroot/, so MartinFlegl goes to www/wwwroot/MartinFlegl/
$remoteBase = "www/wwwroot/MartinFlegl/"

function Upload-File($localFile, $remoteFile) {
    Write-Host "  $($localFile | Split-Path -Leaf) -> $remoteFile"
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$remoteFile")
        $webClient.UploadFile($uri, $localFile)
    }
    catch {
        Write-Host "    ERROR: $($_.Exception.Message)"
    }
}

function Ensure-RemoteDir($remotePath) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$remotePath")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.GetResponse().Close()
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

Write-Host "=== Deploying MartinFlegl static files to wwwroot/MartinFlegl/ ==="

# Ensure directories exist
Ensure-RemoteDir "www/wwwroot/MartinFlegl"
Ensure-RemoteDir "www/wwwroot/MartinFlegl/css"
Ensure-RemoteDir "www/wwwroot/MartinFlegl/js"
Ensure-RemoteDir "www/wwwroot/MartinFlegl/img"

# Upload frontend files
Upload-File "$localWwwroot\index.html" ($remoteBase + "index.html")
Upload-Folder "$localWwwroot\css" ($remoteBase + "css/")
Upload-Folder "$localWwwroot\js" ($remoteBase + "js/")
Upload-Folder "$localWwwroot\img" ($remoteBase + "img/")
Upload-File "$localWwwroot\foto.png" ($remoteBase + "foto.png")
Upload-File "$localWwwroot\qr_kod.png" ($remoteBase + "qr_kod.png")

# Also update the flegl.html at root level (www/)  
Upload-File "$localWwwroot\index.html" "www/flegl.html"

Write-Host ""
Write-Host "=== Deploy complete! ==="
Write-Host "Try: http://ekobio.org/MartinFlegl/index.html"
Write-Host "Or:  http://ekobio.org/flegl.html"
