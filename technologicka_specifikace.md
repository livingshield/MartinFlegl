# Technologická specifikace: Projekt Martin Flegl

Tento dokument definuje technologické standardy, správu tajemství (secrets) a strategii nasazení pro projekt **Martin Flegl**, s ohledem na předchozí problémy s backendem na FTP.

## 1. Správa verzí a `.gitignore`

V repozitáři nesmí nikdy skončit citlivá data (hesla, klíče) ani zbytečné balíky.

**Co všechno bude v `.gitignore`:**

* **Citlivé soubory s klíči:** `.env`, `appsettings.Production.json`, `secrets.json`, `.ftpconfig` (nástroje pro FTP upload).
* **Závislosti:** složka `node_modules/` (pokud se používá NPM), složky `bin/` a `obj/` (pokud se používá .NET), `vendor/` (pokud se používá PHP).
* **Lokální konfigurace IDE:** `.vscode/`, `.idea/`, `*.suo`, `*.user`.
* **Build výstupy:** `dist/`, `build/`, `out/` (produkční soubory se generují až na konci, nebo se přes FTP nahrávají jen ony, ale do gitu nepatří zdrojové + buildnuté duplicitně).
* **Dočasné soubory:** `*.log`, `.DS_Store`, `Thumbs.db`.

## 2. Správa přihlašovacích údajů (Databáze, FTP, AI klíče)

Hesla a API klíče se **nikdy nevkládají přímo do zdrojového kódu**.

* **Lokální vývoj (Localhost):** Použijí se proměnné prostředí (Environment Variables) načítané ze souboru `.env`, nebo u .NET aplikace přes nástroj `Secret Manager` či soubor `appsettings.Development.json` (který může být v gitu jen pokud obsahuje dummy data, jinak v ignore).
* **Produkční prostředí (FTP server):**
  * Přístup k DB a AI klíče budou bezpečně uloženy na serveru (např. v souboru `appsettings.json` nebo `.env` nahraném **pouze** přes FTP a vynechaném z Gitu).
  * FTP přístupové údaje pro automatický deployment (např. v GitHub Actions) budou uloženy v zabezpečených sekrecích repozitáře (GitHub Secrets), odkud si je sype nasazovací skript.

**Konvence pro databázi:**
Veškeré nově vytvářené tabulky v databázi musí striktně využívat prefix `flegl_` (např. `flegl_users`, `flegl_leads`), aby se předešlo případným kolizím s jinými projekty ve sdíleném databázovém prostoru (např. Aspone).

## 3. Back-end strategie na běžném FTP hostingu

Běžné levnější FTP hostingy (např. Aspone, Wedos) často plně nepodporují běh Node.js backendu v režimu serveru nebo mají složité chování pro ESM (ECMAScript moduly). To vede k "zástupným" chybám, kdy nefunguje loadování JS souborů na serveru.

**Jak úspěšně dělat backend v tomto prostředí:**

* **Varianta A: Statický Frontend + Externí API / Serverless** (Nejbezpečnější pro FTP)
  * Frontend (HTML/JS/CSS nebo zkompilovaný React/Vue) se nahraje na FTP.
  * Backendové operace (volání AI klíčů, databáze) **nesmí běžet v prohlížeči**, jinak klíče uniknou. Pro backend se využije buď:
    * Lehký PHP skript (pokud to FTP podporuje) fungující jako proxy pro skrývání AI API klíčů a komunikaci s databází.
    * Oddělený Cloud Backend (např. Vercel Serverless Functions, Supabase, Firebase) nezávislý na lokálním FTP souborovém systému.
* **Varianta B: Kompilovaná .NET/Blazor aplikace (pokud cílíme na Windows Hosting typu Aspone)**
  * Aplikace se napíše např. v ASP.NET Core API.
  * Pomocí příkazu `dotnet publish -c Release` se vygenerují produkční soubory.
  * **Všechny** tyto vygenerované soubory (z Output složky) se překopírují přes FTP na server do složky `/wwwroot/`. Databáze se volá zabezpečeně přes backend.

## 4. Architektura krok za krokem

