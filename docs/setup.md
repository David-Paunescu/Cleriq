# Cleriq — Setup operațional

## Cerințe minime

- **SQL Server local** (orice ediție; Trusted Connection)
- **Docker Desktop** cu auto-start la sign-in
- **Node ≥24.15** SAU **≥22.22.3** (pentru Angular CLI v22 — 24.13 e refuzat la runtime)
- **Visual Studio** cu workload .NET (Package Manager Console pentru migrații)
- Opțional pentru fluxuri specifice: cont Twilio trial (SMS), cont Mailtrap sandbox (email), cont RunPod (transcriere — vezi `whisper.md`)

## Infrastructură locală

### Container Redis

```bash
docker run -d --name cleriq-redis \
  -p 127.0.0.1:6379:6379 \
  -v cleriq-redis-data:/data \
  --restart unless-stopped \
  redis:8-alpine \
  redis-server --appendonly yes
```

- Bind pe `127.0.0.1` exclusiv (NU expus în rețea)
- Volume named `cleriq-redis-data` pentru persistență
- AOF activat (`appendonly yes`) — supraviețuiește restart
- Restart automat la pornirea Docker Desktop

**Index Redis**: 0 = dev (cheile DP — **NU șterge, NU FlushAll**), 15 = teste (golit per rulare prin fixture).

**Inspecție chei**: intrările `IDistributedCache` sunt HASH-uri Redis → `HGET <cheie> data` (GET simplu dă `WRONGTYPE`).

### SQL Server

Două baze:
- `Cleriq` (dev) — persistă între sesiuni
- `CleriqTest` (teste) — recreată per rulare de WebApplicationFactory

Connection string dev: `Server=.;Database=Cleriq;Trusted_Connection=True;TrustServerCertificate=True`.

## Configurare aplicație

### `appsettings.json` (comun, comis)

Conține setări non-secrete: `Jwt:Issuer/Audience/ExpireMinutes/RefreshExpirareZile` (FĂRĂ `Key`), `Worker:IntervalSecunde`, `Worker:IntervalTranscriereSecunde`, `Smtp:TimeoutSecunde`, `Whisper:TimeoutOre`, `Whisper:ModelFolosit`, `Twilio:FromNumber`, `RateLimiting:PublicRequesturiPeFereastra`, `RateLimiting:PublicFereastraSecunde`, `DirectorDocumente:CaleRoot`, `DirectorAudio:CaleRoot`.

### `appsettings.Development.json` (comis)

`ConnectionStrings:Default` (SQL Server local), `ConnectionStrings:Redis` (`localhost:6379`), `Portal:UrlBaza`, `Cors:OriginiPermise: ["http://localhost:4200"]`.

### `appsettings.Production.json` (comis)

DOAR `Logging`. Restul prin env vars (vezi mai jos). Conține comentariu-checklist cu variabilele obligatorii.

### `user-secrets` (Development, exclusiv local)

```json
{
  "Jwt:Key": "<32+ bytes crypto-safe>",
  "SuperAdmin:Email": "superadmin@cleriq.ro",
  "SuperAdmin:Parola": "SuperAdmin1!",
  "Whisper:UrlBaza": "<URL pod RunPod>",
  "Whisper:ApiKey": "<cheie pod>",
  "Twilio:AccountSid": "<AC...>",
  "Twilio:AuthToken": "<...>"
}
```

`secrets.json` editat direct în Visual Studio (preferință user — Project → Manage User Secrets).

### `.gitignore`

```
Cleriq/Data/Documente/
Cleriq/Data/Audio/
```

(Cheile Data Protection sunt în Redis, NU pe disk.)

### QuestPDF License

`QuestPDF.Settings.License = LicenseType.Community` în Program.cs **ÎNAINTE** de prima generare PDF (altfel excepție). Gratuit < 1M USD venit anual; re-verificat la depășirea pragului.

## Credențiale seed dev

