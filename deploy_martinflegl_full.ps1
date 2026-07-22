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

Write-Host "=== Cleaning compressed .gz and .br artifacts from www/MartinFlegl ==="
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

Write-Host "=== Publishing MartinFlegl.Web to ACTUAL live location: ftp://$ftpHost/$remoteBase ==="

Ensure-RemoteDir "www/MartinFlegl"
Ensure-RemoteDir "www/MartinFlegl/css"
Ensure-RemoteDir "www/MartinFlegl/js"
Ensure-RemoteDir "www/MartinFlegl/img"
Ensure-RemoteDir "www/MartinFlegl/wwwroot"
Ensure-RemoteDir "www/MartinFlegl/wwwroot/css"
Ensure-RemoteDir "www/MartinFlegl/wwwroot/js"

# 1. Upload full .NET publish directory into www/MartinFlegl/
Upload-Folder $localPath $remoteBase

# 2. ALSO upload static wwwroot directly into www/MartinFlegl/ root for static IIS serving
Write-Host "Synchronizing static files directly to root..."
Upload-Folder $localWwwroot $remoteBase

# 3. Upload .env configuration into www/MartinFlegl/
Write-Host "Uploading .env configuration..."
Upload-File "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\.env" ($remoteBase + ".env")

Write-Host ""
Write-Host "=== Deploy complete! ==="
Write-Host "URL: http://ekobio.org/MartinFlegl/"
