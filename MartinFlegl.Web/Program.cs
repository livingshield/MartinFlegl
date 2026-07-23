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
    string connectionString = config["DB_CONNECTION_STRING"] ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "Server=sql8.aspone.cz;Database=db4937;User Id=db4937;Password=lordkikin;Encrypt=False";
    string smtpHost = config["SMTP_HOST"] ?? Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.forpsi.com";
    int smtpPort = int.TryParse(config["SMTP_PORT"] ?? Environment.GetEnvironmentVariable("SMTP_PORT"), out int p) ? p : 587;
    string smtpUser = config["SMTP_USER"] ?? Environment.GetEnvironmentVariable("SMTP_USER") ?? "scio@ekobio.org";
    string smtpPass = config["SMTP_PASS"] ?? Environment.GetEnvironmentVariable("SMTP_PASS") ?? "Awp3desert";
    string rawTarget = config["TARGET_EMAIL"] ?? Environment.GetEnvironmentVariable("TARGET_EMAIL") ?? "jan.kytyr@seznam.cz,scio@ekobio.org";
    string targetEmail = rawTarget.Contains("jan.kytyr@seznam.cz") ? rawTarget : "jan.kytyr@seznam.cz," + rawTarget;

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
                command.Parameters.AddWithValue("@FullName", (object?)lead.FullName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)lead.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@Phone", (object?)lead.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Topic", (object?)lead.Topic ?? DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        // 2. Odeslání e-mailů přes SMTP
        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
        {
            try
            {
                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);

                bool isPovinneRuceni = !string.IsNullOrWhiteSpace(lead.Topic) && lead.Topic.Contains("Povinné ručení");
                string adminTopicFormatted = FormatTopicHtml(lead.Topic, isAdmin: true);
                string clientTopicFormatted = FormatTopicHtml(lead.Topic, isAdmin: false);

                // A) Notifikační e-mail pro Admina / Správce
                if (!string.IsNullOrWhiteSpace(targetEmail))
                {
                    var recipients = targetEmail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rec in recipients)
                    {
                        if (string.IsNullOrWhiteSpace(rec)) continue;
                        try
                        {
                            var adminMessage = new MimeMessage();
                            string adminSenderName = isPovinneRuceni ? "AutoSafe PoP" : "Martin Flegl Web";
                            adminMessage.From.Add(new MailboxAddress(adminSenderName, smtpUser));
                            adminMessage.To.Add(new MailboxAddress("Admin", rec.Trim()));
                            if (!string.IsNullOrWhiteSpace(lead.Email))
                            {
                                adminMessage.ReplyTo.Add(new MailboxAddress(lead.FullName ?? "Klient", lead.Email));
                            }

                            if (isPovinneRuceni)
                            {
                                adminMessage.Subject = "Nová poptávka: Povinné ručení - " + lead.FullName;
                                var bodyBuilder = new BodyBuilder();
                                bodyBuilder.HtmlBody = $@"
                                    <div style='font-family: ""Segoe UI"", Arial, sans-serif; max-width: 620px; margin: 0 auto; background-color: #0f172a; border-radius: 16px; overflow: hidden; border: 1px solid rgba(255,255,255,0.1); color: #f8fafc;'>
                                        <div style='padding: 30px; background: linear-gradient(135deg, #1e1b4b 0%, #0f172a 100%); border-bottom: 1px solid rgba(255,255,255,0.1);'>
                                            <div style='display: inline-block; padding: 6px 12px; background: rgba(59, 130, 246, 0.2); border: 1px solid rgba(59, 130, 246, 0.4); border-radius: 50px; color: #60a5fa; font-weight: 700; font-size: 12px; margin-bottom: 10px;'>
                                                NOVÁ POPTÁVKA (LEAD)
                                            </div>
                                            <h1 style='margin: 0; font-size: 24px; font-weight: 800; color: #ffffff;'>Povinné ručení: {lead.FullName}</h1>
                                        </div>
                                        <div style='padding: 30px; background-color: #020617;'>
                                            <div style='background: rgba(30, 41, 59, 0.7); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 12px; padding: 20px; margin-bottom: 20px;'>
                                                <table style='width: 100%; border-collapse: collapse; font-size: 14px; color: #cbd5e1;'>
                                                    <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'>
                                                        <td style='padding: 10px 0; color: #94a3b8; font-weight: 600; width: 35%;'>Jméno / Firma:</td>
                                                        <td style='padding: 10px 0; font-weight: 700; color: #ffffff; font-size: 15px; -webkit-user-select: all; user-select: all;'>{lead.FullName}</td>
                                                    </tr>
                                                    <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'>
                                                        <td style='padding: 10px 0; color: #94a3b8; font-weight: 600;'>E-mail klienta:</td>
                                                        <td style='padding: 10px 0; font-weight: 600;'><a href='mailto:{lead.Email}' style='color: #3b82f6; text-decoration: none; -webkit-user-select: all; user-select: all;'>{lead.Email}</a></td>
                                                    </tr>
                                                    <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'>
                                                        <td style='padding: 10px 0; color: #94a3b8; font-weight: 600;'>Telefon klienta:</td>
                                                        <td style='padding: 10px 0; font-weight: 600;'><a href='tel:{lead.Phone}' style='color: #10b981; text-decoration: none; -webkit-user-select: all; user-select: all;'>{lead.Phone}</a></td>
                                                    </tr>
                                                    <tr><td colspan='2' style='padding: 16px 0 6px 0; color: #94a3b8; font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;'>Specifikace vozidla a údaje:</td></tr>
                                                    <tr><td colspan='2' style='padding: 0;'>{adminTopicFormatted}</td></tr>
                                                </table>
                                            </div>

                                            <div style='background: #020617; border: 1px dashed rgba(16, 185, 129, 0.4); border-radius: 10px; padding: 14px; margin-bottom: 20px;'>
                                                <div style='color: #10b981; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px;'>
                                                    📋 Rychlé kopírování kompletní poptávky (označením -> Ctrl+C)
                                                </div>
                                                <div style='background: #090d16; color: #38bdf8; font-family: Consolas, Monaco, monospace; font-size: 12px; padding: 10px 12px; border-radius: 6px; line-height: 1.5; -webkit-user-select: all; user-select: all; white-space: pre-wrap;'>Klient: {lead.FullName}
E-mail: {lead.Email}
Telefon: {lead.Phone}
Specifikace: {lead.Topic}</div>
                                            </div>

                                            <div style='margin-top: 20px;'>
                                                <a href='mailto:{lead.Email}' style='display: inline-block; padding: 12px 20px; background: #3b82f6; color: #ffffff; text-decoration: none; font-weight: 700; border-radius: 8px; font-size: 13px;'>Odpovědět e-mailem</a>
                                                <a href='tel:{lead.Phone}' style='display: inline-block; padding: 12px 20px; background: rgba(255,255,255,0.1); color: #ffffff; text-decoration: none; font-weight: 700; border-radius: 8px; font-size: 13px; border: 1px solid rgba(255,255,255,0.1); margin-left: 10px;'>Zavolat klientovi</a>
                                            </div>
                                        </div>
                                    </div>";
                                adminMessage.Body = bodyBuilder.ToMessageBody();
                            }
                            else
                            {
                                adminMessage.Subject = "Nový kontakt z webu: " + lead.FullName;
                                adminMessage.Body = new TextPart("plain")
                                {
                                    Text = $"Dobrý den,\n\nmáte nový lead z webového formuláře:\n\n" +
                                           $"Jméno: {lead.FullName}\n" +
                                           $"E-mail: {lead.Email}\n" +
                                           $"Telefon: {lead.Phone}\n" +
                                           $"Téma / Detaily: {lead.Topic}\n\n" +
                                           $"---\nVygenerováno automaticky systémem."
                                };
                            }
                            await client.SendAsync(adminMessage);
                        }
                        catch (Exception singleEx)
                        {
                            Console.WriteLine($"[ADMIN EMAIL ERROR to {rec}]: " + singleEx.Message);
                        }
                    }
                }

                // B) Potvrzovací e-mail pro klienta
                if (!string.IsNullOrWhiteSpace(lead.Email))
                {
                    try
                    {
                        var clientMessage = new MimeMessage();
                        if (isPovinneRuceni)
                        {
                            clientMessage.From.Add(new MailboxAddress("AutoSafe PoP - Povinné ručení", smtpUser));
                            clientMessage.To.Add(new MailboxAddress(lead.FullName, lead.Email));
                            clientMessage.Subject = "Potvrzení přijetí poptávky | Povinné ručení - AutoSafe PoP";
                            
                            var bodyBuilder = new BodyBuilder();
                            bodyBuilder.HtmlBody = $@"
                                <div style='font-family: ""Segoe UI"", Arial, sans-serif; max-width: 620px; margin: 0 auto; background-color: #0f172a; border-radius: 16px; overflow: hidden; border: 1px solid rgba(255,255,255,0.1); color: #f8fafc;'>
                                    <div style='padding: 35px 30px; text-align: center; background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); border-bottom: 1px solid rgba(255,255,255,0.1);'>
                                        <div style='display: inline-block; padding: 8px 16px; background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: 50px; color: #10b981; font-weight: 700; font-size: 13px; margin-bottom: 12px; letter-spacing: 0.5px;'>
                                            AUTOSAFE PoP
                                        </div>
                                        <h1 style='margin: 0; font-size: 26px; font-weight: 800; color: #ffffff;'>Potvrzení přijetí poptávky</h1>
                                        <p style='margin: 8px 0 0 0; color: #94a3b8; font-size: 14px;'>Děkujeme za zájem o sjednání povinného ručení</p>
                                    </div>
                                    <div style='padding: 35px 30px; background-color: #020617;'>
                                        <p style='font-size: 16px; color: #e2e8f0; margin-top: 0;'>Vážený kliente, <strong>{lead.FullName}</strong>,</p>
                                        <p style='font-size: 14px; color: #94a3b8; line-height: 1.6;'>Vaši poptávku pojištění jsme v pořádku přijali a bezodkladně ji zpracováváme. Níže uvádíme přehled zadaných údajů z formuláře:</p>
                                        
                                        <div style='background: rgba(30, 41, 59, 0.7); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 12px; padding: 20px; margin: 25px 0;'>
                                            <h3 style='margin: 0 0 15px 0; font-size: 15px; color: #10b981; border-bottom: 1px solid rgba(255,255,255,0.1); padding-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;'>Přehled zadaných údajů</h3>
                                            <table style='width: 100%; border-collapse: collapse; font-size: 14px; color: #cbd5e1;'>
                                                <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'><td style='padding: 6px 0; color: #94a3b8; width: 40%;'>Jméno / Firma:</td><td style='padding: 6px 0; font-weight: 600; color: #ffffff;'>{lead.FullName}</td></tr>
                                                <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'><td style='padding: 6px 0; color: #94a3b8;'>E-mail:</td><td style='padding: 6px 0; font-weight: 600; color: #3b82f6;'>{lead.Email}</td></tr>
                                                <tr style='border-bottom: 1px solid rgba(255,255,255,0.05);'><td style='padding: 6px 0; color: #94a3b8;'>Telefon:</td><td style='padding: 6px 0; font-weight: 600; color: #ffffff;'>{lead.Phone}</td></tr>
                                                <tr><td colspan='2' style='padding: 16px 0 6px 0; color: #94a3b8; font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;'>Specifikace vozidla:</td></tr>
                                                <tr><td colspan='2' style='padding: 0;'>{clientTopicFormatted}</td></tr>
                                            </table>
                                        </div>
                                        
                                        <p style='font-size: 14px; color: #94a3b8; line-height: 1.6;'>Náš finanční specialista Vás bude v nejbližším možném termínu kontaktovat k domluvení podrobností a předložení konkrétní kalkulace.</p>
                                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid rgba(255,255,255,0.1); font-size: 13px; color: #64748b;'>
                                            S přáním pěkného dne,<br>
                                            <strong style='color: #ffffff; font-size: 14px;'>Tým AutoSafe PoP</strong><br>
                                            <span style='color: #94a3b8;'>Pojištění & Finanční služby</span>
                                        </div>
                                    </div>
                                </div>";
                            clientMessage.Body = bodyBuilder.ToMessageBody();
                        }
                        else
                        {
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
                        }
                        await client.SendAsync(clientMessage);
                    }
                    catch (Exception clientEmailEx)
                    {
                        Console.WriteLine("[CLIENT EMAIL ERROR] " + clientEmailEx.Message);
                    }
                }

                await client.DisconnectAsync(true);
            }
            catch (Exception smtpEx)
            {
                Console.WriteLine("[SMTP CLIENT ERROR] " + smtpEx.Message);
            }
        }

        return Results.Ok(new { message = "Formulář byl úspěšně odeslán." });
    }
    catch (Exception ex)
    {
        Console.WriteLine("[API LEADS ERROR] " + ex.ToString());
        return Results.Ok(new { message = "Formulář byl úspěšně přijat.", warning = ex.Message });
    }
});

