Plan Faza 4 (backend) — Modul C: Dispoziții primar

> Input autoritativ: `docs/research/research_dispozitii.md` (concluzii juridice +
> recomandări de design), pe fundația `research_hcl.md` + `research_MotivInvalidare.md`.
> Suprafața de reutilizare = Modul A / HCL (S49–S57), inspectată în cod, nu presupusă.
> Convenții: `CLAUDE.md` + `Cleriq.Tests/CLAUDE.md`. Roadmap: Faza 4 = 2-3 sesiuni.

---

## Context de pornire (ce reutilizăm din Modul A / HCL)

Dispoziția primarului e un act administrativ cu **același ciclu de viață** ca HCL
(Draft → Numerotat → Semnat), dar cu **origine și reguli diferite**. Diferențele esențiale,
toate ancorate în text de lege (research §1–§7):

- **Origine — emitere unilaterală, fără vot.** Primarul *emite* dispoziția (art. 196 alin. (1)
  lit. b) Cod adm.), nu o adoptă un consiliu. Deci **fără** `PunctOrdineZiId`, **fără** snapshot
  de vot, **fără** `TipMajoritate`.
- **Semnatari = Emitent (primar) + Contrasemnătură secretar general** (art. 240 alin. (1) +
  art. 243 alin. (1) lit. a) — NU președinte de ședință + secretar; cazul art. 140 (refuzul
  președintelui) **nu există** la dispoziție.
- **Două tipuri** (`TipDispozitie`): **Normativ** (se publică în MOL) vs. **Individual**
  (acte de personal — NU se publică implicit; date cu caracter personal, Legea 190/2018).
- **Numerotare** într-un **registru propriu** (separat de HCL), per instituție per an, cu
  **ambele tipuri în aceeași secvență** (Anexa 1 lit. d — confirmat empiric).
- **Comunicare la prefect pentru AMBELE tipuri**, 10 zile lucrătoare (art. 197 alin. (1)).
- **Intangibilitate identică** cu HCL: latch `AIntratInCircuit` la cel mai timpuriu moment
  verificabil (publicare normativ / comunicare destinatar individual / comunicare prefect).

**Piese HCL de citit ca paritate** (le adaptăm, nu le reinventăm): `Models/Hcl.cs`,
`Models/SemnatarHcl.cs`, `Models/ComunicareHclPrefect.cs`, `Models/IstoricActiuneHcl.cs`,
`Controllers/HclController.cs`, `Controllers/ComunicariHclPrefectController.cs`,
`Services/ServiciuNumerotareHcl.cs`, `Services/GeneratorHcl.cs`, `Services/GeneratorPdfHcl.cs`
(+ `GeneratorPdfProcesVerbal.cs`), `Services/ServiciuFunctiiIstorice.cs`, `Data/AppDbContext.cs`,
`Helpers/MapareHcl.cs`, `Controllers/MentenantaController.cs`.

**Stare de pornire S57:** 276 teste verzi, mecanica „Anulează MOL" + latch + audit
(`IstoricActiuneHcl` + `AIntratInCircuit`) deja livrată — o reutilizăm integral.
`IServiciuFunctiiIstorice` are **deja** `CinEPrimarulLa(data)` și `CinESecretarulUatLa(data)`
implementate (verificat în cod) — nu mai construim funcții istorice noi.

### Ce CORECTEAZĂ research-ul față de versiunea ștersă a acestui plan

| Schimbare | Temei |
|---|---|
| Publicare individuale: **NU 409 dur** → **nepublic implicit + override deliberat** (confirmare + motiv + audit + anonimizare = sarcina secretarului) | Validare empirică eMOL; cazuri-limită legitime (act anonimizat, numire comisie publică) |
| `Retractat` „de consiliul emitent" → **„Revocat de primar (emitent)"** + regulă nouă: revocarea proprie admisă **oricând** la Normativ, **doar înainte de `AIntratInCircuit`** la Individual | Distincția reală e normativ/individual, nu primar/consiliu (art. 1 alin. (6) L554/2004; ÎCCJ 74/2023) |
| Nou: stare **„contrasemnătură refuzată cu obiecție de legalitate"** (primarul poate emite pe răspundere proprie peste obiecția secretarului) | Art. 197 alin. (3) + Anexa 1 „registrul refuzurilor"; confirmat eMOL (Deta art. 20, 23) |

