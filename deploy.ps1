$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\publish"
$remoteBase = "www/MartinFlegl/"

# Create base directory
try {
    $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/www/MartinFlegl")
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $req.GetResponse().Close()
}
catch {}

function Upload-Folder {
    param($path, $remote)
    
    $items = Get-ChildItem $path
    foreach ($item in $items) {
        $target = $remote + $item.Name
        if ($item.PSIsContainer) {
            try {
                $makeDir = [System.Net.WebRequest]::Create("ftp://$ftpHost/$target")
                $makeDir.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
                $makeDir.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
                $makeDir.GetResponse().Close()
            }
            catch {}
            Upload-Folder $item.FullName ($target + "/")
        }
        else {
            Write-Host "Uploading $($item.Name) to $target..."
            $webClient = New-Object System.Net.WebClient
            $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
            $uri = New-Object System.Uri("ftp://$ftpHost/$target")
            $webClient.UploadFile($uri, $item.FullName)
        }
    }
}

Upload-Folder $localPath $remoteBase
