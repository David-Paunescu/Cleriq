# Cleriq — Backend

## Stack tehnologic

.NET 10 + ASP.NET Core Web API, EF Core 10 cu SQL Server provider (code-first cu migrații), Microsoft.AspNetCore.Identity.EntityFrameworkCore cu chei int, JWT Bearer pentru autentificare. Scalar.AspNetCore pentru OpenAPI viewer (NU Swagger — incompatibil cu API-ul nou). Pachete-cheie: MailKit (SMTP), Twilio, QuestPDF + Markdig (PDF PV), Microsoft.Extensions.Caching.StackExchangeRedis + Microsoft.AspNetCore.DataProtection.StackExchangeRedis + StackExchange.Redis.

Migrațiile se rulează din Package Manager Console (`Add-Migration X` / `Update-Database`). Pe Production, se aplică automat la pornire prin `MigrateAsync` — pilot single-instance; precondiție multi-instance = gardă cu `ILacatDistribuit` pe migrare (notată, neimplementată).

## Convenții cod

**Limba**: cod, comentarii, clase, fișiere, endpoint-uri — toate în română. Termeni standard .NET (controller, repository) păstrați în engleză. Mesaje eroare client — în română. `Console.OutputEncoding = UTF8` în Program.cs.

**Naming**: PascalCase clase/proprietăți, camelCase parametri/locale. Prefix `I` pe interfețe (`IFurnizorTenant`, `IServiciuNotificareEmail`). Verbe pentru acțiuni controller (`Creeaza`, `Actualizeaza`, `Sterge`, `Lista`, `Detalii`).

**DTOs**: records, NU classes. Pattern: un singur fișier `XDto.cs` per entitate cu mai multe records (`CreareXDto`, `ActualizareXDto`, `XDto`). DTO-urile de creare pentru entități tenant **NU expun `InstitutieId`** — se setează automat la SaveChanges.

**Enumuri**: într-un fișier `Models/Enums.cs`, valori explicite (`= 1, 2, 3`). Enumuri folosite doar la afișare (ex: `StatusConvocare` derivat) stau în DTO-ul respectiv. Etichete textuale prin extension `Eticheta()` pe enum în `Helpers/ExtensiiEnumuri.cs`.

**Constrângere EF**: enumuri configurate cu `HasDefaultValue` care NU încep de la 0 trebuie să aibă `HasSentinel` explicit (altfel warning EF Core 9+). Aplicat la `Institutie.SmtpSecuritate` și `PunctOrdineZi.TipVot`.

**DateTime**: peste tot UTC. `ValueConverter` global în `OnModelCreating` forțează `Kind=Utc` la citire (loop cu reflexie pe toate proprietățile `DateTime`/`DateTime?`). API răspunde cu sufix Z. Doar generatorii de text pentru oameni (PV Markdown, email HTML, SMS) convertesc la fus orar local prin `ExtensiiData.LaFusOrar(string fusOrar)`.

**Validare**: în controller. Răspunsuri standardizate: `Ok` cu DTO, `NotFound`, `BadRequest` cu mesaj, `Conflict` pentru duplicate/tranziții invalide, `NoContent` pentru DELETE.

**Authorize**: `[Authorize]` la nivel de clasă cu override `[Authorize(Roles = "...")]` pe scriere. GET-urile sunt deschise oricărui rol autentificat (cu excepții clar marcate). Controllerele publice (`/public/{slug}/...`) NU au `[Authorize]`.

**Comentarii**: minime în cod. Explicațiile arhitecturale rămân în chat. Excepție: o linie scurtă lângă o constantă neevidentă sau ~2-3 linii lângă o gardă cu motiv subtil.

## Modelul de bază

**`EntitateDeBaza` abstract**: toate entitățile de domeniu moștenesc.
- `int Id` (chei surrogate peste tot, inclusiv pe relațiile mulți-la-mulți — protecție defensivă prin filtered unique index, NU PK compus)
- Audit complet: `CreatLa` + `CreatDe`, `ModificatLa` + `ModificatDe`, `StersLa` + `StersDe` (toate `int?` — alignment cu cheile Identity)
- Soft-delete: `EsteSters` (bool)
- `RowVersion` cu `[Timestamp]` pentru optimistic concurrency

**Audit automat**: `AppDbContext.AplicaAuditSiSoftDelete()` populează câmpurile when/who în funcție de starea EF (Added/Modified/Deleted), apelat din override-ul `SaveChanges`/`SaveChangesAsync`.

**`IEntitateCuTenant { int InstitutieId }`** implementată de toate entitățile tenant. `Institutie` NU implementează (e rădăcina), e filtrată prin `Id == InstitutieIdCurenta`.

**Excepție conștientă**: `RefreshToken` și `Utilizator` (Identity) NU moștenesc `EntitateDeBaza`. Sunt infrastructură de securitate — revocarea înlocuiește soft-delete-ul, fără filtru tenant, cleanup fizic permis.

## Multi-tenancy

**Filtru global combinat** (soft-delete + tenant) aplicat automat prin reflexie în `OnModelCreating` oricărei clase care moștenește `EntitateDeBaza`:
- Dacă entitatea e `IEntitateCuTenant` → `!EsteSters && InstitutieId == InstitutieIdCurenta`
- Dacă e `Institutie` → `!EsteSters && Id == InstitutieIdCurenta`

**`IFurnizorTenant`** rezolvă tenant curent cu prioritate:
1. `HttpContext.Items["InstitutieId"]` — setat de `SlugTenantMiddleware` pe rutele publice
2. Claim JWT `"InstitutieId"` (nume EXACT — parte din contract)

**`IFurnizorUtilizator`** separat citește `UserId` din `ClaimTypes.NameIdentifier`. Două servicii distincte intenționat (separare responsabilități).

**Computed properties pe `AppDbContext`**: `InstitutieIdCurenta` (int) și `UserIdCurent` (int?) citesc dinamic prin `_tenant`/`_utilizator` la fiecare acces. **OBLIGATORIU să nu se cacheze în constructor** — `SlugTenantMiddleware` setează tenant DUPĂ construirea DbContext-ului pentru rute publice. EF parametrizează valoarea în query-uri (zero impact performanță). `AplicaAuditSiSoftDelete` citește o singură dată per SaveChanges într-o locală.

**La SaveChanges**, entitățile noi primesc forțat `InstitutieId = InstitutieIdCurenta` (un user nu poate insera în alt tenant chiar dacă încearcă). Excepție: mod system (vezi mai jos).

**SuperAdmin are `InstitutieId = 0`** — flag special, NU un tenant real. `AplicaAuditSiSoftDelete` sare peste forțarea tenant când `InstitutieIdCurenta == 0` (pentru Provisioning).

**`SlugTenantMiddleware`**: rezolvă slug → tenant pentru rute publice, cache Redis cu TTL (cheie `cleriq:tenant:slug:{slug}`). Cache miss → DB lookup → cache write. Restart Redis → re-populare lazy.

**Pattern obligatoriu `IgnoreQueryFilters`**: ridică TOT filtrul global combinat (soft-delete + tenant simultan). Pentru SuperAdmin care vede toate instituțiile dar NU soft-deleted: `.IgnoreQueryFilters().Where(i => !i.EsteSters)`. Pentru restore-on-re-add: caut intenționat printre soft-deleted din ACELAȘI tenant, deci `.IgnoreQueryFilters().Where(x => ... && x.InstitutieId == _context.InstitutieIdCurenta)`.

