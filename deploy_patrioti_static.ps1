$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$localWwwroot = "c:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\PatriotiTrutnov.Web\wwwroot"
$remoteBase = "www/wwwroot/patriotitrutnov/"

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

Write-Host "=== Uploading static files directly to patriotitrutnov root ==="

# Upload index.html directly to the project root (not inside wwwroot subfolder)
Upload-File "$localWwwroot\index.html" ($remoteBase + "index.html")

# CSS
Ensure-RemoteDir ($remoteBase + "css")
Upload-File "$localWwwroot\css\main.css" ($remoteBase + "css/main.css")

# JS
Ensure-RemoteDir ($remoteBase + "js")
Upload-File "$localWwwroot\js\animations.js" ($remoteBase + "js/animations.js")

# IMG
Ensure-RemoteDir ($remoteBase + "img")

Write-Host ""
Write-Host "=== Static files deployed! ==="
Write-Host "URL: https://www.ekobio.org/patriotitrutnov/"
