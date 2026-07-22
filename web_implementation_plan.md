# Implementační plán: Prestižní web pro Martina Flegla

Tento dokument detailně popisuje vývojovou roadmapu, architekturu a designový systém pro tvorbu kompletního prezentačního "One-Pageru" (jednostránkového webu) pro Martina Flegla.

## 1. Technologický Stack

* **Backend & API:** ASP.NET Core 9.0 Web API (funguje jako backend pro zpracování formulářů a odesílání e-mailů).
* **Databáze:** MS SQL (všechny tabulky s prefixem `flegl_`, aktuálně vytvořena tabulka `flegl_leads`).
* **Frontend:** Čisté HTML5, Vanilla CSS3 a Vanilla JavaScript (bez těžkých frameworků typu React, pro zaručení maximální rychlosti načítání, čistého kódu, plynulých micro-animací a skvělého SEO).
* **Deployment:** Soubory se po zkompilování API nahrají rovnou přes FTP do `/wwwroot/` na Windows hosting (Aspone).

## 2. Design Systém a Vizuální Identita

Web bude koncipován jako vysoce prémiová záležitost (jako vzorový realitní web). Cílem je "WOW efekt" na první pohled.

* **Barevná paleta:**
  * **Pozadí:** Temná černá (`#0a0a0a` a `#000000`) pro hloubku.
  * **Povrchy (karty, boxy):** Tmavě šedá s jemným skleněným efektem (glassmorphism: `rgba(26, 26, 26, 0.8)`).
  * **Akcenty a interakce:** Luxusní zlatá barva (`#d4af37`), zlaté přechody.
  * **Typografie:** Vysoce kontrastní bílá a jemná světle šedá (`#a0a0a0`) pro čitelnost delších textů.
* **Typografie:** Moderní, čisté fonty (např. *Outfit* pro nadpisy, *Inter* pro odstavce).
* **Animace:** Paralaxní scrolling, postupné objevování prvků při rolování dolů (fade-in-up), jemné reakce tlačítek na přejetí myší (hover).

## 3. Informační Architektura (Struktura stránky)

Web bude poskládán z následujících sekcí jdoucích logicky za sebou:

1. **Navigace (Header):** Fixní průhledná lišta s logem a zlatým tlačítkem "Domluvit konzultaci".
2. **Hero Sekce:** Dominantní, pohlcující sekce s luxusním pozadím, hlavní hodnotou ("Vaše jistota ve světě financí") a přímým přesměrováním na konzultaci.
3. **Social Proof (Důvěra na první pohled):** Blok s působivou typografií ukazující tvrdá data: "Od 2010 v oboru", "1400+ klientů", "150+ firem".
4. **O mně (Bio):** Profesionální text z profilu (z textu "O mně") doplněný novou vyříznutou fotografií zkomponovanou s designem pozadí.
5. **10 kroků k cíli (Edukační část):** Interaktivní krokový proces, který klientovi vysvětlí, jak probíhá spolupráce. Odstraňuje strach z neznáma.
6. **Naše Služby (Pro občany a firmy):** Výstavní skříň služeb ve formě moderního mřížkového uspořádání (Bento box grid). Služby budou řazeny do přehledných karet (Např. Hypotéky, Majetek, Život...).
7. **Případové Studie:** Sekce dokazující kompetenci na reálných datech z praxe.
8. **Závěrečné CTA & Kontakt:** Mapa s pobočkami (Trutnov, Dvůr Králové), kontaktní údaje, IČO a vygenerovaný QR kód s rychlým odkazem na konzultační formulář.

## 4. Vývojové Fáze & Úkoly

Abychom zajistili plynulý pokrok, budeme postupovat takto:

* **Fáze 2.1: Založení Layoutu a Design Systému**
  * Vytvoření `index.html` (hlavní kostry).
  * Vytvoření souboru `style.css` a definice všech CSS proměnných (barvy, stíny, fonty).
  * Nastavení SEO meta tagů.
* **Fáze 2.2: Vývoj hlavních sekcí**
  * Kódování Hero sekce a statistik (odstranění zástupného formuláře a přesunutí formuláře na doménu `/konzultace` nebo vytvoření modálního okna).
  * Kódování sekce "O mně" s fotkou a sekce "Služby" pomocí CSS Grid.
* **Fáze 2.3: Interaktivita a Animace**
  * Implementace JavaScriptu pro hladké skrolování a navigaci.
  * Vytvoření IntersectionObserver skriptů pro animování prvků při scrollování (objevování karet, počítání čísel ve statistikách).
* **Fáze 2.4: Dokončení a Propojení**
  * Sloučení sběrného formuláře do designu tak, aby byl snadno dostupný.
  * Optimalizace pro obrovské i malé mobilní obrazovky.
  * Finální vizuální doladění.

---
Tento plán je vytvořen na míru pro maximální prémiovost a splnění úvodního vizuálního vzoru, přičemž bezpečně spolupracuje s připraveným C# backendem.