## Auth + JWT + refresh tokens

**Identity cu chei int**: `AppDbContext` moștenește `IdentityDbContext<Utilizator, Rol, int>`. `base.OnModelCreating(modelBuilder)` se apelează **prima linie** în `OnModelCreating`.

**`Utilizator`**: extinde IdentityUser cu `NumeComplet`, `InstitutieId`, `ConsilierId?` (legătura pentru vot self). `InstitutieId` setat MANUAL la creare — Identity NU trece prin pipeline-ul de audit. Index unic filtrat pe `ConsilierId IS NOT NULL` (un consilier are maxim un cont).

**`Rol`**: extinde IdentityRole. 4 roluri seed la pornire: `SuperAdmin`, `Admin`, `Secretar`, `Consilier` (orizontale, fără moștenire).

**JWT**:
- Access token 15 min, **`ClockSkew = TimeSpan.Zero`** (default-ul .NET de 5 min ar face 15 → ~20)
- Cheia JWT în user-secrets (dev) sau env vars (prod). NICIODATĂ în appsettings.json
- `AddAuthentication` se apelează **DUPĂ** `AddIdentity`, cu lambda care setează explicit toate trei schemele (`DefaultScheme`, `DefaultAuthenticateScheme`, `DefaultChallengeScheme`) pe `JwtBearerDefaults.AuthenticationScheme`. Fără asta, `[Authorize]` returnează 401 cu `Location: /Account/Login` (AddIdentity setează schemele pe cookie).
- Claims: `NameIdentifier`, `Email`, `NumeComplet`, `InstitutieId`, roluri. Claim **`ConsilierId`** CONDIȚIONAT (doar dacă `user.ConsilierId.HasValue`).

**Refresh tokens** (`RefreshToken` entity, nu moștenește EntitateDeBaza):
- 60 zile cu fereastră GLISANTĂ (fiecare rotație emite token nou cu 60 zile proaspete). 60, nu 30, fiindcă ședințele lunare cad la interval de 30-31 zile.
- Hash SHA-256 hex lowercase pe UTF-8 — **PIN DE CONTRACT** (DbTest îl oglindește; schimbarea algoritmului invalidează toate tokenurile)
- 64 bytes random → base64. FK Cascade spre AspNetUsers.
- **Familii** (`Guid Familie`): fiecare login = familie nouă. Logout revocă familia prezentată (inclusiv tokenurile deja folosite — omoară un eventual lanț furat). `RevocaToateAsync(userId)` există pentru viitor schimbare-parolă/dezactivare cont.
- **Rotație atomică**: `ExecuteUpdateAsync` condiționat pe `FolositLa IS NULL` — două cereri paralele NU pot consuma același token.
- **Detecție furt**: token folosit prezentat din nou DUPĂ grația de 60s (const în serviciu, intenționat) → revocă familia ÎNTREAGĂ (inclusiv tokenul "proaspăt" al atacatorului). ÎN grație = cursă legitimă multi-tab → 401 dar familia supraviețuiește.
- **Cleanup**: amortizat la login (DELETE fizic pe tokenurile expirate/revocate ale userului). Tokenurile FOLOSITE rămân până la expirare/revocare — necesare detecției de furt. NU se "optimizează" ștergându-le.

**Endpoint-uri Auth**:
- `POST /api/Auth/login` — returnează `{token, refreshToken}`
- `POST /api/Auth/refresh` — **ANONIM intenționat** (access-ul poate fi expirat la apel; posesia tokenului de 512 biți e autentificarea). Claims REGENERATE din DB (schimbările de rol devin efective în ≤15 min).
- `POST /api/Auth/logout` — ANONIM, **204 mereu**, idempotent, fără scurgere de informație
- `POST /api/Auth/register` — `[Authorize(Roles="Admin")]`. Whitelist intern: **DOAR `Admin`, `Secretar`**. Un Admin compromis NU poate să-și escaladeze drepturile. `InstitutieId` din tokenul Adminului, NU din DTO. **NU permite `Consilier`** — conturile de consilier se creează exclusiv prin `POST /api/Consilieri/{id}/Cont` (linking explicit cu `ConsilierId`).

**Provisioning** (UNICA cale de a crea o instituție):
- `POST /api/Provisioning` — `SuperAdmin only`. Tranzacție atomică: creează `Institutie`, apoi primul `Utilizator` Admin pentru ea. Eșec creare user → rollback.
- Slug: explicit furnizat SAU auto-derivat din denumire prin `Slugify`. Verificare unicitate INCLUSIV soft-deleted (slug-urile arse — `IgnoreQueryFilters`). La conflict, returnează sugestii filtrate de disponibilitate reală.

## Soft-delete + cascadă

**Soft-delete peste tot, NICIODATĂ ștergere fizică**. Cerință administrativă/legală. `_context.X.Remove(entity)` se traduce automat în soft-delete prin override-ul `SaveChanges`.

**Cascadă cu worklist pattern** în `AplicaAuditSiSoftDelete`:
1. Snapshot tracked entities
2. Coadă FIFO
3. Procesare până la golire, adăugând copiii marcați pe parcurs

Permite cascadă N-level atomică într-un singur `SaveChanges`. Modelul curent NU are cicluri — nu există `HashSet` de protecție (de adăugat dacă apar).

**Cascade declarate** în `AplicaCascadaSoftDelete(parinte, ...)` cu switch pe tipul entității:
- `Consilier → ComisieMembru, Mandat, SemnatarHcl`
- `Persoana → MandatFunctie, SemnatarHcl`
- `Sedinta → Prezenta, ProcesVerbal, PunctOrdineZi, Convocare, Document, Transcriere`
- `PunctOrdineZi → Vot, Document`
- `Hcl → SemnatarHcl, ComunicareHclPrefect, Document`

**`IncercareTrimitere` NU cascadează** la soft-delete din `Convocare` sau `Sedinta` — audit forensic complet, toate tentativele din toate "rundele" rămân vizibile.

**FK Institutie cu Restrict pe toate entitățile tenant** (10+). Două motive: (1) SQL Server interzice multiple cascade paths spre aceeași entitate; (2) filosofia soft-delete face cascade-ul DB dead code. Restrict = protecție defensivă DB împotriva DELETE fizic accidental prin SSMS.

**FK-urile spre Consilier cu Restrict** (`ComisieMembru`, `Prezenta`, `Vot`, `Convocare`, `Utilizator.ConsilierId`). Audit istoric rămâne în DB chiar dacă consilierul ar fi șters fizic (imposibil oricum prin app).

**Filtered unique indexes** pe relațiile cu soft-delete: `HasFilter("[EsteSters] = 0")`. Permite multiple rânduri soft-deleted dar maxim unul activ per pereche. Aplicat pe:
- `ComisieMembru(ComisieId, ConsilierId)`
- `Prezenta(SedintaId, ConsilierId)`
- `Vot(PunctId, ConsilierId)`
- `Convocare(SedintaId, ConsilierId)`
- `Transcriere(SedintaId)` (one-to-one cu Sedinta)
- `Utilizator(ConsilierId)` cu filtru `[ConsilierId] IS NOT NULL`
- **Mandate funcție** (3, backstop DB peste `VerificaOverlap`): Primar + Secretar UAT (per `InstitutieId`), Viceprimar (per `InstitutieId, ConsilierId`)
- **HCL** (7): `Hcl(InstitutieId, AnNumerotare, Numar)` (numerotare — numere arse), `Hcl(PunctOrdineZiId)` (max 1 HCL activ/punct), `ComunicareHclPrefect(InstitutieId, AnRegistru, NumarOrdineInRegistru)`, `Document(HclId, NumarOrdinAnexa)` cu filtru `[TipDocumentHcl] = 1`, `RelatieHcl(HclSursaId, HclTintaId, TipRelatie)` cu filtru `[HclTintaId] IS NOT NULL`, `SemnatarHcl(HclId)` × 2 (Președinte rol=1 / Secretar rol=2)

