Rezumat sesiune S57 — Implementare concluzii research juridic Modul A HCL (Î2 motive de invalidare + Î1 anulare MOL / înghețare / audit)

## Context
Input: research-ul juridic deja făcut (`docs/research/research_hcl.md`, `docs/research/research_MotivInvalidare.md`)
pe cele 2 întrebări ridicate la testarea manuală S56. Sesiune de **dezvoltare**, nu testare.
Am lucrat **o întrebare pe rând** (Î2 mică întâi, apoi Î1), pas 0 backend cu teste verzi, FE = oglindă
strictă a backendului, migrație EF la fiecare schimbare de schemă. La fiecare întrebare am cerut și o
**recenzie încrucișată** cu „Claude din sesiunea precedentă"; completările lor (date existente, pragul
exact de intangibilitate) sunt integrate mai jos.

## Î2 — Motive de invalidare (livrat, 270 teste verzi)
**Decizii:**
- **Scos `AnulatPrefect` (valoarea 1) curat**, NU păstrat ca „deprecated". Eroare juridică certă:
  prefectul *atacă*, instanța *anulează* (Constituție art. 123(5), Cod adm. art. 255). Empiric: **0 rânduri**
  foloseau valoarea 1 (verificat în DB). 3 plase de siguranță: gardă `Enum.IsDefined` pe API, ramură
  `default` în eticheta FE (anti-`undefined`), notă documentată în migrație.
- **Adăugat `Caduc`(5), `Inexistent`(6), `Altul`(7)**; valorile 2/3/4 (AnulatInstanta/AbrogatHclUlterior/
  Retractat) păstrate stabile (se stochează ca int).
- **Câmp text liber `MotivInvalidareAltulText`** — obligatoriu DOAR când motiv == Altul; ignorat (null)
  pentru orice alt motiv.
- **Amânat** (per research): sub-câmpul `reclamant` la AnulatInstanta (Etapa 3, regulile ÎCCJ 74/2023) +
  derivarea automată a `AbrogatHclUlterior` din relația de abrogare.