`SeedComun` rulează mereu (roluri + SuperAdmin, fail-fast la `SuperAdmin:Email/Parola` lipsă). `SeedDevelopment` rulează doar pe `IsDevelopment()`, idempotență pe slug (slug-urile soft-deleted = "arse", NU se recreează).

**SuperAdmin** (`InstitutieId = 0`):
- `superadmin@cleriq.ro` / `SuperAdmin1!`

**Slobozia** (slug `primaria-slobozia`, Ialomița, Tip `Oras`):
- Admin: `admin.slobozia@cleriq.ro` / `AdminSlobozia1!`
- Secretar: `secretar.slobozia@cleriq.ro` / `Secretar1234!`
- Consilier cont (ConsilierId=1, Ion Popescu): `ion.popescu.cont@slobozia.ro` / `Consilier1!`
- 3 consilieri seedați: Ion Popescu, Vasile Georgescu, Test Filtru
- Telefoanele = `null` (Twilio real ar trimite SMS unui străin)

**Focșani** (slug `primaria-focsani`, Vrancea, Tip `Municipiu`):
- Admin: `admin.focsani@cleriq.ro` / `AdminFocsani1!`
- Fără config SMTP, fără consilieri seedați

### Reconstrucție DB dev de la zero

```sql
USE master;
ALTER DATABASE Cleriq SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE Cleriq;
CREATE DATABASE Cleriq;
```

Apoi în Package Manager Console: `Update-Database`. Pornire aplicație → seed automat.

Date noi de dev se adaugă în `SeedDevelopment`, NU manual în SSMS.

## Frontend setup

Folder `cleriq-web/` în root-ul repo-ului, lângă `Cleriq/`.

- `environment.development.ts`: `apiUrl: 'https://localhost:7006'`
- `environment.ts` (prod): `apiUrl: ''` (relative, same-origin prin reverse proxy)
- `ng serve` → `http://localhost:4200`
- `ng lint`, `ng build`

Backend trebuie să ruleze pe `https://localhost:7006` cu container `cleriq-redis` pornit. Login dev canonice: vezi credențialele seed de mai sus.

## Smoke testing — `Cleriq.http`

Fișier minimal, 5-7 blocuri canonice:
- 3 login-uri (Admin Slobozia, Secretar Slobozia, SuperAdmin)
- 1 Provisioning (creare instituție nouă)
- 1 Register Secretar
- 2 blocuri auth: Refresh, Logout

Testele specifice se scriu inline în fișier și se șterg la final. Fișierul NU se umflă peste timp.

### Capcane `.http`

- **Indentarea NU e permisă** pentru linii cu verb HTTP sau header-e — sparge parserul.
- Fiecare bloc nou separat cu `###`.
- Comentariile despre așteptări se pun pe linia `###` (separator), **NU sub body** — altfel `#` se interpretează ca parte din JSON și sparge parsing-ul.

### Upload binar în teste

Cleriq.http NU injectează fișiere binare (trimite calea ca text). Pentru upload real folosește **PowerShell + `curl.exe`**:

```powershell
curl.exe -X POST `
  -H "Authorization: Bearer $token" `
  -F "fisier=@audio.mp3" `
  https://localhost:7006/api/Sedinte/1/Transcriere