1. **Vytvoření kostry:** Založíme složku `MartinFlegl`, inicializujeme Git, vytvoříme solidní `.gitignore`.
2. **Volba Backend Proxy:** Rozhodnutí, zda backendové API napíšeme v PHP (pokud je dostupný jen letitý hosting) nebo v C#/.NET (jako pro Scio/Aspone). Umožňuje bezpečné ukládání AI klíčů z dosahu uživatele.
3. **Vývoj a testování:** Lokální vývoj přes `.env` vars.
4. **Deploy přes FTP:** Všechny testované zdrojové kódy (případně po buildu) se synchronizují pomocí FTP nástroje (jen změněné soubory). Klíče `.env` se uploadují ručně jednorázově pro produkci.

## 5. Analýza referenčního webu (Vzor pro finančního poradce)

Tato sekce obsahuje analýzu referenčního webu z pohledu obsahu, vizuálu i technického provedení. Web slouží jako vynikající předloha pro finančního poradce, protože jeho struktura je silně postavená na budování důvěry, prezentaci reálných výsledků a transparentnosti.

### 5.1. Informační architektura a obsah (Content & Flow)

Web funguje jako propracovaná "landing page" s jasným cílem: konvertovat návštěvníka v lead přes výrazné CTA „Domluvte si konzultaci“.

* **Hero sekce:** Hned nahoře je jasná "Value Proposition" (*Silný tým. Silný výsledek.*) a důraz na to, že o klienta bude postaráno bez stresu.
* **Social Proof (Statistiky):** Následují tvrdá data (počet let na trhu, objem portfolia, velikost týmu). Pro finančního poradce je to naprosto klíčový prvek – lze zde implementovat např. objem spravovaného majetku nebo počet zajištěných rodin.
* **Transparentní proces (10 kroků):** Skvělý UX prvek. Rozpadnutí složité služby na 10 srozumitelných kroků zbavuje klienta strachu z neznámého. U financí by to mohl být proces od úvodního auditu až po pravidelný servis portfolia.
* **Případová studie (Case Study):** Ukázka reálného obchodu. Pro finanční web se zde nabízí komponenta ukazující zhodnocení investic nebo záchranu klienta před nevýhodnou hypotékou.
* **Prezentace týmu:** Ukazuje obrovské zázemí a specializaci jednotlivých členů.

### 5.2. Grafika, UI a Vizuální styl

* **Čistota a prémiovost:** Design sází na minimalismus, hodně bílého místa (whitespace) a moderní, čitelnou typografii.
* **Vizuální konzistence:** Portrétové fotografie týmu mají jednotný styl, stejné nasvícení a barevný grading, což přidává na profesionalitě.
* **Kompozice a grid:** Používá se asymetrický grid pro texty a fotky, který rozbíjí monotónnost, a karty (cards) s jemným zaoblením.

### 5.3. Technický pohled a UX

* **Technologický stack:** Původní web byl pravděpodobně postaven na Webflow (odpovídá tomu plynulost animací). Pro klienta lze postavit na moderním stacku (např. Next.js, Nuxt, nebo napojení na headless CMS).
* **Optimalizace obrázků:** Web hodně stojí na vizuálu. Moderní formáty (WebP/AVIF) a lazy loading budou nutností pro udržení rychlého načítání.
* **Mobilní responzivita u dlouhých gridů:** U "one-pagerů" s mnoha bloky (jako je sekce týmu) bude klíčové přeskládání gridu na mobilu, aby uživatel nemusel nekonečně dlouho scrollovat (např. implementace horizontálního swipe slideru pro mobilní zobrazení).

**Doporučení pro další postup:**
S finančním poradcem je vhodné projít hlavně sekce „Případová studie“ a „10 kroků“. Pokud dodá podobně strukturovaná a tvrdá data ze své praxe, lze navrhnout web s vysokým konverzním poměrem. Následovat může návrh struktury komponent nebo datového modelu pro vybraný CMS.

---

*Vyčkávám na potvrzení celého konceptu (architektury i návrhu), abychom mohli projekt úhledně založit a posunout se k vývoji.*

---

## 6. Obsah a služby ze současného webu klienta (Zdrojové texty)

Tato sekce obsahuje strukturované informace extrahované z aktuálního webu klienta (insia.cz/martin-flegl). Tyto texty poslouží jako hrubý stavební materiál pro navrhování nové struktury webu a pro copywriting jednotlivých komponent.

### 6.1. O mně (Představení pro "Bio" sekci)