## Pattern-uri obligatorii

**Restore-on-re-add**: la entități cu cheie compusă unică și soft-delete, când se încearcă INSERT pe o pereche care există deja soft-deletată, **restaurăm rândul vechi** în loc să creăm unul nou. Audit-ul rămâne continuu (un singur rând per pereche). Aplicat în: `ComisieMembru`, `Prezenta`, `Vot`, `Convocare`. Pattern de cod:
```csharp
var existent = await _context.X
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(x => x.Key1 == k1 && x.Key2 == k2
                           && x.InstitutieId == _context.InstitutieIdCurenta);
// existent != null && !EsteSters → Conflict 409
// existent != null && EsteSters  → restore (EsteSters=false, StersLa=null, reset câmpuri)
// existent == null               → INSERT nou
```

**Computed properties pe AppDbContext** — vezi multi-tenancy. **Niciodată cacheate în constructor**.

**`IFurnizorTenant.EsteModSystem`** flag explicit pentru workere/jobs system. `AplicaAuditSiSoftDelete` bypass pe `Added` când e mod system (NU suprascrie `InstitutieId` la entități create în context system). `FurnizorTenantSystem` implementare folosită în BackgroundService.

**Entități create în mod system trebuie să seteze MANUAL `InstitutieId`** din contextul lor (ex: helper `CreeazaIncercare` în `WorkerConvocari` setează `InstitutieId = co.InstitutieId` din convocarea părinte). Pattern obligatoriu pentru orice cod care creează entități în afara unui scope HTTP cu tenant.

**`Utilizator` NU implementează `IEntitateCuTenant`** — `InstitutieId` setat explicit la creare (Provisioning, `Auth/register`, `Consilieri/{id}/Cont`). Pattern obligatoriu peste tot unde se creează Utilizatori.

## Vot

**Două dimensiuni distincte**:
- `TipVot` pe `PunctOrdineZi`: `Nominal` / `Secret`
- Cine introduce votul: manual (secretar/admin via `POST /Voturi`) sau self (consilier via `POST /Voturi/Self`)

**Vot secret = anonimizare la CITIRE, nu la stocare**. Legătura consilier↔opțiune se stochează ca la nominal, dar `ExtensiiVot.Rezumat` (helper centralizat, aplicat pe 3 suprafețe: `VoturiController` GET, `PublicVoturiController` GET, generator PV) returnează doar tally-uri agregate + listă participanți (turnout), FĂRĂ opțiunile nominale. Contractul de citire e identic cu o eventuală variantă "secret real la nivel DB" — tranziție viitoare ar schimba DOAR persistența, nu DTO-urile.

**Gărzi vot**:
- `TipVot` **IMUTABIL** odată ce există voturi pe punct (gardă în PUT punct → 409). Blochează escape nominal → voturi → schimbare la secret.
- La secret, `esteManual && TipVot == Secret` → **409** atât în `InregistreazaVot` cât și în `AnuleazaVot`. Singura cale = self-vote-ul consilierului. Consecință acceptată: voturile pe hârtie (consilieri fără cont) NU intră în tally.
- Doar consilierii `Prezent` sau `OnlinePrezent` pot vota (OUG 57/2019).
- După `InchideVot`, punctul are `Rezultat` setat și voturile nu mai pot fi modificate.

**Vot self**: `[Authorize(Roles = "Consilier")]`, `ConsilierId` citit din claim — consilierul NU poate vota pentru altcineva. Funcționează la nominal ȘI secret.

**Calcul majoritate** la `InchideVot`:
- `Simpla`: prag = `(totalExprimate / 2) + 1`
- `Absoluta`: prag = `(totalInFunctie / 2) + 1` (toți activii, prezenți sau nu)
- `Calificata`: prag = `(2 * totalInFunctie + 2) / 3` (formula 2/3 conform OUG 57/2019)

Rezultat: `Adoptat` / `Respins`. Alternativ: `Amana` / `Retrage` setează `Rezultat` direct (fără calcul vot).

## Notificări

**Două interfețe separate**:
- `IServiciuNotificareEmail` cu `DeschideConexiuneEmailAsync(institutieId, ct) → IConexiuneEmail : IAsyncDisposable`. Pool natural prin `await using`.
- `IServiciuNotificareSms` cu `TrimiteAsync(institutieId, telefon, continut, ct)`.

`NotificareLogger` implementează ambele (fallback dev). `NotificareSmtp` (MailKit) implementează DOAR Email. `NotificareTwilio` implementează DOAR SMS.

**SMTP per instituție** (decizie deliberată — emailul "From" e brand-ul oficial legal-relevant):
- 7 coloane pe `Institutie`: Host/Port/Utilizator/ParolaCriptata/EmailFrom/NumeFrom/Securitate
- `SmtpSecuritate { Auto, StartTls, SslDirect }` cu `HasSentinel(Auto)`
- Parola criptată prin `ICriptareSecreta` + `CriptareDataProtection`, purpose `"Cleriq.SmtpCredentials.v1"`
- Cheile Data Protection în Redis (`cleriq:dataprotection-keys`), partajate între instanțe, supraviețuiesc redeploy-ului
- `NotificareSmtp` citește config-ul într-un **scope DEDICAT** (decuplare scope HTTP — workerul rulează fără request context)
- 3 endpoint-uri admin în `SmtpController`: GET (fără parolă), PUT (cu parolă opțională la prima setare), POST `/Test`

**Twilio global** (decizie deliberată — sender ID-ul SMS mai puțin sensibil, alpha sender per primărie NU e garantat în RO pe Twilio):
- Singleton, `TwilioRestClient` explicit prin constructor (NU `TwilioClient.Init` static — permite swap multi-instanță în Faza 3)
- Status sync mapping: `failed`/`undelivered` → eșec; `queued`/`sending`/`sent` → succes la trimitere
- Delivery confirmation reală (status `delivered`) vine asincron prin webhook — Faza 3
- Înregistrare condiționată în Program.cs: `Twilio:AccountSid` prezent → `NotificareTwilio` real; absent → `NotificareLogger` (developer local fără cont Twilio)
- Twilio SDK `CreateAsync` NU acceptă `CancellationToken` — timeout intern SDK (~30s) acoperă cazurile lente

**Normalizare telefon**: helper `Cleriq/Helpers/Telefon.Normalizeaza(input)`. Aplicat la STOCARE, NU la trimitere (date curate în DB, toate E.164). Input cu prefix `0` (fără `00`) tratat ca RO național.

**Diacritice păstrate în SMS** (cost UCS-2 dublu acceptat — produs SaaS premium, "sedinta ordinara" inacceptabil estetic). Optimizare ASCII viitoare = decorator la layer-ul `NotificareTwilio`, NU în business logic.

## Workere

**Două BackgroundService-uri** cu asimetrie deliberată.