**Fișiere:** `Enums.cs`, `Hcl.cs` (+ migrație `AddMotivInvalidareAltulText`), `HclDto.cs`
(InvalidareHclDto + HclDetaliiDto), `HclController.Invalidare` (gardă + validare Altul; `AnuleazaInvalidare`
resetează câmpul), `MapareHcl`, cele 2 switch-uri de etichetă (`ExtensiiEnumuri.cs` + bannerul „ACT
INVALIDAT" din `GeneratorHcl.cs` — Altul afișează textul liber). FE: `enums.ts`, `etichete.ts`,
`hcl.models.ts`, `invalidare-dialog` (listă nouă + câmp condiționat + copy intro neutru pt. caduc/inexistent),
`hcl-detalii.html` (rând nou „Invalidare" în tab Detalii — înainte era doar badge). Teste: 4 noi + 1
re-folosit în `TesteInvalidareHcl.cs`.

## Î1 — „Anulează MOL" + înghețare latching + audit + DateOnly (livrat, 276 teste verzi)
**Decizii (cu rafinarea din recenzia încrucișată):**
- **Înghețare „latching": flag `AIntratInCircuit`** (bool, set o singură dată la **prima publicare MOL
  SAU prima comunicare la prefect**, niciodată resetat) conduce înghețarea variantei semnate
  (Înlocuiește/Șterge). **Decuplează înghețarea conținutului de starea MOL** → portița se închide
  indiferent de prag. (Rafinare peste planul inițial „blochează doar după comunicare", care era prea
  permisiv pentru actele normative — research: blochează la *cel mai timpuriu* moment {MOL, comunicare}.)
- **„Anulează MOL" = doar metadată**: șterge data MOL, gated pe **Admin + fără comunicare la prefect +
  motiv obligatoriu + rând de audit imuabil**. NU resetează latch-ul → nu redeschide documentul. Scopul
  real: corecția datei publicării / retragerea unei publicări greșite **înainte** de comunicarea la prefect,
  apoi republicare corectă.
- **Audit = tabel nou `IstoricActiuneHcl`** (tenant-scoped, append-only). *cine/când* vin gratis din
  `EntitateDeBaza` (CreatDe/CreatLa); în plus `Tip` (enum `TipActiuneHcl`), `Motiv`, `AdresaIp`.
  Imutabil prin **lipsa oricărui endpoint de mutație**; se soft-cascade cu HCL-ul (FK Restrict).
- **Mesajul 409** descrie mecanismul legal („act de îndreptare/erată, conform Legea 24/2000"), nu un
  buton inexistent.
- **Backfill în migrație**: actele deja în circuit (MOL setat sau comunicare existentă) primesc latch-ul
  retroactiv — HCL **5/2026** a fost actualizat la `AIntratInCircuit=1`.
- **`DataPublicareMol` → `DateOnly`** (pas separat, după partea legală): repară shift-ul de fus din S56.
  Scos `inputLocalLaUtcIso` + hint-ul „fus orar instituție" din `publicare-mol-dialog`.

**Fișiere backend:** `Hcl.cs` (AIntratInCircuit, DataPublicareMol→DateOnly), `IstoricActiuneHcl.cs` (nou),
`Enums.cs` (TipActiuneHcl), `HclController` (PublicareMol setează latch; gărzile IncarcaSemnat/StergeSemnat
pe latch; `AnuleazaPublicareMol` rescris: `[FromBody] AnulareMolDto` + gardă comunicare 409 + motiv +
audit), `ComunicariHclPrefectController.Adauga` (setează latch), `AppDbContext` (DbSet + FK Restrict +
soft-cascade), DTO-uri (`HclDto`, `PublicHclDto`). FE: `hcl.permisiuni.ts` (Înlocuiește/Șterge → `!aIntratInCircuit`;
`poateAnulaMol` → `Admin && areMol && !areComunicari`), `anulare-mol-dialog` (nou, motiv obligatoriu),
`hcl-detalii` (deschide dialogul), `publicare-mol-dialog` (fără conversie de fus), `hcl.models.ts`,
`hcl.service.ts` (`anuleazaMol(id, motiv)` cu body). Teste: `TesteAnulareMolHcl.cs` (6 noi) + actualizări
în `TesteHcl.cs` + helperi în `ExtensiiTeste.cs` (`PublicaMolAsync`, `AnuleazaMolAsync` cu DELETE+body,
`AdaugaComunicarePrefectAsync`).

Test cheie verde: `AnulareMol_NuDezgheataVariantaSemnata_409` — publică MOL → atașează scan → anulează
MOL → Înlocuiește/Șterge rămân **409**. Portița dovedită închisă în cod.

## Verificare (rulat de Claude)
- **Backend: 276/276 teste verzi** (270 după Î2 + 6 noi la Î1). Migrațiile sunt exersate de fixture
  (`Database.MigrateAsync` recreează schema).
- **FE: lint + build de producție curate** la ambele întrebări.
- 3 migrații aplicate pe DB-ul de dev (inclusiv backfill-ul + conversia DateOnly).

## Probleme / de rezolvat pe viitor
1. **Test-schelet FE pre-existent picat:** `app.spec.ts` („should render title" → caută `<h1>` cu
   „Hello, cleriq-web") — testul implicit Angular CLI, niciodată actualizat. Pică la `npm test`.
   **Neatins de noi** (nu e în changeset). De reparat/șters separat. (Restul suitei Vitest e verde.)
2. **Jurnalul de audit nu e încă citibil:** `IstoricActiuneHcl` se captează imutabil, dar NU există
   endpoint GET → nu e vizibil în UI și nu e direct asertabil în teste (regula: verificare doar prin API).
   Viitor: endpoint de citire + afișare (ex. tab „Istoric" pe HCL) + test.
3. **„Fereastra fără erată" (decizie conștientă):** până la **Faza 7**, o eroare de *conținut* pe un act
   publicat nu are corecție în aplicație (am scos doar shortcut-ul ilegal). Acceptat pre-pilot;
   Faza 7 (Erată unificată) o închide corect.
4. **De confirmat cu un avocat (din research, dus mai departe):**
   - `Inexistent` ca *invalidare* vs. *stare anterioară intrării în vigoare* (un act inexistent n-a fost
     niciodată în vigoare).
   - Aplicarea eratei la HCL **individuale** (Legea 24/2000 art. 71 vizează expres actele normative).
   - Pragul exact de intangibilitate — am folosit „cel mai timpuriu moment {MOL, comunicare}" per
     recomandarea de siguranță din research.
5. **Valoarea veche `DataPublicareMol`:** conversia DateOnly trunchiază ora; valoarea shiftată pre-existentă
   pe 5/2026 (din S56) NU se „de-shiftează", doar se trunchiază. Date de test → nu merită reparat;
   publicările noi sunt corecte.

## Clarificat în sesiune (concepte + decizie de roadmap)
- **Scopul lui „Anulează MOL"**: corectarea metadatei de publicare (mai ales **data**) / retragerea unei
  publicări greșite **înainte** de comunicarea la prefect → apoi republicare corectă. NU pentru schimbarea
  conținutului. După anulare, „Publică în MOL" reapare în meniul ⋮.
- **Mecanismul eratei** (explicat userului): actul original **nu se modifică niciodată**; se naște un
  **al doilea act**, cu propriul număr + propriul ciclu (Draft→…→Publicat) + propriul PDF semnat, **legat**
  de original printr-o relație; originalul rămâne intact în MOL cu un marcaj „rectificat prin X"; ambele
  se văd împreună. **Fundația există deja** (relații Modifică/Abrogă/Completează + ciclul de viață +
  înghețarea de azi); fluxul dedicat de erată = **Faza 7**.
- **Două nivele de eroare**: (a) materială/typo → notă de rectificare/erată (art. 71, ușor); (b) de fond →
  act nou modificator/abrogator cu procedură + vot nou (parțial posibil deja azi via relații).
- **Ordinea roadmap păstrată:** următoarea = **Faza 4 (Dispoziții primar)**, apoi 5, 6, 7. Motiv: Faza 7
  („Erată unificată") acoperă PV + HCL + **Dispoziții**; dispozițiile = Faza 4, deci 7 înainte de 4 ar
  însemna pattern construit pentru 2 din 3 + refacere. Regulă fermă: **Faza 7 după Faza 4**.

## Note de mediu / unelte (pentru sesiunea următoare)
- **`dotnet-ef` global actualizat 8.0.6 → 10.0.9** (versiunea veche nu putea migra un proiect EF Core 10).
  Relevant dacă apare confuzie la migrații în sesiunile viitoare.
- **Migrațiile se aplică automat DOAR în Production** (`Program.cs`, gardă `IsProduction`). În Development
  rulează manual: `dotnet ef database update --project Cleriq --startup-project Cleriq`.
- **Build/teste cer backend-ul oprit** (lock pe `Cleriq.exe`). În S57 am oprit procesul userului ca să
  build-uiesc/migrez/testez; se repornește oricum pentru schema+enum noi.
- **3 migrații noi:** `20260630114723_AddMotivInvalidareAltulText`,
  `20260630121638_AddIstoricActiuneHclSiCircuit` (cu backfill), `20260630123015_ConvertDataPublicareMolDateOnly`.
- **Stare date dev după testarea manuală S57** (tenant Slobozia):
  - **5/2026**: neschimbat (doar observat read-only) — Semnat, MOL, comunicat, `AIntratInCircuit=1`, fișier semnat.
  - **6/2026**: invalidat apoi anulată invalidarea → **curat** (câmpurile de invalidare nule).
  - **7/2026**: dus prin atașare scan → publică MOL → anulează MOL în test → a rămas **`AIntratInCircuit=1`,
    fișier semnat atașat (`hcl-7-2026.pdf`), `DataPublicareMol=null`, + 1 rând în `IstoricActiuniHcl`**
    (AnulareMol). **Reset DB la starea inițială oferit** (set `AIntratInCircuit=0`, șterge fișierul + audit)
    — de aplicat la cerere; altfel sesiunea următoare pornește cu această stare.
  - id=4 → Draft (neschimbat).

## Următoarea sesiune
**Faza 4 — Modul C: Dispoziții primar** (`docs/roadmap.md`). Pattern similar Modulului A (refolosește
experiența HCL proaspătă): normative (se publică) vs. individuale (acte de personal, nu se publică),
numerotare separată de HCL, + dispoziția de convocare ca act explicit. Citește planul fazei înainte de cod.
