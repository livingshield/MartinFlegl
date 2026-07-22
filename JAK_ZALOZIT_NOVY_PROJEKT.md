# Jak založit nový projekt (příklad: Patrioti Trutnov)

Tento dokument krok za krokem popisuje proces vytváření nového webového projektu pomocí vytvořené šablony. Tyto instrukce využijete při zakládání složky pro **Patrioti Trutnov** (nebo jakýkoliv jiný nový web), aby se správně propojilo backendové API (ASP.NET Core 9), FTP a databáze.

## 1. Zkopírování šablony

Prvním krokem je vytvoření nové složky projektu ze základní šablony `Template.Web`.

1. Zkopírujte celou složku `Template.Web` a přejmenujte ji (např. na `PatriotiTrutnov.Web`).
2. Uvnitř nové složky přejmenujte soubor `Template.Web.csproj` na `PatriotiTrutnov.Web.csproj`.

## 2. Nastavení přístupových údajů a prostředí (.env)

Všechny projekty načítají kořenový soubor `.env` (o úroveň výš, ve složce `MartinFlegl`), nebo si konfiguraci můžete nastavit lokálně.
Důležité hodnoty, se kterými pracujeme (a které by měly být buď v `.env` nebo v systémových proměnných na serveru):

### Databáze (MS SQL - Aspone)
* **Server:** `sql8.aspone.cz`
* **Základní řetězec (.env):** 
  `DB_CONNECTION_STRING="Server=sql8.aspone.cz;Database=db4937;User Id=db4937;Password=heslo;Encrypt=False"`

### FTP Přístup pro nasazení (Aspone)
* **Host:** `windows11.aspone.cz`
* **Uživatel:** `EkoBio.org_lordkikin`
* **Heslo:** Tvé FTP heslo (definováno v `.env` i ve vizuálních deploy skriptech)
* **Cílová složka na FTP (pro nové projekty):** `www/wwwroot/<nazev_projektu>/`

### SMTP (pro odesílání leadů z formuláře)
API v `Program.cs` se pokouší načíst následující proměnné, které musíte pro odesílání logů nastavit v `.env`:
* `SMTP_HOST` (např. smtp.tvujsite.cz nebo smtp.seznam.cz)
* `SMTP_USER` (přihlašovací e-mail k SMTP - musí patřit odesílateli)
* `SMTP_PASS` (heslo k mailu)
* `TARGET_EMAIL` (tvůj e-mail, kam mají poptávky chodit)

*(Pokud tyto e-mailové klíče v `.env` chybí, uloží se lead jenom do Databáze).*

## 3. Úprava backendu (Program.cs)

Otevřete `Program.cs` ve vaší nové složce (např. `PatriotiTrutnov.Web/Program.cs`) a proveďte následující personifikační úpravy:

1. **Změna jména DB Tabulky!**
   Najděte řádek vytvářející tabulku SQL.
   *Změňte `template_leads` na unikátní název pro klienta*, například `patriotitrutnov_leads`. To musíte změnit na **dvou místech**:
   * U definice CREATE TABLE (`IF NOT EXISTS...`).
   * U definice INSERT INTO v metodě odeslání e-mailu (`INSERT INTO patriotitrutnov_leads...`).

2. **Změna odesílatele e-mailu (Sender Identity)**
   V bloku pro odesílání e-mailů najděte formování zprávy (`var message = new MimeMessage();`).
   *Změňte "Project Template Web" na název projektu, např. "Patrioti Trutnov Web"*, aby bylo jasné, z jakého webu notifikace dorazila:
   `message.From.Add(new MailboxAddress("Patrioti Trutnov Web", smtpUser));`

## 4. Úprava frontendu (Frontend & Design)

Všechny frontendové soubory se nachází ve složce `wwwroot/`.

1. Otevřete `wwwroot/index.html` a změňte obsah, loga a texty (`<title>`, Hero sekce).
2. Otevřete `wwwroot/css/main.css` a přebarvěte proměnné na začátku souboru (odstíny, primární barvy, fonty) do brandingu klienta.

## 5. Příprava a spuštění Deploy skriptu

Aby se nový web dostal na produkci (např. `ekobio.org/patriotitrutnov`), využijte script `deploy.ps1`.

1. Otevřete `deploy.ps1` uvnitř nové složky.
2. Změňte proměnnou `$projectName` na název koncové složky.
   Příklad: `$projectName = "patriotitrutnov"`
3. Ujistěte se, že proměnná prostředí `$localPath` odkazuje pevně do správné podsložky nového projektu (např. `c:\...\PatriotiTrutnov.Web\publish`).
4. Uložte skript.

## 6. Sestavení a nasazení (Build & Deploy)

Jakmile je vše dokončeno a uloženo, přistupte k terminálu a spusťte kompilaci a odeslání:

1. Proveďte build v adresáři nového projektu (PowerShell):
   `dotnet publish -c Release -o publish`
2. Spusťte připravený script:
   `.\deploy.ps1`

Skript vytvoří patřičné adresáře na FTP (uvnitř složky `www/wwwroot/patriotitrutnov/`) a nakopíruje zkompilované .NET soubory. Aplikace tím bude online a ihned bude reagovat na `ekobio.org/patriotitrutnov/`.