**`WorkerConvocari`**: polling cu `Worker:IntervalSecunde` (default 10). Procesează `Convocari` cu `EmailStatus = InAsteptare` sau `SmsStatus = InAsteptare`, doar pe ședințe în `Convocata` sau `InDesfasurare`. Grupează `GroupBy(co => co.InstitutieId)` pentru pool SMTP per instituție. Pattern **trimite-apoi-salvează** → necesită **lock distribuit** (`ILacatDistribuit` cu Redis SET NX + Lua compare-and-delete) per ședință, pentru a evita trimiteri duplicate la multi-instance.

Conținut convocare ÎNGHEȚAT la creare/restore-on-re-add. NICIODATĂ regenerat la re-POST pe convocare activă. Agenda comunicată face parte din actul de convocare oficial — audit cuvânt-cu-cuvânt. Câmpuri pe `Convocare`: `Subiect`, `EmailHtml`, `SmsText`.

Dual-write atomic: rând nou în `IncercareTrimitere` + actualizare flat pe `Convocare` (`EmailStatus`/`EmailTrimisLa`/`EmailDetalii`, similar SMS) în același `SaveChanges`. Câmpurile flat = "last-write cache", `IncercareTrimitere` = istoric complet.

Worker = single-purpose. NU atinge statusul ședinței (doar controllerul). NU setează `FaraDestinatie` (doar controllerul). NU îngheață destinația — `Email`/`Telefon` citite curent din `Consilier` la fiecare tură. `IncercareTrimitere.Destinatar` capturează exact valoarea folosită (audit corect dacă consilierul își schimbă coordonatele între încercări).

Fail-fast pe conexiune SMTP eșuată — toate emailurile grupului primesc rapid `Esuata` cu același mesaj clar. `DbUpdateConcurrencyException` → detach + skip + reia la următoarea tură.

**`WorkerTranscrieri`**: polling cu `Worker:IntervalTranscriereSecunde` (default 30). Secvențial (constraint VRAM), drain loop. Pattern **salvează-apoi-trimite**:
1. Claim atomic `InAsteptare → InProces` prin `ExecuteUpdateAsync` condiționat pe `RowVersion`
2. `SaveChanges` ÎNAINTE de apelul HTTP
3. Apel pod Whisper (timeout 6h)
4. `SaveChanges` cu rezultat → `Finalizata` sau backoff

Asimetria: salvează-apoi-trimite NU cere lock — claim cu `RowVersion` garantează un singur worker preia. Documentat ca decizie deliberată.

Backoff retry: 1 / 5 / 30 min, max 3 încercări. Retriable: 5xx / timeout / network. Non-retriable: 4xx / FileNotFound. `NumarEsecuri`, `UrmatoareaIncercareDupa`, `UltimaEroare` pe entitate.

Shutdown grațios: re-throw, task rămâne `InProces`, curățat la restart (`InProces` cu `ModificatLa` stale > 1h → `Esuata`). `ModificatLa` (audit automat la claim) = timestamp-ul pe care se bazează cleanup-ul orfanilor.

Înregistrare CONDIȚIONATĂ în Program.cs: `Whisper:UrlBaza` prezent → `IServiciuTranscriere` + worker; absent → warning la pornire, transcrierile rămân `InAsteptare`.

**HttpClient pentru Whisper**: Timeout 6h, `SetHandlerLifetime(6h)`, Bearer din `Whisper:ApiKey`.

**`MultipartBodyLengthLimit` global = 500 MB** (pentru upload audio). `[RequestSizeLimit]` per controller restrânge local pentru documente (25 MB) și PV semnat.

## Documente

**`IStocareDocumente` abstracție obligatorie** pentru orice acces fizic la fișiere. Swap cloud (Azure Blob, S3) în Faza 3 = o linie în Program.cs.

**`StocareDocumenteDisk`** implementare curentă (Singleton, zero stare per-request):
- Numele pe disk = **Guid** (NU original) — protejează contra path traversal și coliziuni
- Hash SHA-256 streaming în `CryptoStream` la salvare
- Numele original = metadată, returnat doar la download prin `Content-Disposition`
- `CaleStocare` = identificator OPAC — NU se parse-uiește în business logic

**`Document` entity**: FK Restrict spre `Sedinta` SAU `Punct`, niciodată ambele. Check constraint `CK_Document_ExactUnContext` cu CASE WHEN — extensibil pentru `ProcesVerbalId?` viitor:
```sql
(CASE WHEN [SedintaId] IS NULL THEN 0 ELSE 1 END +
 CASE WHEN [PunctId]   IS NULL THEN 0 ELSE 1 END) = 1
```

**Validare upload**: 25 MB, whitelist `.pdf/.doc(x)/.xls(x)/.jpg/.jpeg/.png`. `TipMime` derivat din extensie (NU header client — ușor de falsificat). Helper `ValidareDocument.Valideaza` + `TipMimePentru`.

**`EstePublic`**: default `false` (publicări accidentale = imposibile). Toggle EXPLICIT prin endpoint dedicat `PUT /Documente/{id}/Vizibilitate`, NU la upload.

**Vizibilitate publică** (în `PublicDocumenteController`):
- `EstePublic == true`
- ȘI sedinta `Status >= Convocata && Status != Anulata`
- Retrogradarea unei ședințe ascunde automat documentele

**Soft-delete NU șterge fișierul fizic** (audit). Cleanup = `MentenantaController` cu `IStocareDocumente.StergeAsync`.

## Transcriere

**Entitate paralelă cu PV**, decuplată (PV-ul e legal corect art. 138; transcript-ul poate avea halucinații). Combinare în output unificat = la export, NU în DB. Unificare cu LLM corector = Faza 3.

**`Transcriere` entity** (one-to-one cu `Sedinta` prin index unic filtrat):
- `Status`: `InAsteptare` / `InProces` / `Finalizata` / `Esuata`
- `ContinutBrut` JSON imutabil după primire (perechi brut+corectat = date pentru fine-tune viitor)
- `ContinutEditat` Markdown editat de secretar, SUPRAVIEȚUIEȘTE la Retry
- `ContinutPublicat` Markdown — **snapshot la momentul publish**. Decuplare draft/publicat: secretarul corectează în liniște și republică explicit
- `DataPublicare`, `PublicataDe` — audit publicare separat de pipeline-ul automat (`ModificatDe` se schimbă la fiecare edit ulterior; `PublicataDe` capturează precis cine a apăsat butonul). Pattern reutilizabil pentru orice "act explicit" viitor.
- `CaleStocareAudio` (în `IStocareAudio`), `DimensiuneAudio`, `DurataAudioSecunde`
- `ModelFolosit` din config (`Whisper:ModelFolosit`, fallback `large-v2`)
- Retry tracking: `NumarEsecuri`, `UrmatoareaIncercareDupa`, `UltimaEroare`

**Idempotență upload**: re-upload acceptat DOAR pe `Esuata` sau soft-deleted (altfel 409). Restore = reset complet câmpuri INCLUSIV `ContinutPublicat`/`DataPublicare`/`PublicataDe` (audio nou = transcriere complet nouă inclusiv față de consilieri). Audio vechi șters POST-COMMIT (nu pierdem dovada dacă SaveChanges eșuează).

**`IStocareAudio` SEPARAT de `IStocareDocumente`**. ~175 linii duplicate acceptate conștient; refactor `StocareDiskBase` la a 3-a stocare.

**Validare audio**: 500 MB, whitelist `.mp3/.wav/.m4a/.ogg/.flac/.aac`. Helper `ValidareAudio`.

