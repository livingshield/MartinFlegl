$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot\"
$remoteBase = "www/wwwroot/"

function Upload-File($localFile, $remoteFile) {
    Write-Host "Uploading $localFile to $remoteFile ..."
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$remoteFile")
        $webClient.UploadFile($uri, $localFile)
        Write-Host "Successfully uploaded $remoteFile"
    }
    catch {
        Write-Host "Error uploading $remoteFile : $($_.Exception.Message)"
    }
}

function Upload-Folder {
    param($path, $remote)
    
    $items = Get-ChildItem $path
    foreach ($item in $items) {
        $target = $remote + $item.Name
        if ($item.PSIsContainer) {
            try {
                $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$target")
                $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
                $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
                $req.GetResponse().Close()
            }
            catch {}
            Upload-Folder $item.FullName ($target + "/")
        }
        else {
            Upload-File $item.FullName $target
        }
    }
}

# 1. Upload main HTML as index.html (overwriting the old Kiki one if desired, or use flegl.html)
# Let's use index.html to make it the main page of the domain
Upload-File "$localPath\index.html" ($remoteBase + "index.html")

# 2. Upload asset folders
Upload-Folder "$localPath\css" ($remoteBase + "css/")
Upload-Folder "$localPath\js" ($remoteBase + "js/")
Upload-Folder "$localPath\img" ($remoteBase + "img/")

# 3. Upload images from root to remote root (including the photo and qr code)
Upload-File "$localPath\../qr_kód.png" ($remoteBase + "qr_kód.png")
Upload-File "$localPath\../foto.png" ($remoteBase + "foto.png")