```

### Capcane Windows / PowerShell

- **`-SkipCertificateCheck`** doar PowerShell 7+. PS 5.1 fallback:
  ```powershell
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
  ```
- **U+202A** (LRE — Left-to-Right Embedding) — caracter invizibil la copy-paste de căi din Windows Properties/Explorer → `curl: error 26`. Fix: `cd` în folder + nume relativ, NU calea absolută din Properties.

## Servicii externe în dev

### Twilio trial

- Prefix automat în SMS: **`Sent from your Twilio trial account -`**. Dispare la upgrade Pay-as-you-go (fără cumpărare, doar adăugare metodă plată). Cosmetic.
- **Eroare 21608**: trial respinge SMS spre destinații neverificate. **Caller ID adăugat manual în Twilio Console** înainte de orice test.
- Cost real diacritice: 2 segmente UCS-2 per SMS (140 chars). La 100 primării × 120 SMS/lună ≈ 4.000 RON/lună. Revizuire negociere/provider local la scalare.
- `Twilio:AccountSid` lipsă în config → `NotificareLogger` activat automat (fără cont Twilio funcționează local).

### Mailtrap sandbox

- Rate limit `"5.7.0 Too many emails per second"` la trimiteri la ~1s distanță. **Limitare DE MEDIU DE TEST, NU bug**. Producția folosește SMTP-ul real al primăriei.

### Pod Whisper

Vezi `whisper.md` — atașat când lucrăm pe transcriere. `Whisper:UrlBaza` lipsă în config → worker NU se înregistrează, transcrierile rămân `InAsteptare` (warning la pornire).

## Producție

### Mediu

`ASPNETCORE_ENVIRONMENT=Production`. Migrațiile se aplică automat la pornire prin `MigrateAsync`.

### Variabile de mediu obligatorii

Convenția .NET pentru chei nested: `__` (dublu underscore) ca separator.

```bash
ConnectionStrings__Default=...
ConnectionStrings__Redis=...
Jwt__Key=...                          # crypto-safe, 32+ bytes
Portal__UrlBaza=https://...
SuperAdmin__Email=...
SuperAdmin__Parola=...
Whisper__UrlBaza=...                  # opțional — fără el, transcrierile NU rulează
Whisper__ApiKey=...
Twilio__AccountSid=...                # opțional — fără el, SMS-urile sunt logate
Twilio__AuthToken=...
Cors__OriginiPermise__0=https://...   # array: __0, __1, ...
```

**Lipsă** `Jwt__Key` / `SuperAdmin__*` / `ConnectionStrings__*` → **fail-fast la pornire**.

### Redis producție

**Policy de evicție OBLIGATORIE**: `volatile-lru` (sau orice `volatile-*`). `allkeys-*` ar evacua cheile Data Protection (fără TTL) → parolele SMTP pierdute ireversibil → tenants NU mai pot trimite emailuri.

Backup producție = include datele Redis (AOF deja activat în container). Pierderea Redis = pierderea cheilor DP.

### Multi-instance (precondiții)

Pilot = **single-instance**. La multi-instance, două gărzi obligatorii înainte de scalare:
- `ILacatDistribuit` pe `MigrateAsync` (acum migrate-at-startup pe Production e cod nevalidat la multi-instance)
- Rate limiting per-IP în reverse proxy (defense in depth peste limitarea globală `/public/*`)

### CORS

`Cors:OriginiPermise` whitelist strict, array. `AllowCredentials` interzis (JWT pe header). Config lipsă → fail-closed (origini = gol).

## Multi-instance test local

Pentru testarea lock-urilor distribuite și a cache-ului Redis partajat:

```bash
# Instanța 2, port alternativ
cd Cleriq
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ASPNETCORE_URLS="https://localhost:7007"
dotnet run --no-build --no-launch-profile
```

`--no-build` **OBLIGATORIU** (altfel lock pe binare cu instanța 1).

## Capcane mediu

### Hot Reload Visual Studio

**NU aplică** schimbări de constructor sau câmpuri de clasă. Pentru orice modificare a DI, signature constructor, sau adăugare de câmp pe entitate: **Stop → Rebuild → Start**.

### CI / cross-platform

Testele de mentenanță depind de `File.SetCreationTimeUtc`. **Pe Linux** birth time NU e setabil → garda 1h pentru fișiere recente ar proteja toți orfanii → testele de mentenanță ar pica.

CI Linux = sesiune dedicată pre-deployment (necesită ajustare teste sau condiționare per platformă).

### Pachete NuGet adăugate cumulativ

QuestPDF, Markdig, MailKit, Twilio, Microsoft.Extensions.Caching.StackExchangeRedis, Microsoft.AspNetCore.DataProtection.StackExchangeRedis, StackExchange.Redis.