**Endpoint-uri Transcriere**:
- `POST /Sedinte/{id}/Transcriere` — upload (Admin/Secretar). Gardă: status `null` SAU `Esuata` SAU soft-deleted
- `GET` detalii / `GET /Continut` / `GET /Audio`
- `PUT /Continut` — editare `ContinutEditat`, gardă `Status = Finalizata`
- `POST /Retry` — gardă `Status = Esuata`, reset retry tracking
- `POST /Publica` — gardă `Status = Finalizata` ȘI `ContinutEditat` ne-vid (`IsNullOrWhiteSpace`). **Idempotent** — același endpoint pentru primul publish ȘI republicare
- `DELETE /Publica` — Admin only, nullează cele 3 câmpuri, păstrează `ContinutEditat`
- `DELETE` — Admin only, soft-delete

**`IGeneratorPromptTranscriere`**: construiește prompt scurt doar-nume din lista de prezență (Prezent/OnlinePrezent), fallback silent toți activii din tenant. Cleriq NU parsează semantic `ContinutBrut` — parser dedicat necesar la viewer Angular (implementat în frontend).

**Health check**: `GET /api/Transcriere/HealthCheck` — `[Authorize(Roles = "Admin,SuperAdmin")]`. Diagnostic rapid pe tot lanțul DNS/TLS/Caddy/Bearer/IP. Pattern pentru orice integrare black-box viitoare (ex: LLM corector).

**Contractul HTTP cu wrapper-ul learnedmachine, pod-ul Docker custom și decizia model (large-v2 + prompt scurt + lecții acustice) sunt detaliate în `whisper.md` — atașat când atingem fluxul transcriere.** În backend rămâne integrarea .NET (entitate, worker, `IServiciuTranscriere`, generator prompt).

## Proces Verbal

**Două niveluri de semnătură electronică implementate**:

**Nivel 0 — PDF generat on-the-fly** din `pv.Continut` (Markdown cu editări secretar):
- Generator `IGeneratorPdfProcesVerbal` cu QuestPDF + Markdig. Motorul Markdown→PDF e `RandareMarkdownPdf` (static, extras în S51, partajat cu HCL); generatorul PV păstrează doar page chrome (margini, watermark DRAFT, antet, footer)
- **QuestPDF Community License**: `QuestPDF.Settings.License = Community` în Program.cs ÎNAINTE de prima generare (gratuit < 1M USD venit anual)
- Draft → watermark "DRAFT" în PDF. Finalizat fără watermark.
- Fără stocare a PDF-ului generat — regenerat la fiecare cerere. Cache Redis pe portal public.

**Nivel 1 — Upload PDF semnat extern** (acoperă PAdES cu token ȘI scan hârtie cu olografe):
- Câmpuri pe `ProcesVerbal`: `CaleStocareSemnat`, `NumeFisierSemnat`, `MarimeSemnat`, `HashSha256Semnat`, `DataIncarcareSemnat`
- **Metadate pe `ProcesVerbal`, NU `Document`** cu `ProcesVerbalId` (1-la-1, reguli proprii de vizibilitate)
- **Stocare fizică în `IStocareDocumente`** refolosit (NU a 3-a stocare). Mentenanță TREBUIE să includă `ProceseVerbale.CaleStocareSemnat` în dicționarele de cleanup — altfel cleanup-ul șterge PV-uri semnate ca orfane. **Pattern critic**: orice tabelă care referențiază fișiere din stocările partajate TREBUIE inclusă în mentenanță.
- Doar PV `Finalizat` poate primi variantă semnată (gard backend). Replace = upload din nou; fișierul vechi devine orfan acceptabil, măturat de mentenanță. **DELETE** semnat = Admin only (mai destructivă decât replace), portalul retrogradează la PDF-ul generat.

**Aplicația NU validează criptografic semnătura** (scan-ul pe hârtie nici nu are semnătură embedded). Răspunderea conținutului = a operatorului (ca la Documente).

**Nivel 2 (semnare in-app prin API furnizor — certSIGN/DigiSign) = Faza 3 doar la cerere piloți. eMOL nu o oferă → NU e cerință de piață.**

**Aprobare oficială (s45)**: 3 câmpuri audit pe `ProcesVerbal` (`DataAprobare`, `AprobatDe`, `AprobatInSedintaId` cu nav `AprobatInSedinta`, FK Restrict). Status (Draft/Finalizat) rămâne axa "redactare internă"; aprobarea = axa "act legal extern" ortogonală. Endpoints `POST/DELETE /Aproba` (Admin+Secretar / Admin only).

**Gărzi**:
- D8 — `POST/DELETE /Semnat` → 409 dacă `DataAprobare != null` (variantă semnată intangibilă după aprobare).
- D7 — `DELETE /Sedinte/{id}` → 409 în DOUĂ scenarii: (1) sedinta = sedinta de aprobare (`AprobatInSedintaId == id`), (2) sedinta deține un PV aprobat (`SedintaId == id && DataAprobare != null`). Fără (2), ștergerea sedintei proprii cascadează soft-delete pe PV-ul aprobat și sparge audit-ul.

Pattern reutilizabil pentru Modulele A/C: orice act aprobat în alt act blochează AMBELE ștergeri (referențiare bidirecțională).

**`ProcesVerbal` entity** (one-to-one cu Sedinta):
- `Continut` Markdown
- `Status`: `Draft` / `Finalizat` (ireversibil)
- `DataGenerare`, `DataFinalizare`

**Flux**:
- `POST /Sedinte/{id}/ProcesVerbal/Genereaza` — Admin/Secretar. Creează draft din date structurate (prezență, ordine de zi, voturi, documente publice cu linkuri). Regenerare permisă pe Draft (suprascrie); blocată pe Finalizat (409).
- `GET /ProcesVerbal` / `GET /Markdown` / `GET /Pdf` (cu watermark dacă Draft)
- `PUT /ProcesVerbal` — editare conținut, gardă `Status = Draft`
- `POST /Finalizeaza` — `Draft → Finalizat`, ireversibil
- Upload/Download/Delete semnat (Nivel 1)

**Generator Markdown**: secțiuni Prezență cu calcul cvorum, Documente atașate ședinței (doar `EstePublic`), Ordine de zi cu rezumat per punct (rezultat, voturi nominale sau tally secret). URL-uri publice spre documente prin `Portal:UrlBaza` config.

**Decizii arhitecturale acceptate** (NU se redeschid fără context):
- PV finalizat = **ireversibil** (OUG 57/2019 + Legea 52/2003). Fără "anulare finalizare" în MVP.
- Typo material în PV finalizat = workaround SQL manual de Admin (frecvență estimată 1-2/an per primărie). Soluție viitoare = flow "Erată" oficial (al 2-lea PV legat de primul prin `EsteErataPentruPvId`, pattern Legea 24/2000) — separat în roadmap.

## Funcții oficiale (Persoane + Mandate)

Cine deține o funcție oficială (Primar / Viceprimar / Secretar UAT) la o dată dată — bază pentru semnăturile derivate pe acte administrative (HCL, viitor Dispoziții). Migrație `AddTrasabilitateFunctii`.

