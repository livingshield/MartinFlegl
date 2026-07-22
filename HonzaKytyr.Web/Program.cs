using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using DotNetEnv;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
}
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    string? connectionString = config["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            string createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='honzakytyr_leads' AND xtype='U')
                BEGIN
                    CREATE TABLE honzakytyr_leads (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        FullName NVARCHAR(200) NOT NULL,
                        Email NVARCHAR(200) NOT NULL,
                        Phone NVARCHAR(50) NOT NULL,
                        Topic NVARCHAR(500),
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    )
                END";
            using var command = new SqlCommand(createTableSql, connection);
            command.ExecuteNonQuery();
            Console.WriteLine("[DB] Tabulka honzakytyr_leads je připravena.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB] CHYBA při vytváření tabulky: " + ex.Message);
        }
    }
}

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/leads", async ([FromBody] LeadModel lead, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(lead.FullName) || string.IsNullOrWhiteSpace(lead.Email) || string.IsNullOrWhiteSpace(lead.Phone))
    {
        return Results.BadRequest(new { message = "Jméno, e-mail a telefon jsou povinné." });
    }

    string? connectionString = config["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    string? smtpHost = config["SMTP_HOST"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
    int smtpPort = int.TryParse(config["SMTP_PORT"] ?? Environment.GetEnvironmentVariable("SMTP_PORT"), out int p) ? p : 587;
    string? smtpUser = config["SMTP_USER"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
    string? smtpPass = config["SMTP_PASS"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
    string? targetEmail = config["TARGET_EMAIL"] ?? Environment.GetEnvironmentVariable("TARGET_EMAIL");

    try
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string insertSql = "INSERT INTO honzakytyr_leads (FullName, Email, Phone, Topic) VALUES (@FullName, @Email, @Phone, @Topic)";
                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@FullName", lead.FullName);
                command.Parameters.AddWithValue("@Email", lead.Email);
                command.Parameters.AddWithValue("@Phone", lead.Phone);
                command.Parameters.AddWithValue("@Topic", lead.Topic ?? (object)DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass) && !string.IsNullOrWhiteSpace(targetEmail))
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Jan Kytýr Web", smtpUser));
            message.To.Add(new MailboxAddress("Admin", targetEmail));
            message.Subject = "Nový kontakt z webu: " + lead.FullName;

            message.Body = new TextPart("plain")
            {
                Text = $"Dobrý den,\n\nmáte nový lead z webového formuláře:\n\n" +
                       $"Jméno: {lead.FullName}\n" +
                       $"E-mail: {lead.Email}\n" +
                       $"Telefon: {lead.Phone}\n" +
                       $"Téma / Dotaz: {lead.Topic}\n\n" +
                       $"---\nVygenerováno automaticky systémem pro Jana Kytýře."
            };

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s,c,h,e) => true; 
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        return Results.Ok(new { message = "Formulář byl úspěšně odeslán." });
    }
    catch (Exception ex)
    {
        Console.WriteLine("[API ERROR] " + ex.Message);
        return Results.StatusCode(500);
    }
});

app.Run();

record LeadModel(string FullName, string Email, string Phone, string Topic);