---

## Decizii de arhitectură (reuse) — recomandate, cu motiv scurt

**Principiu:** generalizăm infrastructura care e *același concept juridic* (numerotare,
lifecycle, audit) + extragem „chrome"-ul la a 3-a folosire (PDF, variantă semnată);
paralelizăm ce e *divergent ca domeniu* (template de conținut) sau cu suprafață mare încă
neclară (comunicare prefect). Rule-of-three nu e dogmă: îl respectăm unde similaritatea e
incidentală, îl depășim unde codul duplicat ar fi concurrency-critic.

| # | Piesă | Folosire | Recomandare | Motiv |
|---|---|---|---|---|
| 1 | **Entitate `Dispozitie`** | — | **SEPARATĂ** (NU moștenire EF din `Hcl`) | Origine, semnatari, regula normativ/individual diferă prea mult; o tabelă partajată ar umple `Hcl` de coloane nullable + discriminator. Reutilizăm prin servicii comune, nu prin tabelă. |
| 2 | **Generator PDF** (page/header/footer/watermark) | a 3-a (PV+HCL+Disp) | **EXTRAGE** bază `GeneratorPdfAct` | Rule-of-three împlinit; cele 3 diferă doar la watermark (verificat în cod). |
| 3 | **Variantă semnată Nivel 1** (validare/stocare/hash/replace) | a 3-a (PV+HCL+Disp) | **EXTRAGE** helper de file-handling | Mecanica de stocare e identică; gărzile de freeze (divergente) rămân inline per-controller. |
| 4 | **Numerotare** (compare-and-swap anti-lacună) | a 2-a (HCL+Disp) | **GENERALIZEAZĂ** `IActNumerotat` + serviciu generic | Algoritm concurrency-critic, divergență minimă; duplicarea = dublu risc de bug pe partea cea mai greu de făcut corect. |
| 5 | **Lifecycle enum** (Draft/Numerotat/Semnat) | a 2-a | **GENERALIZEAZĂ** rename `StatusHclRedactional`→`StatusActRedactional` | Aceeași noțiune; precondiție pt. numerotarea generică; rename mecanic, valorile int rămân, teste = plasă. |
| 6 | **Audit** (`IstoricActiuneHcl`) | a 2-a | **GENERALIZEAZĂ** → `IstoricActiuneAct` (`TipAct`+`ActId`, ref. slabă) | Auditul e prin natură cross-act; tabel mic azi (1 tip de acțiune), refactor ieftin acum; pregătit pt. erata (Faza 7). |
| 7 | **Generator de conținut** (template juridic) | a 2-a | **PARALEL** `GeneratorDispozitie` | Template-ul diferă real (preambul cu competențele primarului, fără vot/cvorum); similaritate incidentală, nu de extras. |
| 8 | **Comunicare prefect** (entitate+controller+alertă T-3) | a 2-a | **PARALEL** `ComunicareDispozitiePrefect` | Suprafață mare CRUD/DTO/alertă; abstracția ar fi leaky; întrebarea „registru comun sau separat" e deschisă — paralel ține opțiunea liberă. |

**Note de mecanică pentru deciziile de generalizare:**

- **#4 Numerotare:** `IActNumerotat : IEntitateCuTenant` expune `Numar?`, `AnNumerotare?`,
  `Status (StatusActRedactional)`, `Continut?`, `DataReferintaNumerotare` (computed: `DataAdoptare`
  la HCL / `DataEmitere` la Dispoziție). Serviciul generic folosește **metode generice**
  `…Async<T>() where T : EntitateDeBaza, IActNumerotat` peste `_context.Set<T>()` (T concret la
  call-site → EF traduce corect proprietățile mapate). Păstrez `IServiciuNumerotareHcl` ca
  **fațadă subțire** peste generic ⇒ `HclController` + `TesteNumerotareHcl` rămân **neatinse**.
  Placeholder-ul `PlaceholderHcl.NumarNeatribuit` devine `PlaceholderAct.NumarNeatribuit` (text
  partajat).