**`Persoana`** (tenant + soft-delete + audit): `NumeComplet`, `Email?`, `Telefon?`. **Separată de `Consilier`** — Primarul și Secretarul UAT pot fi din afara consiliului. Index NON-unic `(InstitutieId, NumeComplet)` (pot exista doi omonimi). `PersoaneController` (api/Persoane): CRUD (scriere [Admin]); DELETE blocat dacă persoana are mandate (activ SAU soft-deleted — audit, gardă `IgnoreQueryFilters`). Telefon normalizat E.164 la stocare.

**`MandatFunctie`** (tenant + soft-delete + audit): `TipFunctie` (Primar/Viceprimar/SecretarUat), `PersoanaId?` XOR `ConsilierId?`, `DataInceput`, `DataSfarsit?` (null = mandat deschis), `NrActNumire?`. **Regula de mapare** (OUG 57/2019): Primar/Secretar UAT = `PersoanaId` (funcționari, nu consilieri); Viceprimar = `ConsilierId` (mereu consilier ales). Impusă pe 3 niveluri: `ValideazaMapare` (controller) + check constraints `CK_MandatFunctie_ExactUnSubject` & `CK_MandatFunctie_FkCorectaPerTip` + FK Restrict spre Persoana/Consilier/Institutie. `CK_MandatFunctie_PerioadaValida` (DataSfarsit >= DataInceput).

**`IServiciuValidareMandate`** (la POST/PUT):
- `VerificaOverlap` — **per-TipFunctie** pentru Primar/Secretar (un singur primar/secretar la un moment dat, indiferent de persoană → 409), **per-(TipFunctie, Consilier)** pentru Viceprimar (mai mulți viceprimari în paralel OK, dar nu același consilier de două ori). Backstop DB: 3 filtered unique.
- `PoateFiViceprimar` — cere ca acel consilier să aibă un mandat de **consilier** care acoperă perioada propusă → altfel 400.

