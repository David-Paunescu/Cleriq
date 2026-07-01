Rezumat sesiune S59 — Faza 4 (Modul C: Dispoziții primar), backend: Faza C–D parțial (Pașii 5–8)

## Context
Sesiune de **dezvoltare**, continuare directă după S58 (fundația: generalizări + strat de date, 276 teste
verzi, commit-uite). Am implementat, pas cu pas cu teste verzi la fiecare sub-pas, **Pașii 5–8** din
breakdown-ul planului `docs/planuri/plan_faza4_backend.md`. Input juridic: `docs/research/research_dispozitii.md`.
Pornire: **276 verzi**. Final: **312 verzi** (+36 teste noi). **Zero migrații** în toată sesiunea (toate câmpurile
existau din Pas 4; interfețele noi sunt doar contracte peste proprietăți existente). Nimic comis (userul comite
la final).

## Implementare

### Pas 5 — Creează + conținut + numerotare (sub-împărțit 5a + 5b)

**5a — Creează + generator de conținut** (am pliat `EditeazaContinut`/`RegenereazaContinut` aici, sunt minuscule
și țin de același ciclu):
- `DTOs/DispozitieDto.cs`: `CreareDispozitieDto(TipDispozitie, Titlu, DataEmitere, EmitentConsilierId?)`,
  `DispozitieDto` (slim, pt. `Lista`), `DispozitieDetaliiDto`, `SemnatarDispozitieDto`. DTO-ul de detalii include
  de la început câmpurile de contrasemnătură refuzată (ca să nu-l reformez la Pas 6).
