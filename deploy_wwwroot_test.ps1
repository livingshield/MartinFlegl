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
        Write-Host "Error: $($_.Exception.Message)"
    }
}

Upload-File "$localPath\index.html" "www/wwwroot/flegl.html"
