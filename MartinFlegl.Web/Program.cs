using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using DotNetEnv;

// Načtení .env souboru z kořene repozitáře nebo aktuální složky
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

// Povolíme CORS pro testy
builder.Services.AddCors();

var app = builder.Build();

// Zajistíme vytvoření databázových tabulek 'flegl_leads' a 'flegl_content' po spuštění
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

            // 1. Tabulka pro zájemce / leady
            string createLeadsSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='flegl_leads' AND xtype='U')
                BEGIN
                    CREATE TABLE flegl_leads (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        FullName NVARCHAR(200) NOT NULL,
                        Email NVARCHAR(200) NOT NULL,
                        Phone NVARCHAR(50) NOT NULL,
                        Topic NVARCHAR(500),
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    )
                END";
            using (var cmd = new SqlCommand(createLeadsSql, connection))
            {
                cmd.ExecuteNonQuery();
            }

            // 2. Tabulka pro dynamický obsah (inline editor textů)
            string createContentSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='flegl_content' AND xtype='U')
                BEGIN
                    CREATE TABLE flegl_content (
                        ContentKey NVARCHAR(150) PRIMARY KEY,
                        ContentValue NVARCHAR(MAX) NOT NULL,
                        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    )
                END";
            using (var cmd = new SqlCommand(createContentSql, connection))
            {
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("[DB] Tabulky flegl_leads a flegl_content jsou připraveny.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB] CHYBA při inicializaci tabulek: " + ex.Message);
        }
    }
    else
    {
        Console.WriteLine("[DB] UPOZORNĚNÍ: DB_CONNECTION_STRING není nastaven v .env ani v prostředí.");
    }
}

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
    }
});

// --- Pomocné metody pro autentizaci adminů ---
string GetSecretKey(IConfiguration config) =>
    config["ADMIN_JWT_SECRET"] ?? Environment.GetEnvironmentVariable("ADMIN_JWT_SECRET") ?? "FleglFinanceDefaultSecretKey2026";

bool ValidateAdminCredentials(string username, string password, IConfiguration config)
{
    string rawAdmins = config["ADMIN_USERS"] ?? Environment.GetEnvironmentVariable("ADMIN_USERS") ?? "jankytyr:brzsilpot7,martinflegl:fleglmartin";
    var pairs = rawAdmins.Split(',', StringSplitOptions.RemoveEmptyEntries);
    foreach (var pair in pairs)
    {
        var parts = pair.Trim().Split(':', 2);
        if (parts.Length == 2 && parts[0].Equals(username, StringComparison.OrdinalIgnoreCase) && parts[1] == password)
        {
            return true;
        }
    }
    return false;
}

string GenerateToken(string username, IConfiguration config)
{
    string secret = GetSecretKey(config);
    long timestamp = DateTime.UtcNow.Ticks;
    string payload = $"{username}|{timestamp}";
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    string sig = Convert.ToBase64String(hash);
    return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{sig}"));
}

bool ValidateToken(string? token, IConfiguration config, out string username)
{
    username = "";
    if (string.IsNullOrWhiteSpace(token)) return false;
    try
    {
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = decoded.Split('|', 3);
        if (parts.Length != 3) return false;

        string user = parts[0];
        long timestamp = long.Parse(parts[1]);
        string sig = parts[2];

        // Platnost tokenu max 7 dní
        if (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - timestamp).TotalDays > 7) return false;

        string secret = GetSecretKey(config);
        string payload = $"{user}|{timestamp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string expectedSig = Convert.ToBase64String(hash);

        if (sig == expectedSig)
        {
            username = user;
            return true;
        }
    }
    catch { }
    return false;
}

// --- API ENDPOINTY ---

// 1. Prihlaseni Admina
app.MapPost("/api/admin/login", ([FromBody] LoginRequest req, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { message = "Uživatelské jméno a heslo jsou povinné." });
    }

    if (ValidateAdminCredentials(req.Username, req.Password, config))
    {
        string token = GenerateToken(req.Username, config);
        return Results.Ok(new { success = true, token, username = req.Username });
    }

    return Results.Json(new { message = "Neplatné přihlašovací údaje." }, statusCode: 401);
});

// 2. Nacteni dynamickeho obsahu (veřejné API)
app.MapGet("/api/content", async (IConfiguration config) =>
{
    string? connectionString = config["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    var result = new Dictionary<string, string>();

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Ok(result);
    }

    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        string sql = "SELECT ContentKey, ContentValue FROM flegl_content";
        using var cmd = new SqlCommand(sql, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(0)] = reader.GetString(1);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[API CONTENT GET ERROR] " + ex.Message);
    }

    return Results.Ok(result);
});