**`MandateFunctieController`** (api/MandateFunctie, scriere [Admin]): GET listă (filtru `tipFunctie`)/detalii; POST/PUT (validare overlap + viceprimar); `POST /{id}/Inchide` (setează `DataSfarsit`; **NU re-validează `PoateFiViceprimar`** — e instrumentul de rezolvare a „viceprimarului fantomă"); DELETE.

**`IServiciuFunctiiIstorice`** — query „cine deținea funcția la data X": `CinEPrimarulLa`, `CinESecretarulUatLa` (→ `Persoana`), `CineEViceprimariiLa`, `CineEConsilieriiLa`, `CineEMembriiComisieiLa`, `CinePresedinteleComisieiLa`. Parametrul `data` = **data evenimentului juridic** (ședință / emitere act), NU data apelului — la regenerare PDF lookup-ul rămâne pe data originală. Consumat de Modul HCL (Secretar UAT pe act).
- **Capcană `IgnoreQueryFilters`**: pe `Persoana`/`Consilier` ridică filtrul (audit — vrem și soft-deleted), dar pe `MandatFunctie`/`Mandat`/`ComisieMembru` RESPECTĂ filtrul global (corecții anulate = soft-deleted NU apar în istoric). Tenant re-impus manual pe ramurile cu `IgnoreQueryFilters` (anti-scurgere cross-tenant).
- **„Viceprimar fantomă"**: viceprimar cu mandat de consilier expirat fără închidere explicită → EXCLUS din `CineEViceprimariiLa` (corectat prin `Inchide`).

**`FunctiiIstoriceController`** (api/FunctiiIstorice) [Admin,Secretar]: GET `Primar`/`SecretarUat`/`Viceprimari`/`Consilieri`/`Comisii/{id}/Membri`/`Comisii/{id}/Presedinte` cu `?data=`. La negăsit → `JsonResult(null)` (200 cu body `null`, NU 404) — contract de citire pentru UI.

## Modul HCL (Faza 3 — Modul A)

Hotărâri de Consiliu Local generate din puncte adoptate: ciclu redacțional + comunicare legală către prefect + publicare portal/MOL. **4 entități noi** (tenant + soft-delete + audit), migrație `AddModulHCL` (include și cele 5 câmpuri signed-PDF — fără migrație separată):
- `Hcl` — actul. Vot snapshot imutabil (`VotPentru/Impotriva/Abtinere` + `TipMajoritate`) copiat la generare.
- `SemnatarHcl` — semnatari derivați (Președinte ședință / Secretar UAT / Semnatar alternativ art. 140). XOR `PersoanaId`/`ConsilierId` (check `CK_SemnatarHcl_*` + FK-corectă-per-rol). Filtered unique pe Președinte/Secretar.
- `ComunicareHclPrefect` — registru comunicări către prefect (numerotare proprie pe an).
- `RelatieHcl` — relații între acte; țintă internă `HclTintaId` XOR externă `ReferintaActExternText` (`CK_RelatieHcl_ExactUnaTinta`).

**Două axe ortogonale** (paritar PV status vs aprobare):
- `StatusHclRedactional`: `Draft → Numerotat → Semnat` (redactare internă).
- Validitate juridică: `DataInvalidare` + `MotivInvalidare` + `RefInvalidare` (anulat prefect/instanță, abrogat, retractat). Un act semnat poate fi invalidat ulterior.

**`HclController`** (`api/Hcl`, 18 endpoint-uri). `POST /Genereaza` din punct adoptat cu **5 precondiții ordonate**: (1) punct ProiectHCL `Rezultat == Adoptat` + `TipMajoritate` setat; (2) ședință `>= Convocata && != Anulata`; (3) `PresedinteSedintaConsilierId` setat; (4) Secretar UAT valid la **data adoptării locală** (`IServiciuFunctiiIstorice.CinESecretarulUatLa`, data = `Sedinta.DataOra.LaFusOrar`); (5) un singur HCL per punct (409). Creează Draft + 2 semnatari + conținut Markdown. Restul: `Continut`/`RegenereazaContinut` (gardă `!= Semnat`), `AtribuieNumar`, `Semneaza`, `Invalidare`±DELETE, `Publicare`, `PublicareMol`±DELETE, `MotivLipsaPresedinte`, `Pdf`, `Semnat` (POST/GET/DELETE), `DELETE`.

**`Semneaza`** (`Numerotat → Semnat`): validează exact 1 Secretar UAT ȘI (1 Președinte SAU ≥2 alternativi art. 140 alin. 2 + motiv setat).

**Numerotare** (`IServiciuNumerotareHcl`): an de registru = anul **local** al adoptării. Numerele soft-deleted rămân **arse** (subdecizie paritară slug-uri instituții — `IgnoreQueryFilters` + tenant re-impus manual). `AtribuieAsync` întoarce rezultat tipat: Succes / NumarInvalid (≤0) / StareInvalidaHcl (≠ Draft) / LacuneNeconfirmate (numărul sare peste sugestie → 409 cu lacunele, confirmabil cu `confirmaCuLacune=true`) / NumarLuat (activ SAU ars → 409 cu sugestie; compare-and-swap pe filtered unique). La reușită, placeholder-ul `PlaceholderHcl.NumarNeatribuit` din conținut e înlocuit țintit cu `{numar}/{an}` în același SaveChanges (păstrează editările secretarului, spre deosebire de regenerare).

**Matricea DELETE = 4 gărzi ordonate cu early-return** (NU switch — stările sunt ortogonale):
1. Comunicări active → 409 (registru prefect inviolabil).
2. `DataInvalidare != null` → OK (override: act mort legal, eliminat la cererea instanței — bate chiar și garda Semnat).
3. Status `Semnat` → 409.
4. `EstePublicat` → 409 (depublică întâi).
Altfel (Draft/Numerotat fără comunicări) → OK; numărul rămâne ars. Pattern reutilizabil la dispoziții/erată.

**Generator PDF + renderer partajat**: `RandareMarkdownPdf` (static, `RandeazaIn(col, markdown)`) extras din `GeneratorPdfProcesVerbal` (S51) — motor Markdig→QuestPDF comun PV+HCL (rule-of-three plătit devreme; single point of failure, fără test PDF byte-level momentan). `IGeneratorPdfHcl` randează `hcl.Continut` (NU regenerează) cu **watermark 3 stări**: INVALIDAT dacă `DataInvalidare != null`, altfel DRAFT dacă `Status != Semnat`, altfel curat.

**Variantă semnată (Nivel 1, paritar PV) — garda „varianta B"**: prima atașare permisă mereu cât `Status == Semnat` (chiar post-MOL — upgrade benign); replace blocat dacă există cale ȘI `DataPublicareMol != null`; DELETE blocat dacă `DataPublicareMol != null`. Divergență intenționată față de PV (ancore diferite: PV `DataAprobare`, HCL `DataPublicareMol`). **Critic**: `Hcluri.CaleStocareSemnat` inclus în scanul de orfani din Mentenanță (stocare partajată cu Documente + PV).

**Semnatari** (`SemnatariHclController`): XOR persoană/consilier, FK-corectă-per-rol (Secretar=persoană, Președinte/Art140=consilier), filtered unique pe rolurile unice, conflict cross-rol pe același consilier. Art. 140 alin. 2: motiv lipsă președinte setat + consilier prezent (Prezent/OnlinePrezent). Ștergerea ultimului alternativ auto-curăță motivul.

**Comunicare prefect** (`ComunicariHclPrefectController` + `RegistruComunicariPrefectController`): gardă `Status >= Numerotat` (art. 197 — termenul curge de la adoptare, nu semnare). `NumarOrdineInRegistru` per (instituție, an), arse — retry loop pe violare unique. Update: imutabile HclId/NumarOrdine/AnRegistru/DataTrimiteri/CanalTransmitere; DELETE = Admin only.

**Termen + alerte T-N** (`CalculatorZileLucratoare` singleton + `IServiciuComunicareHclPrefect`): termen comunicare = **10 zile lucrătoare** de la adoptare (art. 197 alin. 1). Calculatorul acoperă weekend + sărbători fixe (Codul Muncii art. 139) + Paște ortodox (Meeus + 13 zile, **expiră ~2100**), cache per an. `GET /api/Hcl/UrgentDeComunicat?prag=N` — HCL Numerotat+, neinvalidate, fără comunicare live, cu zile rămase ≤ prag (negativ = termen depășit). Filtrare in-memory — OK ~50 HCL/an. Rută literală bate `{id}`.

**Relații** (`RelatiiHclController`): XOR țintă internă/externă, text extern max 300, auto-referință → 400, duplicat (sursă,țintă,tip) → 409. Se administrează din capătul **sursă** (DELETE doar `HclSursaId == hclId`); `GET` partiționează sursă/țintă în memorie.

**Anexe** (extindere `DocumenteController`): document atașat la HCL via `hclId` → `TipDocument` forțat `Altele`, `TipDocumentHcl` devine sursa de adevăr. Anexă (`TipDocumentHcl.Anexa`) cere `NumarOrdinAnexa` (unic per HCL, imutabil când Semnat). Context exact-unul-din-trei: `SedintaId` XOR `PunctId` XOR `HclId` (rescriere `CK_Document_ExactUnContext`).

**Portal public** (`PublicHclController`, `/public/{slug}/hcl`): vizibilitate `EstePublicat && Status >= Numerotat`. HCL invalidat rămâne vizibil cu badge (vizibilitatea persistă — decizie juridică). `GET /` listă, `GET /{id}` detalii (expune semnatari nume+rol, vot, anexe publice, relații; NU comunicări sau internals de stocare), `GET /{id}/pdf` — variantă semnată prioritară (nume canonic `hcl-{numar}-{an}-semnat.pdf`) + fallback grațios la PDF generat. **Cache Redis DOAR la Semnat** (conținut înghețat): cheie `cleriq:hcl:pdf:{id}` sau `:inv` la invalidat; la Numerotat (conținut încă mutabil) generare proaspătă fără cache. Anexele HCL se descarcă prin `PublicDocumenteController` (branch `HclId` — anexa are Sedinta+Punct null).

## Portal public

**Rute `/public/{slug}/...`** — fără `[Authorize]`. `SlugTenantMiddleware` rezolvă slug → tenant și setează `HttpContext.Items["InstitutieId"]` ÎNAINTE de execuția acțiunii. Filtrul global aplică automat `Id == InstitutieIdCurenta`.

**Vizibilitate uniformă**: ședință accesibilă dacă `Status >= Convocata && Status != Anulata` (`Planificata` și `Anulata` NU expuse).

**Endpoint-uri publice**:
- `GET /public/{slug}` — metadate instituție (`PublicInstitutieDto`, fără StatusAbonament/CodSiruta/audit)
- `GET /public/{slug}/sedinte` — listă (fără ordine de zi)
- `GET /public/{slug}/sedinte/{id}` — detalii + ordine de zi
- `GET /public/{slug}/sedinte/{id}/voturi` — voturi cu tally + nominale (sau gol la secret)
- `GET /public/{slug}/sedinte/{id}/convocari` — listă cu `StatusGeneral` agregat (NU detalii per canal — intern)
- `GET /public/{slug}/sedinte/{id}/documente`, `GET /public/{slug}/puncte/{id}/documente`
- `GET /public/{slug}/documente/{id}` — descărcare cu dublă validare
- `GET /public/{slug}/sedinte/{id}/procesverbal` (+ `/markdown`, `/pdf`)
- `GET /public/{slug}/hcl`, `/hcl/{id}`, `/hcl/{id}/pdf` — listă/detalii/PDF HCL (vezi Modul HCL)

**PV public**: doar `Status = Finalizat`. PDF: varianta semnată are prioritate (nume canonic `proces-verbal-{data}-semnat.pdf`, NU numele fișierului încărcat). Lipsă pe disk → warning în log + degradare grațioasă la PDF generat (cetățeanul primește mereu un PDF).

**Cache PDF public** Redis (`cleriq:pv:pdf:{sedintaId}`, TTL 1h):
- Cache-uim DOAR generarea; verificările de vizibilitate rămân per-request — o ședință ascunsă/ștearsă nu mai servește PDF imediat
- Citire/scriere defensive cu try/catch → Redis indisponibil = warning + generare directă (NU 500 spre cetățean)

**Rate limiting global pe `/public/*`** (middleware nativ Microsoft.AspNetCore.RateLimiting):
- Partiționare: rute `/public/*` → partiție `publica` cu `FixedWindowLimiter`. Restul → `NoLimiter("interna")`
- Limită config: `RateLimiting:PublicRequesturiPeFereastra` (default 100), `RateLimiting:PublicFereastraSecunde` (default 10)
- Config Testing: 30/2s
- `OnRejected` setează header `Retry-After`
- Limita e GLOBALĂ pe partiție (zero pachete, fără ForwardedHeaders/topologie). Per-IP fin = reverse proxy la deployment (defense in depth — vezi `roadmap.md`)

**CORS whitelist strict** (`Cors:OriginiPermise` array în config). NICIODATĂ `AllowAnyOrigin`. FĂRĂ `AllowCredentials` (JWT pe header, nu cookies). Config lipsă → fail-closed (origini = `Array.Empty<string>()`).

## Redis

**Dependență de infrastructură obligatorie**, ca SQL Server. Fail-fast la pornire: dacă Redis nu răspunde, aplicația se oprește cu eroare clară. Aici stau cheile Data Protection — aplicația NU poate funcționa corect fără el.

**Citire din config** `ConnectionStrings:Redis`, `ConnectionMultiplexer.Connect` singleton. **InstanceName** = `"cleriq:"` — namespacing curat.

**Chei active**:
- **Cu TTL** (sigur la evicție):
  - `cleriq:tenant:slug:{slug}` — SlugTenantMiddleware cache
  - `cleriq:pv:pdf:{sedintaId}` — PDF PV public (1h)
  - `cleriq:hcl:pdf:{hclId}` (+ `:inv`) — PDF HCL public, DOAR la Semnat (1h)
  - `cleriq:lock:{...}` — locks distribuite workeri
- **Fără TTL** (critic):
  - `cleriq:dataprotection-keys` — listă chei Data Protection (parolele SMTP criptate)

**Producție: policy de evicție OBLIGATORIE `volatile-*`**. `allkeys-*` ar evacua cheile DP fără TTL = parolele SMTP pierdute ireversibil. Backup producție = include datele Redis (AOF activat) — vezi `setup.md` pentru detalii operaționale.

**`ILacatDistribuit`** (`LacatDistribuitRedis`):
- SET NX cu token Guid + TTL mărginit (fără renewal — durată fixă acoperă operațiile)
- Script Lua compare-and-delete la eliberare (releases doar dacă tokenul nostru)
- `await using` ca pattern `IConexiuneEmail`
- Eșec dobândire = ratare (amânare > risc duplicate). Eșec Redis tratat ca ne-dobândit (worker amână, NU intră pe cale fără siguranță).
- **`ReloadAsync` SUB lacăt** ÎNAINTEA efectelor în lumea reală (citim starea curentă după ce ne-am asigurat că nimeni altcineva nu lucrează pe ea).

**Citirile publice degradează grațios la DB** (try/catch + warning). Doar dependențele critice la pornire fail-fast.

## Mentenanță

**`MentenantaController`** — `[Authorize(Roles = "SuperAdmin")]` (mentenanța = cross-tenant).

**Două endpoint-uri per stocare** (Documente, Audio):
- `GET /Mentenanta/Orfani{Documente|Audio}` — preview read-only
- `POST /Mentenanta/Orfani{Documente|Audio}` — ștergere fizică (REST canonic, NU parametru `?confirma`)

Query: `?zile=N` (default 90, **min 30** — gardă defensivă).

**Două categorii orfan**:
- `FaraRandInDb` — fișier pe disk, fără rând în DB
- `SoftDeletedVechi` — DB row soft-deleted cu `StersLa < (acum - zile)`

**Gardă universală anti-race**: fișiere cu `LastWriteTime > (acum - 1h)` sunt PROTEJATE indiferent de categorie. Acoperă fereastra de ~ms între `SalveazaAsync` și `SaveChangesAsync` la upload. Const `VarstaMinimaFisier = 1h`.

**`IStocare*.EnumereazaToateAsync`** = abstracție obligatorie pe orice stocare. Migrare cloud drop-in.

**Pattern critic**: orice tabelă care referențiază fișiere din `IStocareDocumente`/`IStocareAudio` TREBUIE inclusă în dicționarele de scan din `MentenantaController` — altfel cleanup-ul le șterge fizic ca orfane. Curent: `Documente.CaleStocare`, `ProceseVerbale.CaleStocareSemnat`, `Hcluri.CaleStocareSemnat` (toate trei din aceeași stocare partajată!), `Transcrieri.CaleStocareAudio`.

**Logging structured cu `ILogger`** pe fiecare ștergere — audit pentru SuperAdmin.

## Răspunsuri eroare la client

- `Ok` cu DTO pentru succes
- `BadRequest` cu mesaj string SAU `Errors` din Identity SAU `ValidationProblemDetails` (model binding)
- `NotFound` cu mesaj scurt pentru resursă inexistentă
- `Conflict` cu mesaj pentru duplicate/tranziții invalide/gărzi business
- `NoContent` pentru DELETE
- `Unauthorized` cu mesaj scurt pentru auth eșuat
- Erori Identity = array de `{description}` — frontend-ul are helper `extrageMesajEroare` care acoperă toate formele (vezi `frontend.md`)

## Seed

`SeedComun` (roluri + SuperAdmin) rulează mereu, fail-fast la `SuperAdmin:Email/Parola` lipsă. `SeedDevelopment` rulează doar pe `IsDevelopment()`, idempotență pe slug (slug-urile soft-deleted sunt "arse" — NU se recreează). Date noi de dev se adaugă în `SeedDevelopment`, NU manual în SSMS.

Telefoanele consilierilor seedați = `null` (Twilio real în user-secrets ar trimite SMS unui străin).

## Teste

Suită xUnit + WebApplicationFactory, **262 teste verzi**. Izolare prin multi-tenancy (instituție proprie per test cu slug Guid), NU prin curățare DB între teste. DB de test = SQL Server real `CleriqTest` (NU InMemory — traduce filtered indexes, RowVersion, check constraints, filtre globale). `public partial class Program { }` la finalul Program.cs (necesar pentru `WebApplicationFactory` cu top-level statements).

Convențiile fixture, patterns de aranjare prin DbTest, izolarea, rate limiting test, helper-i sunt în **`teste.md` — atașat când adăugăm/debug teste**.

**Regulă**: orice modificare arhitecturală (entitate nouă, schimbare matrice gărzi, schimbare DTO, schimbare claim JWT) poate sparge teste — discutăm impactul înainte de implementare.

## Migrații cumulative

Lista migrațiilor aplicate (în ordine):
`AddFkInstitutieRestrict`, `AddTipVotPunctOrdineZi`, `AddConsilierIdUtilizator`, `AddContinutConvocare`, `AddDocument`, `AddIncercareTrimitere`, `AddSmtpConfigInstitutie`, `AddTranscriere`, `AddHotwordsTranscriere`, `DropHotwordsFolosite`, `AddPvSemnat`, `RenameNumarIncercariToNumarEsecuri`, `AddRefreshToken`, `AddPublishTranscriere`, `AddAprobarePv`, `AddTrasabilitateFunctii`, `AddModulHCL` (cele 5 câmpuri signed-PDF HCL incluse aici, fără migrație separată).

## Legături la fișiere on-demand

- **`whisper.md`** — contract HTTP wrapper learnedmachine, pod Docker custom (Caddy + IP allowlist + Bearer + rate limit), decizia model `large-v2` + prompt scurt + lecții acustice. Atașat când atingem fluxul transcriere.
- **`teste.md`** — convenții xUnit + fixture, patterns aranjare prin API, izolare prin multi-tenancy, rate limiting test. Atașat când adăugăm/debug teste.
- **`setup.md`** — Docker Redis, SQL Server local, user-secrets dev, env vars prod, credențiale seed, comenzi reproducere mediu. Atașat când lucrăm la dev environment sau deployment.
- **`roadmap.md`** — probleme deschise + roadmap detaliat (Etapa 1 PV semnat, modul ActNormativ, Management conturi, Deployment, Faza 3). Atașat când planificăm următoarea fereastră de lucru.
