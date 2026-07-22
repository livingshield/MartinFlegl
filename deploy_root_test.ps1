$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot\"

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

# Upload index.html to the www root as flegl.html for test
# We use flegl.html to avoid overwriting your main web index if it exists in the www root.
Upload-File "$localPath\index.html" "www/flegl.html"

# Also upload css and js folders to www root
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

Upload-Folder "$localPath\css" "www/css/"
Upload-Folder "$localPath\js" "www/js/"
Upload-Folder "$localPath\img" "www/img/"
