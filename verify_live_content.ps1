$url = "http://ekobio.org/MartinFlegl/"
$resp = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
$content = $resp.Content

Write-Host "Searching for 'Flégl' (accented):"
$content.Contains("Flégl")

Write-Host "`nSearching for 'Flegl' (non-accented):"
$content.Contains("Flegl")

Write-Host "`nSearching for 'Spolupráce':"
$content.Contains("Spolupráce")

Write-Host "`nSearching for 'Husova 129':"
$content.Contains("Husova 129")