- `Services/GeneratorDispozitie.cs` + interfață: type-aware — antet „PRIMARUL …", temei art. 155 + art. 196
  alin. (1) lit. b), formula „DISPUNE:", **fără** vot/cvorum; semnături în markdown (`PRIMAR` / `p. PRIMAR` +
  `Viceprimar` la înlocuitor / `Contrasemnează pentru legalitate, SECRETAR GENERAL`); baner de invalidare cu
  formulare proprie dispoziției (Retractat → „revocată de primar"). Ramura de refuz din generator a fost scrisă
  aici, testată abia la Pas 6.
- `Helpers/MapareDispozitie.cs`: `CuIncludeComplet` (AsNoTracking + semnatari) + `SpreDetaliiDto`.
- `Controllers/DispozitiiController.cs`: `Lista`, `Detalii`, `Creeaza`, `EditeazaContinut`, `RegenereazaContinut`.
  `Creeaza` derivă emitentul din `CinEPrimarulLa(DataEmitere)` (400 dacă null) **sau** override `emitentConsilierId`
  (viceprimar înlocuitor, sare peste primar) + secretarul din `CinESecretarulUatLa` (**400 conștient** dacă null),
  creează cei 2 semnatari, apoi scaffold `Continut` cu generatorul. `Creeaza` întoarce `DispozitieDetaliiDto`
  (nu slim — util pt. FE + testabil direct).
- DI: `IGeneratorDispozitie`. Helperi de test: `AsigurareMandatPrimarAsync/SecretarAsync`, `CreeazaDispozitieAsync`.
- **9 teste**: derivare + forma conținutului, 400 fără primar/secretar/titlu, override „p. PRIMAR", emitent
  inexistent→400, editare/regenerare (blocat pe Semnat prin `DbTest`), `Lista` cu filtru, izolare tenant.

**Decizii de cod:**
- **Numerotare: injectez `IServiciuNumerotareActe` DIRECT cu `T=Dispozitie`, fără fațadă.** Fațada
  `IServiciuNumerotareHcl` există doar ca shim de compatibilitate pentru codul HCL vechi (S58); codul nou n-are
  nevoie de un wrapper în plus.
- **`DataEmitere` (input `DateOnly`) → stocat la prânz UTC** (`dto.DataEmitere.ToDateTime(12:00, Utc)`). Numerotarea
  calculează anul prin `.LaFusOrar(fus).Year`; prânzul UTC dă un tampon de 12h → anul de registru rămâne corect
  pentru fusul RO (UTC+2/+3). Nu există helper invers local→UTC și nici nu e nevoie.

**5b — Numerotare**: `AtribuieNumar` + `SugestieNumar` prin `IServiciuNumerotareActe.…<Dispozitie>`. Wart-ul de
enum mapat în controller: `TipRezultatAtribuire.HclInexistent` → **404 „Dispoziția nu există."**;
`StareInvalidaHcl` → **409** (mesajul serviciului e deja act-neutru „Actul în stare …"). Reutilizat
`SugestieNumarDto`. **7 teste** paritar `TesteNumerotareHcl` (Draft→Numerotat + înlocuire placeholder, nr.0→400,
deja numerotat→409, lacune, nr. activ luat→409+sugestie, nr. ars→409+sugestie, `SugestieNumar`).

### Pas 6 — Semnare + contrasemnătură refuzată
- `Semneaza` (paritar HCL): doar `Numerotat`→`Semnat`; gardă = exact 1 Emitent + (1 SecretarContrasemnatura activ
  **SAU** `ContrasemnaturaRefuzata` cu obiecție motivată). Semnatarii soft-șterși nu se numără (filtru global).
- `RefuzContrasemnare` (nou): cere obiecție motivată (≤2000), o consemnează (autor + dată), apoi **soft-șterge
  rândul de secretar** (`_context.SemnatariDispozitie.Remove(secretar)`) — eliberează filtered-unique-ul și forțează
  garda pe ramura de refuz (altfel l-ar număra drept contrasemnat). Blocat pe Semnat; refuz dublu→409.
- Cele 3 cazuri din plan: (1) contrasemnătură normală, (2) refuz + emite peste refuz, (3) substitut consilier-juridic
  = **amânat** (corect, neconstruit). **6 teste.**

### Pas 7 — Variantă semnată + Mentenanță (sub-împărțit 7a + 7b)

**7a — extras helper file-handling (rule-of-three, a 3-a folosire PV+HCL+Disp):**
- `Models/IActCuVariantaSemnata.cs`: contract cu cele 5 câmpuri (`CaleStocareSemnat`, `NumeFisierSemnat`,
  `MarimeSemnat`, `HashSha256Semnat`, `DataIncarcareSemnat`), implementat de `Hcl`, `ProcesVerbal`, `Dispozitie`
  (toate le aveau deja identic — **fără migrație**).
- `Helpers/VariantaSemnata.cs`: `ValideazaPdf` / `StocheazaAsync` (stochează + hash, întoarce calea veche) /
  `StergeVecheAsync` (replace post-commit, best-effort) / `Curata` (DELETE).
- **Refactor comportament-neschimbat**: `HclController` + `ProcesVerbalController` folosesc helper-ul; **gărzile de
  freeze rămân inline** în fiecare (diverg: HCL/Disp pe `AIntratInCircuit`, PV pe `Status/DataAprobare`). Suitele
  HCL+PV au rămas verzi.
- Nou pe Dispoziție: `POST/GET/DELETE {id}/Semnat` + injectat `IStocareDocumente`. Gărzi inline paritar HCL: doar
  pe `Semnat`, înghețat după `AIntratInCircuit` (prima atașare post-circuit permisă = varianta B). **3 teste.**

**7b — Mentenanță (CRITIC):** adăugat `Dispozitii.CaleStocareSemnat` la dicționarul scanului de orfani din
`MentenantaController.CalculeazaOrfaniDocumenteAsync` (lângă Documente/PV/HCL). Fără asta, cleanup-ul ar șterge
fizic PDF-urile de personal semnate. Per **regula 8** (un test per tip de stocare), am **extins testul de Documente
existent** (`OrfaniDocumente_…_SiActeSemnateProtejate`) cu o dispoziție semnată, în loc de un al doilea test redundant.

### Pas 8 — Invalidare + revocare + `EtichetaDispozitie` + Delete
- `Invalidare` (paritar HCL, fără relații): `Enum.IsDefined` + „Altul" cu text obligatoriu (≤300) + 409 dacă deja
  invalidată. **Regula de revocare** (`Retractat`): Normativ oricând; Individual doar înainte de `AIntratInCircuit`;
  Individual în circuit → **409** (trimite la „Anulat de instanță", art. 1 alin. (6) L554/2004). Regula e specifică
  DOAR revocării proprii — `AnulatInstanta` rămâne mereu posibil.
- `AnuleazaInvalidare` (Admin): revine la valid.
- `Sterge` — matrice de gărzi cu early-return: invalidat→OK (override, chiar și pe semnat); semnat→409; publicat→409;
  Draft/Numerotat→OK. **Garda de comunicări la prefect e amânată la Pas 10** (tabelul nu există încă) — comentată în cod.
- `EtichetaDispozitie` (`ExtensiiEnumuri`): `Retractat` → „Revocat de primar (emitent)", `AbrogatHclUlterior` →
  „Abrogată prin dispoziție ulterioară"; restul deleagă la globalul HCL (neatins). Expusă ca `motivInvalidareEticheta`
  în `DispozitieDetaliiDto` (populată doar când e invalidată) — ca să fie „vie" + testabilă și ca FE/PDF să ia
  eticheta corectă fără să reimplementeze remap-ul. **11 teste.**

## Verificare (rulat de Claude)
- **Verde după fiecare sub-pas** (întâi clasa nouă, apoi suita completă). Progresie: 276 → 285 (5a) → 292 (5b) →
  298 (Pas 6) → 301 (Pas 7) → **312 (Pas 8)**. Toate prin API, izolare prin tenant nou (`ProvisioneazaInstitutieAsync`).
- Refactorul de la 7a validat cu suitele HCL+PV (64 verzi pe Dispoziții+HCL+PV împreună) — zero regresie.

## Probleme / capcane descoperite
1. **`Edit` cere `Read` prealabil (nu `Grep`).** Când am adăugat interfața pe `Hcl.cs`/`ProcesVerbal.cs`, Edit a
   eșuat cu „File has not been read yet" fiindcă le văzusem doar prin `Grep`. Regulă: citește fișierul cu `Read`
   înainte de `Edit`, chiar dacă știi linia din grep.
2. **Gărzi netestabile prin API (design conștient):** (a) constrângerile DB (max 1 emitent/secretar activ, XOR
   subiect) nu-s violabile prin API — `Creeaza` produce mereu perechea validă, iar singurul mod de a pierde
   secretarul e `RefuzContrasemnare` (care setează și flag-ul). Validate structural (migrație S58) + prin testul de
   creare. (b) `Delete` pe `EstePublicat` și freeze pe `AIntratInCircuit` sunt încă accesibile doar prin `DbTest`
   (endpoint-urile reale de publicare/comunicare vin în Pas 9/10).
3. **Wart HCL în coduri interne** (`TipRezultatAtribuire.HclInexistent`/`StareInvalidaHcl`) — mapat în
   `DispozitiiController` cu mesaje proprii; enum-ul nu l-am redenumit (ar atinge `HclController`, testele nu depind
   de nume).
4. **Lock pe `Cleriq.exe`** (din S58): l-am ținut oprit toată sesiunea, zero probleme de build/test.

## Sfaturi pentru sesiunile următoare

### Pas 9 — Publicare + MOL + latch (următorul)
- **Normativ = liber** (paritar HCL): `PublicareMol` setează `DataPublicareMol` + latch `AIntratInCircuit=true`.
  „Anulează MOL" = metadată + motiv obligatoriu + rând `IstoricActiuneAct(TipAct.Dispozitie, ActId, AnulareMol)`;
  blocat după comunicarea la prefect (dar comunicarea = Pas 10, deci ACEA parte a gărzii se completează în Pas 10).
- **Individual = nepublic implicit + override deliberat**: `Publicare`/`PublicareMol` cer
  `ConfirmaPublicareIndividuala=true` + `Motiv` ⇒ altfel **soft-409 + confirmare** (paritar `ConfirmaCuRelatiiActive`
  din `HclController.Invalidare`, NU 409 dur). La override: rând `IstoricActiuneAct(PublicareIndividualaOverride, motiv)`
  + avertisment că anonimizarea e sarcina secretarului (nu se procesează automat).
- **Enum-urile de audit sunt gata**: `TipActiuneAct { AnulareMol=1, PublicareIndividualaOverride=2 }`, `TipAct.Dispozitie=2`.
  Pattern-ul de insert e în `HclController.AnuleazaPublicareMol` (`_context.IstoricActiuniAct.Add(...)` cu `AdresaIp`).
- **Atenție la forma publicării**: HCL are DOUĂ căi — `PUT /Publicare` (toggle `EstePublicat`) ȘI `PUT /PublicareMol`
  (`DataPublicareMol` + latch). Decide în Pas 9 ce expui pe Dispoziție. Câmpurile `EstePublicat`, `DataPublicareMol`,
  `PublicataDe`, `AIntratInCircuit` există deja pe entitate. Helperii `DbTest.SeteazaEstePublicat/AIntratInCircuitDispozitieAsync`
  se pot înlocui cu endpoint-uri reale pe măsură ce apar (testul de freeze din Pas 7 și cel de Delete-publicat din Pas 8
  vor putea folosi calea reală).
- Testul de paritate util: `TesteAnulareMolHcl.cs`.

### Pas 10 — Comunicare prefect
- Entitatea `ComunicareDispozitiePrefect` + `DbSet` + navigarea `Dispozitie.Comunicari` + cascada `case Dispozitie`
  au fost **amânate explicit la Pas 4** — de adăugat acum (migrație). Paralel `ComunicareHclPrefect` +
  `ServiciuComunicareHclPrefect`, registru propriu `(InstitutieId, AnRegistru, NumarOrdine)`, 10 zile lucrătoare,
  latch la prima comunicare, controller nested `/api/Dispozitii/{id}/Comunicari`, endpoint T-3 de dashboard.
- **Abia acum se completează două gărzi lăsate în așteptare**: (a) `Sterge` — „comunicări active → 409" (comentat în
  `DispozitiiController.Sterge`); (b) „Anulează MOL" blocat după comunicare (Pas 9). De adăugat și `Comunicari` la
  `MapareDispozitie.CuIncludeComplet` + în `DispozitieDetaliiDto`.

### Pas 11 — PDF
- Extrage bază comună `GeneratorPdfAct` din `GeneratorPdfProcesVerbal` + `GeneratorPdfHcl` (rule-of-three), refactor
  ambele. `GeneratorPdfDispozitie` = **doar watermark** pe stare (semnăturile stau în markdown, deja în generator).

### General
- Pattern-ul de sub-împărțire (fiecare pas → 1–3 sub-pași, stop la verde pentru OK) merge bine. `DbTest` +
  `ExtensiiTeste` s-au îmbogățit cu tot ce-ți trebuie pentru Dispoziție; reutilizează-le.

## Fișiere noi / atinse
- **Noi (backend)**: `DTOs/DispozitieDto.cs`, `Services/IGeneratorDispozitie.cs` + `GeneratorDispozitie.cs`,
  `Helpers/MapareDispozitie.cs`, `Controllers/DispozitiiController.cs`, `Models/IActCuVariantaSemnata.cs`,
  `Helpers/VariantaSemnata.cs`.
- **Noi (teste)**: `Cleriq.Tests/TesteDispozitii.cs` (29 teste: creare/conținut/semnare/refuz/variantă semnată/
  invalidare/delete), `Cleriq.Tests/TesteNumerotareDispozitii.cs` (7).
- **Modificate (backend)**: `Program.cs` (DI), `Models/Dispozitie.cs` + `Hcl.cs` + `ProcesVerbal.cs`
  (`+ IActCuVariantaSemnata`), `Controllers/HclController.cs` + `ProcesVerbalController.cs` (refactor la
  `VariantaSemnata`), `Controllers/MentenantaController.cs` (`+ dispozitiiSemnateDb`), `Helpers/ExtensiiEnumuri.cs`
  (`+ EtichetaDispozitie`).
- **Modificate (teste)**: `Infrastructura/ExtensiiTeste.cs` (helperi Dispoziție), `Infrastructura/DbTest.cs`
  (`SeteazaStatusDispozitieAsync`, `SoftDeleteDispozitieAsync`, `SeteazaAIntratInCircuitDispozitieAsync`,
  `SeteazaEstePublicatDispozitieAsync`, `CitesteCaleStocareSemnatDispozitieAsync`), `TesteMentenanta.cs` (extins).

## Mediu / stare
- Build: `dotnet build Cleriq.slnx`. Teste: `dotnet test Cleriq.Tests/Cleriq.Tests.csproj` (312 teste, ~90s, SQL
  Server real + Redis). Înainte de build/test: `Stop-Process -Name Cleriq -Force` (lock pe `Cleriq.exe`).
- **Zero migrații** în S59. Stare DB dev neschimbată față de S58 (tabelele `Dispozitii`/`SemnatariDispozitie` există,
  se populează doar în teste pe tenanți efemeri).
- **Git**: branch `claude/gracious-turing-fh5l6r`; **modificările sunt NEcommise** (userul comite la final).
  Backend-ul de dev e oprit.

## Următoarea sesiune
**Pas 9 (Faza D)** — Publicare + MOL + latch. Apoi Pas 10 (Comunicare prefect — migrație + gărzile amânate), Pas 11
(PDF), Pas 12 (dispoziția de convocare, opțional). Citește secțiunea pașilor din `plan_faza4_backend.md` înainte.
