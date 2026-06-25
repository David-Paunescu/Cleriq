# Cleriq — Teste

**84 teste verzi** în proiectul `Cleriq.Tests`. Suită xUnit + WebApplicationFactory.

## Stack teste

- **xUnit 2.9.3** — NU v3 (incompatibilități subtile, neexplorat)
- **Microsoft.AspNetCore.Mvc.Testing** + **Microsoft.NET.Test.Sdk**
- **FĂRĂ FluentAssertions** (licență plătită începând cu v8) — folosim assert-urile xUnit native
- **Global using Xunit** într-un singur fișier

**DB de test = SQL Server real `CleriqTest`** (NU InMemory/SQLite). Motivul: traducerea filtered indexes, RowVersion, check constraints, filtre globale. InMemory ar trece teste false pozitive pe lucruri care în prod nu funcționează.

**Redis index 15** = teste, golit per rulare prin fixture.

## Precondiție obligatorie în `Program.cs`

```csharp
public partial class Program { }
```

la finalul fișierului. Necesar pentru `WebApplicationFactory` cu top-level statements (altfel `Program` nu e accesibil din proiectul de teste).

## Convenții obligatorii

**Orice clasă de teste**:
```csharp
[Collection("Cleriq")]
public class TesteX
{
    private readonly CleriqFixture _fixture;
    public TesteX(CleriqFixture fixture) => _fixture = fixture;
}
```

**NICIODATĂ factory propriu per clasă**. Toate testele partajează un singur `CleriqFixture` prin Collection.

**`parallelizeTestCollections: false`** în `xunit.runner.json`. Testele se rulează secvențial — necesar pentru izolarea prin multi-tenancy + DB real partajată.

**Comentarii minime în cod** (cerință explicită user). Explicațiile arhitecturale rămân în chat.

## Izolare prin multi-tenancy

Izolare = **instituție proprie per test cu slug `Guid.NewGuid()`**, NU curățare DB între teste. Exersează constant calea reală de onboarding (Provisioning → login Admin → operații).

Pattern tipic:
```csharp
var slug = $"test-{Guid.NewGuid():N}";
await _fixture.ProvisioneazaInstitutieAsync(slug, ...);
var clientAdmin = await _fixture.AutentificaAdminAsync(slug);
// teste pe clientAdmin — toate datele sunt izolate per InstitutieId
```

Date orfane din rulări anterioare nu interferează (filtru global tenant).

## DbTest = aranjare inaccesibilă prin API

**Regulă strictă**: `DbTest` (helper de acces direct la DB în teste) e EXCLUSIV pentru **aranjarea** stării pe care API-ul nu permite să o setez (ex: `FolositLa` pe `RefreshToken`, `StersLa` în trecut, statusuri în mijlocul unui flux).

**Verificările NUMAI prin API**. Niciodată `DbTest.GetX(...).Assert(...)` — exercită calea reală observabilă de client.

**Setteri DbTest care oglindesc servicii**:
- `HashRefreshToken` — copia exactă a hash-ului din `ServiciuRefreshTokens` (SHA-256 hex lowercase pe UTF-8). **Schimbarea algoritmului în serviciu trebuie reflectată aici** — altfel toate testele de refresh devin invalide silent.
- Aranjarea `Trimisa`/`Esuata` pe `Convocare` setează ȘI `TrimisLa`/`Detalii` — oglindește exact ce face `WorkerConvocari`. Aranjare incompletă = test minciună.

**`DbTest.SeteazaStatusSedintaAsync(sedintaId, status)` (s45)**: forțează status pe ședință când API-ul nu expune tranziția directă (ex: `Convocata` vine prin Convocare, nu prin SchimbaStatus).

**Pattern Secretar inline în teste**: nu există `_factory.ClientSecretarAsync`. Cale: `admin.PostAsJsonAsync("/api/Auth/register", {email, parola, numeComplet, rol: "Secretar"})` → assert IsSuccessStatusCode → `_factory.ClientAutentificatAsync(email, parola)`. Email cu Guid pentru unicitate cross-test.