- **#6 Audit:** `IstoricActiuneAct { TipAct, ActId, Tip, Motiv, AdresaIp, InstitutieId }` — **fără
  FK/navigație** către act (ref. slabă, pentru a suporta orice tip de act fără coloană-per-tip).
  Integritatea rămâne în practică prin soft-delete-everywhere (actele nu pleacă fizic). **Auditul
  NU se mai cascadează** la soft-delete-ul actului — un log imutabil supraviețuiește actului
  (mai corect decât cascada din S57; renunțăm la `case Hcl → IstoricActiuniHcl`). Migrația
  redenumește tabela + coloana `HclId→ActId`, adaugă `TipAct` (backfill `=Hcl` pe rândurile
  existente), scoate FK-ul către `Hcl`.

---

## Decizii de domeniu confirmate (sesiunea de planificare)

1. **Emitent — derivare + înlocuitor de drept.** Emitentul se derivă din `CinEPrimarulLa(DataEmitere)`.
   Dacă e null (vacanță/suspendare) → `Creeaza` întoarce 400 cu mesaj clar; operatorul alege manual
   viceprimarul ca emitent (prefix „p. Primar" în PDF). **NU codăm art. 163** (titular vs. înlocuitor
   de drept) — principiul *defer-to-secretar*: aplicația oferă structura + auditul, decizia o ia
   omul responsabil legal.
2. **Contrasemnătură refuzată — modelare minimală în Faza 4.** Câmpuri pe `Dispozitie` +
   endpoint de consemnare a refuzului (obiecție de legalitate motivată), fiindcă starea gatează
   `Semneaza` (primarul poate emite pe răspundere proprie peste refuz). „Registrul refuzurilor"
   public (Anexa 1) = **Faza 9**.
3. **Override publicare individuală — în Faza 4.** Chiar dacă portalul public e Faza 9, starea
   internă (`EstePublicat`/MOL) + calea de override pe individuale trebuie corecte de la început.
4. **Dispoziția de convocare — Pas 4, izolat.** Rămâne în Faza 4 ca pas final, deferabil în
   sesiune separată dacă timpul o cere.

---

## Întrebări de confirmat (deschise / defer-to-secretar)

Acestea NU blochează implementarea — sunt puncte juridic-interpretabile pe care le rezolvăm prin
*structură + audit*, decizia substanțială rămânând a secretarului (la fel ca la HCL, S57):

1. **Strictețea gărzii de revocare** pe Individual intrat în circuit: blocaj 409 (recomandat,
   paritar gărzile HCL) vs. avertisment + permitere. Lean: **blocaj**, cu mesaj care trimite la
   „Anulat de instanță".
2. **`Inexistent` pentru o individuală:** invalidare vs. stare anterioară intrării în vigoare —
   clasificarea o face secretarul la marcare (carry din S57, punct de avocat).
3. **Registrul comunicărilor la prefect:** comun cu HCL sau separat per tip de act. Am paralelizat
   (= separat) ⇒ de reconfirmat doar dacă practica cere o secvență unificată.
4. **`reclamant` la `AnulatInstanta` + sub-fluxul de reanaliză după atacare:** rămân **amânate**
   (paritate cu HCL Etapa 3 / S57). De confirmat că rămân în afara Fazei 4.
5. **Verificarea juridică finală** (avocat de drept administrativ) — planificată de echipă la
   încheierea aplicației, ca pas de asigurare, nu precondiție.

---

## Construcția pe pași (backend), teste-verzi-la-fiecare-pas

Lucrăm ca în S57: fiecare pas = cod + teste verzi înainte de a trece mai departe (NU amânăm
testele). Migrație EF la orice schimbare de schemă, aplicată pe DB de dev (`dotnet ef database
update` — manual în Development). Domeniu în română; multi-tenant + soft-delete peste tot.

