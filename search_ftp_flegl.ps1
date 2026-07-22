$ftpHost = "windows11.aspone.cz"
$user = "EkoBio.org_lordkikin"
$pass = "Brzsilpot7!"

function Search-Ftp($dir) {
    try {
        $req = [System.Net.WebRequest]::Create("ftp://$ftpHost/$dir")
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
        $res = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($res.GetResponseStream())
        $lines = $reader.ReadToEnd() -split "`n"
        $reader.Close()
        $res.Close()

        foreach ($line in $lines) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            $parts = $line.Trim() -split "\s+"
            $name = $parts[-1]
            if ($name -eq "." -or $name -eq "..") { continue }

            $isDir = $line.StartsWith("d")
            $fullPath = "$dir$name"

            if ($name -like "*flegl*" -or $name -like "*Flegl*") {
                Write-Host "FOUND MATCH: $fullPath (isDir=$isDir)"
            }

            if ($isDir -and $dir.Split('/').Count -lt 5) {
                Search-Ftp "$fullPath/"
            }
        }
    } catch {}
}

Write-Host "Searching FTP for Flegl..."
Search-Ftp "www/"