// 3. Ulozeni změn obsahu + Automatická záloha (chráněné API pro Admina)
app.MapPost("/api/content", async ([FromBody] SaveContentRequest req, IConfiguration config) =>
{
    if (!ValidateToken(req.Token, config, out string username))
    {
        return Results.Json(new { message = "Neautorizovaný přístup nebo vypršené sezení." }, statusCode: 401);
    }

    if (req.Items == null || req.Items.Count == 0)
    {
        return Results.Ok(new { success = true, updatedCount = 0 });
    }

    string? connectionString = config["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.StatusCode(500);
    }

    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        string mergeSql = @"
            MERGE flegl_content AS target
            USING (SELECT @ContentKey AS ContentKey, @ContentValue AS ContentValue) AS source
            ON (target.ContentKey = source.ContentKey)
            WHEN MATCHED THEN
                UPDATE SET ContentValue = source.ContentValue, UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (ContentKey, ContentValue, UpdatedAt) VALUES (source.ContentKey, source.ContentValue, GETDATE());";

        int count = 0;
        foreach (var item in req.Items)
        {
            using var cmd = new SqlCommand(mergeSql, connection);
            cmd.Parameters.AddWithValue("@ContentKey", item.Key);
            cmd.Parameters.AddWithValue("@ContentValue", item.Value ?? "");
            await cmd.ExecuteNonQueryAsync();
            count++;
        }

        Console.WriteLine($"[DB CONTENT] Admin '{username}' uložil {count} textových položek.");

        // --- AUTOMATICKÉ VYTVOŘENÍ ZÁLOHY DO SLOŽKY 'backup' ---
        try
        {
            string backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backup");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var fullSnapshot = new Dictionary<string, string>();
            using (var cmdAll = new SqlCommand("SELECT ContentKey, ContentValue FROM flegl_content", connection))
            using (var reader = await cmdAll.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    fullSnapshot[reader.GetString(0)] = reader.GetString(1);
                }
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFileName = $"content_backup_{timestamp}_{username}.json";
            string backupPath = Path.Combine(backupDir, backupFileName);

            var backupPayload = new
            {
                timestamp = DateTime.Now.ToString("o"),
                updatedBy = username,
                updatedItemCount = count,
                totalItemCount = fullSnapshot.Count,
                content = fullSnapshot
            };

            string jsonContent = JsonSerializer.Serialize(backupPayload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(backupPath, jsonContent, Encoding.UTF8);

            Console.WriteLine($"[BACKUP] Vytvořena záloha: backup/{backupFileName} ({jsonContent.Length} bytů).");
        }
        catch (Exception backupEx)
        {
            Console.WriteLine("[BACKUP ERROR] Chyba při tvorbě záložního souboru: " + backupEx.Message);
        }

        return Results.Ok(new { success = true, updatedCount = count });
    }
    catch (Exception ex)
    {
        Console.WriteLine("[API CONTENT SAVE ERROR] " + ex.Message);
        return Results.StatusCode(500);
    }
});

// 4. Odeslání formuláře s leady (DB + E-mail pro Admina + Potvrzovací E-mail pro Klienta)
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
        // 1. Uložení do databáze
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string insertSql = "INSERT INTO flegl_leads (FullName, Email, Phone, Topic) VALUES (@FullName, @Email, @Phone, @Topic)";
                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@FullName", lead.FullName);
                command.Parameters.AddWithValue("@Email", lead.Email);
                command.Parameters.AddWithValue("@Phone", lead.Phone);
                command.Parameters.AddWithValue("@Topic", lead.Topic ?? (object)DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        // 2. Odeslání e-mailů přes SMTP
        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
        {
            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);

            // A) Notifikační e-mail pro Martina Flegla (Admin)
            if (!string.IsNullOrWhiteSpace(targetEmail))
            {
                var adminMessage = new MimeMessage();
                adminMessage.From.Add(new MailboxAddress("Martin Flegl Web", smtpUser));
                adminMessage.To.Add(new MailboxAddress("Martin Flegl", targetEmail));
                adminMessage.Subject = "Nový kontakt z webu: " + lead.FullName;
                adminMessage.Body = new TextPart("plain")
                {
                    Text = $"Dobrý den,\n\nmáte nový lead z webového formuláře:\n\n" +
                           $"Jméno: {lead.FullName}\n" +
                           $"E-mail: {lead.Email}\n" +
                           $"Telefon: {lead.Phone}\n" +
                           $"Téma / Dotaz: {lead.Topic}\n\n" +
                           $"---\nVygenerováno automaticky systémem pro Martina Flegla."
                };
                await client.SendAsync(adminMessage);
            }

            // B) Potvrzovací e-mail pro klienta
            if (!string.IsNullOrWhiteSpace(lead.Email))
            {
                var clientMessage = new MimeMessage();
                clientMessage.From.Add(new MailboxAddress("Martin Flegl - Finanční specialista", smtpUser));
                clientMessage.To.Add(new MailboxAddress(lead.FullName, lead.Email));
                clientMessage.Subject = "Potvrzení přijetí poptávky | Martin Flegl";
                clientMessage.Body = new TextPart("plain")
                {
                    Text = $"Dobrý den, {lead.FullName},\n\n" +
                           $"děkuji Vám za zájem o mé služby a za odeslání poptávkového formuláře.\n\n" +
                           $"Vaši zprávu týkající se oblasti \"{lead.Topic}\" jsem v pořádku přijal. Co nejdříve Vás budu kontaktovat k domluvení termínu nezávazné konzultace.\n\n" +
                           $"Shrnutí zadaných údajů:\n" +
                           $"• Jméno a příjmení: {lead.FullName}\n" +
                           $"• E-mail: {lead.Email}\n" +
                           $"• Telefon: {lead.Phone}\n" +
                           $"• Téma konzultace: {lead.Topic}\n\n" +
                           $"S přáním pěkného dne,\n\n" +
                           $"Martin Flegl\n" +
                           $"Nezávislý finanční specialista | Partner INSIA\n" +
                           $"Telefon: +420 736 453 532 | E-mail: martin.flegl@insia.com\n" +
                           $"Kanceláře: Trutnov (Pražská 523) | Dvůr Králové nad Labem (Husova 129)"
                };
                await client.SendAsync(clientMessage);
            }

            await client.DisconnectAsync(true);
        }

        return Results.Ok(new { message = "Formulář byl úspěšně odeslán." });
    }
    catch (Exception ex)
    {
        Console.WriteLine("[API LEADS ERROR] " + ex.Message);
        return Results.StatusCode(500);
    }
});

app.Run();

// DTO Modely
record LeadModel(string FullName, string Email, string Phone, string Topic);
record LoginRequest(string Username, string Password);
record SaveContentRequest(string Token, Dictionary<string, string> Items);