### Pas 1 — Fundație generalizată + strat de date `Dispozitie` + numerotare

**Generalizări (cu testele HCL ca plasă):**
- Rename `StatusHclRedactional` → `StatusActRedactional` (Enums + toate referințele + eticheta
  din `ExtensiiEnumuri`). Mecanic; valorile int neschimbate.
- `IActNumerotat` + `IServiciuNumerotareActe` generic (vezi nota #4). `Hcl : IActNumerotat`
  (`DataReferintaNumerotare => DataAdoptare`). Fațadă `IServiciuNumerotareHcl` păstrată.
- Rename audit `IstoricActiuneHcl` → `IstoricActiuneAct` (+`TipAct`,`ActId` ref. slabă; vezi nota
  #6). Enum `TipAct { Hcl=1, Dispozitie=2 }`. `TipActiuneHcl` → `TipActiuneAct` (+ valoare nouă
  `PublicareIndividualaOverride=2`, pe lângă `AnulareMol=1`).

**Strat de date Dispoziție:**
- Entitate `Dispozitie : EntitateDeBaza, IEntitateCuTenant, IActNumerotat`. Câmpuri (paralel `Hcl`,
  fără cele HCL-specifice):
  - `Numar?`, `AnNumerotare?`, `TipDispozitie`, `Titlu`, `Continut?`, `DataEmitere`,
    `DataIntrareInVigoare?`, `Status (StatusActRedactional)`.
  - Variantă semnată: `CaleStocareSemnat?`, `NumeFisierSemnat?`, `MarimeSemnat?`,
    `HashSha256Semnat?`, `DataIncarcareSemnat?`.
  - Publicare: `EstePublicat`, `DataPublicareMol? (DateOnly)`, `PublicataDe?`, `AIntratInCircuit`.
  - Contrasemnătură refuzată: `ContrasemnaturaRefuzata`, `ObiectieLegalitateSecretar?`,
    `RefuzContrasemnareDe?`, `DataRefuzContrasemnare?`.
  - Invalidare (reuse `MotivInvalidare`): `DataInvalidare?`, `MotivInvalidare?`, `RefInvalidare?`,
    `MotivInvalidareAltulText?`, `InvalidatDe?`.
  - `InstitutieId`; navigări `Semnatari`, `Comunicari`.
- `SemnatarDispozitie : EntitateDeBaza, IEntitateCuTenant`: `DispozitieId`, XOR `PersoanaId?`/
  `ConsilierId?` (Consilier necesar pt. viceprimar-înlocuitor), `RolSemnatar
  (RolSemnatarDispozitie { Emitent=1, SecretarContrasemnatura=2 })`, `DataSemnare`,
  `OrdineAfisare`, `InstitutieId`.
- Enum-uri noi: `TipDispozitie { Normativ=1, Individual=2 }`, `RolSemnatarDispozitie`.

**`AppDbContext`:**
- DbSets: `Dispozitii`, `SemnatariDispozitie`, `ComunicariDispozitiePrefect` (Pas 3).
- FK Institutie + FK Dispozitie cu **Restrict** (paritar HCL).
- Index unic filtrat `(InstitutieId, AnNumerotare, Numar)` pe `Dispozitii`
  (`[EsteSters]=0 AND [Numar] IS NOT NULL`) — **registru propriu**, ambele tipuri în aceeași
  secvență.
- Filtered unique pe `SemnatarDispozitie`: max 1 `Emitent` activ + max 1 `SecretarContrasemnatura`
  activ per dispoziție.
- Check constraints `SemnatarDispozitie`: exactly-one(Persoana,Consilier) +
  FK-corectă-per-rol (`SecretarContrasemnatura` ⇒ Persoana; `Emitent` ⇒ Persoana **sau** Consilier).
- Soft-cascade `case Dispozitie h:` → `SemnatariDispozitie` + `ComunicariDispozitiePrefect`
  (NU auditul — vezi nota #6).
- DbSet/cascade audit: actualizare la `IstoricActiuniAct` (referință slabă, fără cascadă din act).

**Migrație** (tabele noi + renames + backfill audit) → aplic pe dev.

**Teste (Pas 1):** numerotare pe registrul de dispoziții (lacune, nr. luat, concurență) —
paritar `TesteNumerotareHcl`; izolare prin tenant nou (`ProvisioneazaInstitutieAsync`).

### Pas 2 — Lifecycle controller: Creează → Semnează + conținut + semnat + invalidare + delete

- `DispozitiiController` (paritar `HclController`):
  - `Creeaza(tip, titlu, dataEmitere)`: derivă emitent (`CinEPrimarulLa`, 400 dacă null +
    posibil override manual consilier) + secretar (`CinESecretarulUatLa`, 400 dacă null), creează
    cei 2 semnatari, scaffold `Continut` via `GeneratorDispozitie`.
  - `EditeazaContinut` / `RegenereazaContinut` (blocate pe Semnat).
  - `AtribuieNumar` / `SugestieNumar` (prin serviciul generic).
  - `Semneaza`: gardă de completitudine — exact 1 Emitent + (1 SecretarContrasemnatura **SAU**
    `ContrasemnaturaRefuzata=true` cu `ObiectieLegalitateSecretar` completată).
  - `RefuzContrasemnare` (nou): consemnează refuzul secretarului (obiecție motivată + autor + dată).
  - Variantă semnată: `POST/GET/DELETE {id}/Semnat` — file-handling prin **helper extras**
    (decizia #3), gărzi de freeze pe `AIntratInCircuit` inline.
  - `Invalidare` / `AnuleazaInvalidare`: reuse `MotivInvalidare` + **regula de revocare**
    (Retractat: oricând la Normativ; blocat la Individual intrat în circuit → trimite la
    `AnulatInstanta`). Eticheta `Retractat` → „Revocat de primar (emitent)" pe calea dispoziției
    (label dedicat, nu se schimbă globalul HCL).
  - `Delete`: matrice de gărzi ordonate cu early-return (paritar HCL — Comunicări/Semnat/Publicat).
- `GeneratorDispozitie` (paralel, type-aware): antet „PRIMARUL …", preambul „În temeiul art. 155
  + art. 196 alin. (1) lit. b) OUG 57/2019 … DISPUNE:", **fără** vot/cvorum; secțiunea de semnături
  „PRIMAR / Contrasemnează pentru legalitate, SECRETAR GENERAL" (cu „p. PRIMAR" la înlocuitor /
  „Contrasemnează: consilier juridic" la refuzul secretarului — eMOL Deta art. 23); baner de
  invalidare reutilizat.
- **CRITIC — Mentenanță:** adaug `Dispozitii.CaleStocareSemnat` la dicționarul de scan din
  `MentenantaController.CalculeazaOrfaniDocumenteAsync` (lângă Documente/PV/HCL). Fără asta,
  cleanup-ul ar șterge fizic PDF-urile de personal semnate (impact pe probe + arhivare).

**Teste (Pas 2):** ciclul Creeaza→…→Semnat; gărzi semnare (refuz contrasemnătură); invalidare cu
Altul + regula de revocare; delete; **un** test de mentenanță pentru noul tip de stocare
(`Cleriq.Tests/CLAUDE.md` regula 8).

### Pas 3 — Publicare (Normativ + override Individual) + MOL + latch + comunicare prefect + PDF

- **Publicare/MOL:**
  - Normativ → liber (paritar HCL): `PublicareMol` setează latch `AIntratInCircuit`; „Anulează MOL"
    = metadată + motiv obligatoriu + rând `IstoricActiuneAct(AnulareMol)`, blocat după comunicarea
    la prefect.
  - **Individual = nepublic implicit + override deliberat:** `Publicare`/`PublicareMol` cer
    `ConfirmaPublicareIndividuala=true` + `Motiv` ⇒ altfel 409 cu mesaj (date personale). La
    override: rând `IstoricActiuneAct(PublicareIndividualaOverride, motiv)` + avertisment că
    anonimizarea e sarcina secretarului. (Anonimizarea NU se procesează automat — defer-to-secretar.)
- **Comunicare la prefect (ambele tipuri):** `ComunicareDispozitiePrefect` +
  `ServiciuComunicareDispozitiePrefect` (paralel HCL): registru propriu
  `(InstitutieId, AnRegistru, NumarOrdine)`, 10 zile lucrătoare, alertă T-3, latch la prima
  comunicare. Controller nested `/api/Dispozitii/{dispozitieId}/Comunicari`.
- **PDF:** extrag `GeneratorPdfAct` (bază comună: page/margin/text-style/watermark-hook/header/
  content/footer) din `GeneratorPdfProcesVerbal` + `GeneratorPdfHcl`; refactor ambele să o
  folosească (interfețele + DI neschimbate); `GeneratorPdfDispozitie` o consumă, cu watermark pe
  stare (DRAFT/INVALIDAT) + secțiunea de semnături primar/secretar.

**Teste (Pas 3):** publicare Normativ liberă; Individual blocat fără confirmare, permis cu override
+ audit; fluxul MOL + latch + „Anulează MOL" (paritar `TesteAnulareMolHcl`); comunicare prefect
(ambele tipuri) + alertă; randare PDF (smoke prin endpoint).

### Pas 4 — Bonus: dispoziția de convocare (opțional, posibil sesiune separată)

- La trimiterea convocării de către primar (`ConvocareController` / `WorkerConvocari`), auto-generez
  o `Dispozitie` **Individuală / de organizare**, numerotată în registrul general, legată de
  ședință (FK nullable `SedintaId?` pe `Dispozitie` sau câmp de referință), cu „proiectul ordinii
  de zi" în conținut, **fără** publicare automată în MOL.
- Doar pentru convocările făcute de primar (art. 133 alin. (1), (2) lit. a) și c) — **NU** pentru
  convocarea de 1/3 consilieri (art. 133 alin. (2) lit. b)).
