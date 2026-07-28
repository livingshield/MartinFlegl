# Multi-target deployment script for MartinFlegl project
# Deploys automatically to BOTH Test (ekobio.org/MartinFlegl) and Production (martinflegl.cz)

$localPath = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\publish"
$localWwwroot = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot"
$envFile = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\.env"

$targets = @(
    @{
        Name = "TESTING (http://ekobio.org/MartinFlegl/)";
        Host = "windows11.aspone.cz";
        User = "EkoBio.org_lordkikin";
        Pass = "Brzsilpot7!";
        RemoteBase = "www/MartinFlegl/";
        Url = "http://ekobio.org/MartinFlegl/"
    },
    @{
        Name = "PRODUCTION (http://martinflegl.cz/)";
        Host = "windows12.aspone.cz";
        User = "martinflegl.cz";
        Pass = "Trutnov-2026";
        RemoteBase = "www/";
        Url = "http://martinflegl.cz/"
    }
)

function Delete-FtpFile($ftpHost, $user, $pass, $path) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$path")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $response = $req.GetResponse()
        $response.Close()
        Write-Host "  Deleted FTP file: $path on $ftpHost"
    } catch {}
}

function Upload-File($ftpHost, $user, $pass, $localFile, $remoteFile) {
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

function Ensure-RemoteDir($ftpHost, $user, $pass, $remotePath) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$remotePath")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.GetResponse().Close()
    }
    catch {}
}

function Upload-Folder($ftpHost, $user, $pass, $localPath, $remotePath) {
    $items = Get-ChildItem $localPath
    foreach ($item in $items) {
        $target = $remotePath + $item.Name
        if ($item.PSIsContainer) {
            Ensure-RemoteDir $ftpHost $user $pass $target
            Upload-Folder $ftpHost $user $pass $item.FullName ($target + "/")
        }
        else {
            Upload-File $ftpHost $user $pass $item.FullName $target
        }
    }
}

# Create temporary app_offline.htm
$tempOffline = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\app_offline.htm"
"<html><body><h2>Probiha aktualizace webu...</h2></body></html>" | Out-File -FilePath $tempOffline -Encoding utf8

# Clean local compressed artifacts
Get-ChildItem -Path $localPath -Recurse -Include *.gz, *.br -ErrorAction SilentlyContinue | Remove-Item -Force

foreach ($target in $targets) {
    $h = $target.Host
    $u = $target.User
    $p = $target.Pass
    $rb = $target.RemoteBase

    Write-Host ""
    Write-Host "========================================="
    Write-Host "=== DEPLOYING TO $($target.Name) ==="
    Write-Host "========================================="

    Write-Host "=== 1. Putting IIS App Offline ==="
    Upload-File $h $u $p $tempOffline ($rb + "app_offline.htm")
    Start-Sleep -Seconds 2

    Write-Host "=== 2. Cleaning compressed .gz and .br artifacts ==="
    Delete-FtpFile $h $u $p ($rb + "index.html.gz")
    Delete-FtpFile $h $u $p ($rb + "index.html.br")
    Delete-FtpFile $h $u $p ($rb + "css/main.css.gz")
    Delete-FtpFile $h $u $p ($rb + "css/main.css.br")
    Delete-FtpFile $h $u $p ($rb + "js/admin.js.gz")
    Delete-FtpFile $h $u $p ($rb + "js/admin.js.br")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/index.html.gz")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/index.html.br")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/css/main.css.gz")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/css/main.css.br")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/js/admin.js.gz")
    Delete-FtpFile $h $u $p ($rb + "wwwroot/js/admin.js.br")

    Write-Host "=== 3. Uploading publish output ==="
    Ensure-RemoteDir $h $u $p $rb
    Ensure-RemoteDir $h $u $p ($rb + "css")
    Ensure-RemoteDir $h $u $p ($rb + "js")
    Ensure-RemoteDir $h $u $p ($rb + "img")
    Ensure-RemoteDir $h $u $p ($rb + "wwwroot")
    Ensure-RemoteDir $h $u $p ($rb + "wwwroot/css")
    Ensure-RemoteDir $h $u $p ($rb + "wwwroot/js")

    Upload-Folder $h $u $p $localPath $rb

    Write-Host "=== 4. Uploading static wwwroot files directly to root ==="
    Upload-Folder $h $u $p $localWwwroot $rb

    Write-Host "=== 5. Uploading .env configuration ==="
    if (Test-Path $envFile) {
        Upload-File $h $u $p $envFile ($rb + ".env")
    }

    Write-Host "=== 6. Bringing IIS App Online (Deleting app_offline.htm) ==="
    Delete-FtpFile $h $u $p ($rb + "app_offline.htm")

    Write-Host "=== Deployed successfully to $($target.Name) -> $($target.Url) ==="
}

Remove-Item $tempOffline -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================="
Write-Host "=== ALL DEPLOYMENTS COMPLETED! ==="
Write-Host "Test URL: http://ekobio.org/MartinFlegl/"
Write-Host "Production URL: http://martinflegl.cz/"
Write-Host "========================================="
