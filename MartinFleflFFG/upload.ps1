$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"
$ftpHost = "windows11.aspone.cz"

# 1. Upload main portal index.html to www/wwwroot/index.html
$mainIndexLocal = "C:\Users\janky\.gemini\antigravity\scratch\KikiAI\KikiAI\wwwroot\index.html"
$mainIndexUri = "ftp://$ftpHost/www/wwwroot/index.html"
$req = [System.Net.WebRequest]::Create($mainIndexUri)
$req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
$req.UseBinary = $true
$content = [System.IO.File]::ReadAllBytes($mainIndexLocal)
$req.ContentLength = $content.Length
$stream = $req.GetRequestStream()
$stream.Write($content, 0, $content.Length)
$stream.Close()
$resp = $req.GetResponse()
$resp.Close()
Write-Host "Uploaded main index.html with new menu item"

# 2. Upload project files to www/wwwroot/MartinFleflFFG/
$remoteDir = "www/wwwroot/MartinFleflFFG"
$localDir = "C:\Users\janky\.gemini\antigravity\scratch\MartinFleflFFG"
$files = Get-ChildItem -Path $localDir -File
foreach ($file in $files) {
    if ($file.Name -eq "upload.ps1") { continue }
    $uri = "ftp://$ftpHost/$remoteDir/" + $file.Name
    $req = [System.Net.WebRequest]::Create($uri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true
    
    $content = [System.IO.File]::ReadAllBytes($file.FullName)
    $req.ContentLength = $content.Length
    $stream = $req.GetRequestStream()
    $stream.Write($content, 0, $content.Length)
    $stream.Close()
    $resp = $req.GetResponse()
    $resp.Close()
    Write-Host "Uploaded $($file.Name)"
}