## Config de test prin env vars

**Limitarea minimal hosting + top-level statements**: `Program.cs` citește config inline (ex: `builder.Configuration["Jwt:Key"]`) ÎNAINTE ca `ConfigureAppConfiguration` din factory să se aplice.

Concluzie: **config de test exclusiv prin env vars** setate în constructorul `CleriqFixture` (sau în `WebApplicationFactory.ConfigureWebHost` cu `Environment.SetEnvironmentVariable`).

**Regulă obligatorie**: orice cheie nouă citită inline în `Program.cs` → adăugată în `SeteazaVariabileDeMediu`. Drift = teste care eșuează cu erori derutante (NullReference pe config).

## Asertare orice pas de setup

Eșecul unui setup downstream apare derutant ca 404 / 401 / 500. Construire defensivă:

```csharp
var raspunsCreare = await client.PostAsJsonAsync(...);
Assert.True(raspunsCreare.IsSuccessStatusCode, await raspunsCreare.Content.ReadAsStringAsync());

var raspunsOperatie = await client.PostAsJsonAsync(...);
// Aici știu sigur că setup-ul a reușit
```

Mesajul de eroare la setup eșuat trebuie să fie util — `await Content.ReadAsStringAsync()` în assert-ul de setup.

## Teste de mentenanță (cleanup fișiere orfane)

**UN singur test per tip de stocare** (Documente, Audio). Două teste pe aceeași stocare = primul mătură orfanii celuilalt.

**Îmbătrânirea artificială fișiere = AMBELE timestamp-uri**:
```csharp
File.SetCreationTimeUtc(path, acum.AddDays(-100));
File.SetLastWriteTimeUtc(path, acum.AddDays(-100));
```

Controllerul ia `Max(LastWriteTime, CreationTime)` la garda anti-race de 1h.

**`TestData/*` resetat per rulare**. Orice test cu stare pe disk = fișiere construite în aceeași rulare. Niciun test nu depinde de stare reziduală dintr-o rulare anterioară.

## Teste rate limiting

**Config Testing**: limită 30 cereri / fereastră 2s (vs prod 100/10s). Setat prin env vars în fixture.

**Dimensionarea limitei** se face față de consumatorul legitim cel mai agresiv al suitei (ex: `TestePortalPublic` face ~6 GET în <400ms), NU doar față de testul care exercită limita. Limita prea mică = altele pică colateral.

**Testele asertează COMPORTAMENT**, NU numărătoare exactă:
- Există un punct de respingere (primește 429)
- Header `Retry-After` setat
- Fereastra nouă permite cereri din nou

NU `Assert.Equal(30, succese)` — fragil la timing.

**`AsteaptaFereastraNouaAsync` la ÎNCEPUT ȘI FINAL** în testele care consumă fereastra. Pornire curată + curățare pentru testul următor.

**Flaky cunoscut**: `TesteRateLimiting.RutePublice_PesteLimita_429CuRetryAfter_ApoiFereastraNouaPermite` e sensibil la timing (GC/build paralel întârzie un milisecund critic). Re-rulat izolat → verde. **Non-blocant**. Soluție viitoare la sesiune polish CI: mărire marjă în `AsteaptaFereastraNouaAsync` sau retry intern în test.

## Teste de contract pe integrări externe

Pentru integrările black-box (Whisper wrapper, Twilio): **unit pur cu `HandlerHttpFals`**, FĂRĂ fixture, FĂRĂ DB.

```csharp
var handler = new HandlerHttpFals(raspuns => { /* asertare cerere, returnare răspuns */ });
var client = new HttpClient(handler);
var serviciu = new TranscriereWhisperWrapper(client, ...);
```

