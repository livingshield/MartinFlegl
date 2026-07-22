using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env from root if exists
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Database Setup Template
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            // CHANGE TABLE_NAME to your project name prefix (e.g. project_leads)
            string createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='greenberg_leads' AND xtype='U')
                BEGIN
                    CREATE TABLE greenberg_leads (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        FullName NVARCHAR(200) NOT NULL,
                        Email NVARCHAR(200) NOT NULL,
                        Phone NVARCHAR(50),
                        Topic NVARCHAR(500),
                        CreatedAt DATETIME DEFAULT GETDATE()
                    )
                END";
            using var command = new SqlCommand(createTableSql, connection);
            command.ExecuteNonQuery();
            Console.WriteLine("[DB] Database table is ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB] Error: " + ex.Message);
        }
    }
}

app.MapPost("/api/leads", async (LeadModel lead, IConfiguration config) =>
{
    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
    var smtpPort = 587;
    var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
    var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS");
    var targetEmail = Environment.GetEnvironmentVariable("TARGET_EMAIL");
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

    try
    {
        // 1. Save to Database
        if (!string.IsNullOrEmpty(connectionString))
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // CHANGE TABLE_NAME here too
                string insertSql = "INSERT INTO greenberg_leads (FullName, Email, Phone, Topic) VALUES (@FullName, @Email, @Phone, @Topic)";
                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@FullName", lead.FullName);
                command.Parameters.AddWithValue("@Email", lead.Email);
                command.Parameters.AddWithValue("@Phone", (object?)lead.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Topic", (object?)lead.Topic ?? DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        // 2. Send Email
        if (smtpUser != null && smtpPass != null && targetEmail != null)
        {
            var message = new MimeMessage();
            // SET YOUR PROJECT NAME HERE
            message.From.Add(new MailboxAddress("Greenberg Web", smtpUser));
            message.To.Add(new MailboxAddress("Admin", targetEmail));
            message.Subject = "Nový kontakt z webu: " + lead.FullName;

            message.Body = new TextPart("plain")
            {
                Text = $"Name: {lead.FullName}\n" +
                       $"Email: {lead.Email}\n" +
                       $"Phone: {lead.Phone}\n" +
                       $"Topic: {lead.Topic}\n\n" +
                       $"---\nSent by Automation System."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        return Results.Ok(new { message = "Success" });
    }
    catch (Exception ex)
    {
        return Results.Problem("Error: " + ex.Message);
    }
});

app.Run();

public record LeadModel(string FullName, string Email, string? Phone, string? Topic);