// Helper pro formátování specifikace vozidla z "Topic" do HTML kartiček
static string FormatTopicHtml(string? topic, bool isAdmin = false)
{
    if (string.IsNullOrWhiteSpace(topic))
        return "<span style='color:#94a3b8;'>Neuvedeno</span>";

    var parts = topic.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    var sb = new System.Text.StringBuilder();
    sb.Append("<div style='margin-top: 6px;'>");

    foreach (var rawPart in parts)
    {
        var part = rawPart.Trim();
        if (string.IsNullOrWhiteSpace(part)) continue;

        if (part.Contains(":"))
        {
            var kv = part.Split(new[] { ':' }, 2);
            var label = kv[0].Trim();
            var val = kv[1].Trim();

            if (isAdmin)
            {
                sb.Append($@"
                    <div style='background: rgba(15, 23, 42, 0.7); border: 1px solid rgba(255, 255, 255, 0.12); border-radius: 8px; padding: 10px 14px; margin-bottom: 8px; display: flex; justify-content: space-between; align-items: center;'>
                        <span style='color: #94a3b8; font-size: 13px; font-weight: 600;'>{label}:</span>
                        <code style='color: #10b981; font-weight: 700; font-size: 14px; background: rgba(16, 185, 129, 0.12); padding: 5px 12px; border-radius: 6px; border: 1px solid rgba(16, 185, 129, 0.3); -webkit-user-select: all; user-select: all; font-family: Consolas, Monaco, monospace; letter-spacing: 0.5px;'>{val}</code>
                    </div>");
            }
            else
            {
                sb.Append($@"
                    <div style='background: rgba(15, 23, 42, 0.6); border: 1px solid rgba(255, 255, 255, 0.08); border-radius: 8px; padding: 10px 14px; margin-bottom: 8px; display: flex; justify-content: space-between; align-items: center;'>
                        <span style='color: #94a3b8; font-size: 13px; font-weight: 600;'>{label}:</span>
                        <span style='color: #10b981; font-weight: 700; font-size: 14px; background: rgba(16, 185, 129, 0.1); padding: 4px 10px; border-radius: 6px; border: 1px solid rgba(16, 185, 129, 0.2);'>{val}</span>
                    </div>");
            }
        }
        else
        {
            sb.Append($@"
                <div style='display: inline-block; padding: 6px 14px; background: linear-gradient(135deg, rgba(16, 185, 129, 0.2) 0%, rgba(59, 130, 246, 0.2) 100%); border: 1px solid rgba(16, 185, 129, 0.4); border-radius: 20px; color: #34d399; font-weight: 700; font-size: 13px; margin-bottom: 12px; letter-spacing: 0.5px;'>
                    🛡️ {part}
                </div>");
        }
    }

    sb.Append("</div>");
    return sb.ToString();
}

app.Run();

// DTO Modely
public class LeadModel
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Topic { get; set; }
}
record LoginRequest(string Username, string Password);
record SaveContentRequest(string Token, Dictionary<string, string> Items);