- Îl ținem la final, izolat, ca să nu blocheze Pașii 1–3.

---

## Ce NU intră în Faza 4 (limite de scop)

- **Portalul public** pentru dispoziții normative = **Faza 9** (rute publice + decizia SSR). Faza 4
  face doar starea internă de publicare (`EstePublicat` + MOL), nu endpoint-urile publice.
- **Erata pe dispoziție** + **relațiile între dispoziții** (modifică/abrogă) = **Faza 7** (pattern
  unificat PV/HCL/Dispoziție). Consecință: abrogarea unei dispoziții normative prin act nou nu se
  modelează ca relație în Faza 4.
- **Registrul electronic** cronologic + gap-detection UI = **Faza 5**.
- **Sub-fluxul de reanaliză** după atacarea de prefect + sub-câmpul `reclamant` = **amânate**
  (paritate cu HCL).
- **„Registrul refuzurilor" public** (Anexa 1) = **Faza 9**; în Faza 4 doar consemnarea internă.
- **Anexe-ca-`Document` pe dispoziție** (`Document.DispozitieId`) — **în afara scopului** deocamdată;
  convocarea-dispoziție pune ordinea de zi în conținut. Singura stocare nouă de fișiere în Faza 4 =
  varianta semnată (`Dispozitii.CaleStocareSemnat`), deja acoperită de scanul de orfani.
- **Regimul art. 528** (intrarea în vigoare a actelor de personal) + **auto-anonimizarea** datelor
  personale = **NU se codează** (defer-to-secretar; aplicația oferă structură + audit).

---

## Filozofie de implementare (din planurile Fazei 3, încă validă)

Planul e ghid, nu script. La fiecare piesă: citesc controllerul/serviciul de paritate HCL,
identific pattern-ul, adaptez la Dispoziție. Verific compilarea + testele după **fiecare** piesă,
nu doar la final. Generalizările (numerotare, audit, lifecycle, PDF) le fac cu testele HCL/PV
existente ca plasă de siguranță — orice regresie acolo se prinde imediat. Regula de aur a
research-ului: **calculez ce e text de lege clar (numerotare, gărzi de circuit, semnatari,
publicare doar normativ); deferă către secretar ce e interpretabil sau ține de o lege separată.**
