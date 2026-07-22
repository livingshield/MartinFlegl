$connectionString = "Server=sql8.aspone.cz;Database=db4937;User Id=db4937;Password=lordkikin;Encrypt=False"
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "Database connection successful!"
    $connection.Close()
} catch {
    Write-Error "Database connection failed: $($_.Exception.Message)"
}
