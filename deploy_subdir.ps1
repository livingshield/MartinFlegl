$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\MartinFlegl.Web\wwwroot\"
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

function Create-FtpDir {
    param($dir)
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$dir")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $response = $req.GetResponse()
        $response.Close()
        Write-Host "Created Dir: $dir"
    }
    catch {
        # Already exists or no permission, we will just proceed
    }
}

# 1. Create main subdirectory
Create-FtpDir "www/wwwroot/MartinFlegl"

# 2. Create subdirs
Create-FtpDir ($remoteBase + "css")
Create-FtpDir ($remoteBase + "js")
Create-FtpDir ($remoteBase + "img")

# 3. Upload CSS
$cssFiles = Get-ChildItem ($localPath + "css")
foreach ($f in $cssFiles) { Upload-File $f.FullName ($remoteBase + "css/" + $f.Name) }

# 4. Upload JS
$jsFiles = Get-ChildItem ($localPath + "js")
foreach ($f in $jsFiles) { Upload-File $f.FullName ($remoteBase + "js/" + $f.Name) }

# 5. Upload IMG
$imgFiles = Get-ChildItem ($localPath + "img")
foreach ($f in $imgFiles) { Upload-File $f.FullName ($remoteBase + "img/" + $f.Name) }

# 6. Upload root level files
Upload-File ($localPath + "index.html") ($remoteBase + "index.html")
Upload-File "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\qr_kód.png" ($remoteBase + "qr_kód.png")
Upload-File "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\foto.png" ($remoteBase + "foto.png")

Write-Host "Deployment to subdirectory complete."
