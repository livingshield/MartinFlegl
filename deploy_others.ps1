$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

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

Write-Host "=== Deploying HonzaGG and HonzaKytyr to wwwroot/ subfolders ==="

# HonzaGG
Ensure-RemoteDir "www/wwwroot/HonzaGG"
Upload-Folder "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\HonzaGG.Web\publish_gg" "www/wwwroot/HonzaGG/"

# HonzaKytyr  
Ensure-RemoteDir "www/wwwroot/HonzaKytyr"
Upload-Folder "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\HonzaKytyr.Web\publish_kytyr" "www/wwwroot/HonzaKytyr/"

Write-Host ""
Write-Host "=== Deploy complete! ==="
