$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$remoteBase = "www/wwwroot/HonzaKytyr/"
$localPath = "c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\HonzaKytyr.Web\wwwroot"

function Make-FtpDir {
    param($dir)
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$dir")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $response = $req.GetResponse()
        Write-Host "Created dir: $dir"
    }
    catch {
        Write-Host "Dir exists or error: $dir - $($_.Exception.Message)"
    }
}

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

Make-FtpDir $remoteBase
Make-FtpDir ($remoteBase + "css")
Make-FtpDir ($remoteBase + "js")

$files = Get-ChildItem -Path $localPath -Recurse -File
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($localPath.Length + 1).Replace('\', '/')
    $remotePath = $remoteBase + $relativePath
    Upload-File $file.FullName $remotePath
}
Write-Host "Deploy completed!"