Dobrý den, ve finančním oboru působím od roku 2010. Během této doby jsem navázal spolupráci s více než 1 400 klienty a 150 firmami a podnikateli, kterým dlouhodobě pomáhám řešit finanční záležitosti. Svoji práci vnímám jako průvodcovství světem financí. Klientům poskytuji podporu nejen při uzavírání smluv, ale i v situacích, které přináší život. Vždy se snažím, aby naše spolupráce byla založena na důvěře a byla dlouhodobá. Jako nezávislý finanční specialista spolupracuji s většinou významných finančních institucí na trhu. Díky tomu mohu doporučit řešení šitá na míru – od zajištění majetku a života až po financování bydlení či investice. Cílem mé práce je minimalizovat Vaše starosti, snížit administrativní zátěž a pomoci Vám na cestě k finanční jistotě. Rád se s Vámi potkám osobně v mých kancelářích v Trutnově nebo ve Dvoře Králové nad Labem.

### 6.2. Služby pro občany

* **Životní pojištění:** Zajištění rodiny pro případ předčasného odchodu, splacení hypotéky. Součástí je investování tvořící rezervu na penzi (s daňovými úlevami).
* **Pojištění invalidity:** Ochrana před výpadkem příjmu a dodatečnými náklady, pokud dojde ke snížení pracovní schopnosti ze zdravotních důvodů (o více než 35 %).
* **Pojištění vážných nemocí:** Připojištění kryjící nemoci nad rámec standardního krytí.
* **Úrazové pojištění:** Zahrnuje krytí smrti úrazem, trvalých následků a denní odškodné za dobu léčení.
* **Pojištění nemovitosti a domácnosti:** Ochrana budov a veškerého vnitřního vybavení vč. elektroniky a cenností proti živlům, krádežím a dalším rizikům.
* **Pojištění chaty / chalupy:** Specifické pojištění pro případ vykradení (např. nářadí) a živelné škody u rekreačních staveb.
* **Pojištění odpovědnosti rodiny:** Takzvaná "pojistka na blbost". Pojišťuje situace, kdy člen rodiny či mazlíček způsobí škodu (rozbité zboží, vytopení sousedů).
* **Pojištění motorových vozidel:**
  * *Povinné ručení:* S ohledem na limity a asistenci, nejen na nejnižší cenu.
  * *Havarijní pojištění:* Krytí vandalismu, odcizení a škod, které si způsobí řidič sám.
  * *GAP (Garantovaná ochrana hodnoty):* Kryje finanční ztrátu mezi pořizovací cenou nového vozu a aktuálním plněním při totální škodě či krádeži.
* **Cestovní pojištění:** Ochrana zdraví i vybavení v zahraničí.
* **Pojištění zvířat:** Náklady na léčbu u veterináře pro domácí mazlíčky (nemoc, úraz).
* **Pojištění odpovědnosti zaměstnance:** Nahradí zaměstnavateli škody způsobené u plnění úkolů, chráněno až do 4,5násobku platu.
* **Úvěry na bydlení a Stavební spoření:** Kompletní proces od výběru hypotéky k papírování nebo financování menších rekonstrukcí.
* **Spoření na penzi (III. pilíř):** Spoření podporované přímým příspěvkem státu a daňovými úlevami.

### 6.3. Služby pro firmy

* **Pojištění firemních vozidel (Povinné, Havarijní, GAP):** Zahrnuje i výhodné flotilové pojištění.
* **Pojištění přepravy:** Pojištění škod na zboží při zasílání a ochrana dle podmínek INCOTERMS.
* **Pojištění nemovitostí, movitých věcí a zásob:** Od budov přes IT techniku, až po hotové výrobky na skladě. Krytí proti požáru (FLEXA), vodovodním škodám, krádežím apod.
* **Pojištění strojů a elektroniky:** Specifické krytí technické havárie jako zkrat, přepětí, chyba obsluhy u serverů či CNC strojů.
* **Pojištění odpovědnosti a profesní odpovědnosti:** Úhrada újmy způsobené okolí provozní činností (povinné v některých oborech, např. chybný projekt architekta, chyba daňového poradce).
* **Pojištění odpovědnosti za výrobek:** Nezbytné pro výrobce chránící před škodami zaviněnými vadným výrobkem (často vyžadováno při exportu).
* **Zaměstnanecké benefity a pojištění pracovních cest:** Komplexní péče o zdraví vyslaných zaměstnanců v zahraničí bez hromadné byrokracie.

### 6.4. Kontaktní údaje a dostupnost

* **Kanceláře:** Trutnov (Pražská 523, 541 01) a Dvůr Králové nad Labem.
* **Telefon:** +420 736 453 532
* **E-mail:** <martin.flegl@insia.com>
