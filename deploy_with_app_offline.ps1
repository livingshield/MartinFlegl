$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localPath = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\publish"
$localWwwroot = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot"
$remoteBase = "www/MartinFlegl/"

function Delete-FtpFile($path) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$path")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $response = $req.GetResponse()
        $response.Close()
        Write-Host "Deleted FTP file: $path"
    } catch {}
}

function Upload-File($localFile, $remoteFile) {
    Write-Host "  $($localFile | Split-Path -Leaf) -> $remoteFile"
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $uri = New-Object System.Uri("ftp://$ftpHost/$remoteFile")
        $webClient.UploadFile($uri, $localFile)
    }
    catch {
        Write-Host "    ERROR ($remoteFile): $($_.Exception.Message)"
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

# Create temporary app_offline.htm
$tempOffline = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\app_offline.htm"
"<html><body><h2>Probiha aktualizace webu...</h2></body></html>" | Out-File -FilePath $tempOffline -Encoding utf8

Write-Host "=== 1. Putting IIS App Offline ==="
Upload-File $tempOffline ($remoteBase + "app_offline.htm")
Start-Sleep -Seconds 3

Write-Host "=== 2. Cleaning compressed .gz and .br artifacts ==="
Get-ChildItem -Path $localPath -Recurse -Include *.gz, *.br -ErrorAction SilentlyContinue | Remove-Item -Force
Delete-FtpFile "www/MartinFlegl/index.html.gz"
Delete-FtpFile "www/MartinFlegl/index.html.br"
Delete-FtpFile "www/MartinFlegl/css/main.css.gz"
Delete-FtpFile "www/MartinFlegl/css/main.css.br"
Delete-FtpFile "www/MartinFlegl/js/admin.js.gz"
Delete-FtpFile "www/MartinFlegl/js/admin.js.br"
Delete-FtpFile "www/MartinFlegl/wwwroot/index.html.gz"
Delete-FtpFile "www/MartinFlegl/wwwroot/index.html.br"
Delete-FtpFile "www/MartinFlegl/wwwroot/css/main.css.gz"
Delete-FtpFile "www/MartinFlegl/wwwroot/css/main.css.br"
Delete-FtpFile "www/MartinFlegl/wwwroot/js/admin.js.gz"
Delete-FtpFile "www/MartinFlegl/wwwroot/js/admin.js.br"

Write-Host "=== 3. Uploading publish output to www/MartinFlegl/ ==="
Ensure-RemoteDir "www/MartinFlegl"
Ensure-RemoteDir "www/MartinFlegl/css"
Ensure-RemoteDir "www/MartinFlegl/js"
Ensure-RemoteDir "www/MartinFlegl/img"
Ensure-RemoteDir "www/MartinFlegl/wwwroot"
Ensure-RemoteDir "www/MartinFlegl/wwwroot/css"
Ensure-RemoteDir "www/MartinFlegl/wwwroot/js"

Upload-Folder $localPath $remoteBase

Write-Host "=== 4. Uploading static wwwroot files directly to root ==="
Upload-Folder $localWwwroot $remoteBase

Write-Host "=== 5. Uploading .env configuration ==="
Upload-File "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\.env" ($remoteBase + ".env")

Write-Host "=== 6. Bringing IIS App Online (Deleting app_offline.htm) ==="
Delete-FtpFile "www/MartinFlegl/app_offline.htm"
Remove-Item $tempOffline -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== Deploy complete! ==="
Write-Host "URL: http://ekobio.org/MartinFlegl/"