Asertăm:
- Forma exactă a cererii HTTP (headere, body multipart, query)
- Comportamentul la diverse răspunsuri (200 / 4xx / 5xx / timeout)

Bug-ul Form-vs-Query din s25 (wrapper Whisper) ar fi fost prins instant cu un test de contract proper. Notat ca lacună în `roadmap.md`.

## Cache PDF — egalitate byte-cu-byte

Pentru verificarea că GET-ul pe portal public servește din cache (NU regenerează):

```csharp
var pdf1 = await raspuns1.Content.ReadAsByteArrayAsync();
var pdf2 = await raspuns2.Content.ReadAsByteArrayAsync();
Assert.Equal(pdf1, pdf2);  // egalitate byte-cu-byte
```

Regenerarea QuestPDF diferă prin metadatele PDF (timestamp creare în XMP) — egalitate byte exactă e dovada certă a cache-ului. Tehnică elegantă fără mock-uri sau spy-uri.

## Capcane mediu

**Hot Reload VS NU aplică**:
- Modificări de constructor
- Adăugare/eliminare câmpuri de clasă
- Schimbare DI

Pentru orice modificare structurală: **Stop → Rebuild → Start**. Altfel testele rulează cu binarul vechi (rezultate confuze).

**CI Linux**: testele de mentenanță depind de `File.SetCreationTimeUtc`. **Pe Linux birth time nu e setabil** → garda 1h pentru fișiere recente protejează toți orfanii artificiali → testele pică.

CI Linux = sesiune dedicată pre-deployment (ajustare teste sau `[SkippableFact]` cu condiționare per OS).

## Layout fișiere teste

```
Cleriq.Tests/
├── CleriqFixture.cs          # singletonul de Collection, configurare WAF
├── DbTest.cs                  # helper-i aranjare DB inaccesibilă prin API
├── HandlerHttpFals.cs         # mock HTTP handler pentru contract tests
├── TesteAuth.cs               # autentificare, register, provisioning
├── TesteRefreshTokens.cs      # 7 teste rotație/familii/detecție furt
├── TesteIzolareTenant.cs      # cross-tenant 404
├── TesteCvorum.cs             # majoritate Simpla/Absoluta/Calificata OUG 57
├── TesteVotSecret.cs          # anonimizare la citire + gărzi
├── TesteVotulMeu.cs           # privacy claim ConsilierId (s37)
├── TesteConvocare.cs          # outbox + retry + IncercareTrimitere
├── TesteProcesVerbal.cs       # gărzi PV + PV semnat
├── TesteTranscriere.cs        # upload + retry + gărzi
├── TestePublishTranscriere.cs # 5 teste publish flow snapshot (s40)
├── TestePortalPublic.cs       # vizibilitate + slug + cache PDF
├── TesteMentenanta.cs         # orfani Documente + Audio
├── TesteRateLimiting.cs       # global + flaky cunoscut
└── TesteContractWhisper.cs    # unit pur HandlerHttpFals
```

## Migrații pe CleriqTest

Aplicate automat la pornire fixture prin `db.Database.Migrate()`. Nu e nevoie de `Update-Database` manual.

## Reguli de aur (recapitulare)

1. **`[Collection("Cleriq")]`** sau nu există
2. **Verificările prin API**, niciodată DbTest
3. **Izolare prin tenant nou cu slug Guid**, niciodată curățare DB
4. **Config prin env vars în fixture**, nu prin `ConfigureAppConfiguration` pentru chei citite inline în Program.cs
5. **Asertă fiecare pas de setup** cu mesaj util
6. **Aranjare oglindește semantica reală a serviciilor** (Trimisa + TrimisLa, hash-ul refresh token)
7. **Asertă COMPORTAMENT pentru rate limiting**, nu numărătoare exactă
8. **Un singur test per tip de stocare** la mentenanță
9. **Stop + Rebuild + Start** după modificări structurale (Hot Reload nu prinde)
