Plan Faza 4 (backend) — Modul C: Dispoziții primar

> Input autoritativ: `docs/research/research_dispozitii.md` (concluzii juridice +
> recomandări de design), pe fundația `research_hcl.md` + `research_MotivInvalidare.md`.
> Suprafața de reutilizare = Modul A / HCL (S49–S57), inspectată în cod, nu presupusă.
> Convenții: `CLAUDE.md` + `Cleriq.Tests/CLAUDE.md`. Roadmap: Faza 4 = 2-3 sesiuni.

> **Recenzie încrucișată S58 (integrată mai jos, marcată ◆ în text):** numerotare —
> `DataReferintaNumerotare` `[NotMapped]` + `DateTime`; audit — migrația de rename
> hand-edited (nu drop+create); semnare — `RefuzContrasemnare` soft-șterge rândul de
> secretar (3 cazuri distincte, substitutul consilier-juridic AMÂNAT); emitent/secretar
> null — asimetrie conștientă (emitent → override viceprimar / secretar → 400); invalidare —
> `EtichetaDispozitie` remapează `Retractat` **ȘI** `AbrogatHclUlterior`; publicare
> individuală — soft-409 + confirmare (NU 409 dur); comunicare prefect — alertă T-3 =
> endpoint de dashboard (NU worker), registru separat ca alegere de design (NU mandat
> legal); PDF — `GeneratorPdfDispozitie` = doar watermark (semnăturile stau în markdown);
> convocare — `SedintaId?` exclus din cascada `Sedinta`.

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
`Controllers/HclDashboardController.cs`, `Services/ServiciuNumerotareHcl.cs`,
`Services/GeneratorHcl.cs`, `Services/GeneratorPdfHcl.cs` (+ `GeneratorPdfProcesVerbal.cs`),
`Services/ServiciuComunicareHclPrefect.cs`, `Services/ServiciuFunctiiIstorice.cs`,
`Data/AppDbContext.cs`, `Helpers/MapareHcl.cs`, `Controllers/MentenantaController.cs`.

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
  `Status (StatusActRedactional)`, `Continut?`, `DataReferintaNumerotare`. ◆ `DataReferintaNumerotare`
  e **`[NotMapped]`** și întoarce **`DateTime` brut** (`DataAdoptare` la HCL / `DataEmitere` la
  Dispoziție); serviciul aplică conversia de fus orar (`.LaFusOrar(fusOrar).Year`, fusul venit din
  DB) — proprietatea computed **nu se filtrează niciodată** într-un `.Where` LINQ-to-SQL. Serviciul
  generic folosește **metode generice** `…Async<T>() where T : EntitateDeBaza, IActNumerotat` peste
  `_context.Set<T>()` (T concret la call-site → EF traduce corect proprietățile **mapate**
  Numar/AnNumerotare/Status). Păstrez `IServiciuNumerotareHcl` ca **fațadă subțire** peste generic
  ⇒ `HclController` + `TesteNumerotareHcl` rămân **neatinse**. Placeholder-ul
  `PlaceholderHcl.NumarNeatribuit` devine `PlaceholderAct.NumarNeatribuit` (text partajat).
- **#6 Audit:** `IstoricActiuneAct { TipAct, ActId, Tip, Motiv, AdresaIp, InstitutieId }` — **fără
  FK/navigație** către act (ref. slabă, pentru a suporta orice tip de act fără coloană-per-tip).
  Integritatea rămâne în practică prin soft-delete-everywhere (actele nu pleacă fizic) + FK
  `InstitutieId` păstrat. **Auditul NU se mai cascadează** la soft-delete-ul actului — un log imutabil
  supraviețuiește actului (mai corect decât cascada din S57; renunțăm la `case Hcl → IstoricActiuniHcl`).
  ◆ Migrația redenumește tabela + coloana `HclId→ActId`, adaugă `TipAct` (backfill `=Hcl`), scoate FK-ul
  către `Hcl` — **hand-edited** la `RenameTable`/`RenameColumn` (EF scaffold-uiește des drop+create, ce
  pierde rândurile de audit existente; pe dev = date de test, în prod = audit real).

---

## Decizii de domeniu confirmate (sesiunea de planificare + recenzie S58)

1. **Emitent — derivare + înlocuitor de drept; asimetrie conștientă cu secretarul.** Emitentul se
   derivă din `CinEPrimarulLa(DataEmitere)`. Dacă e null (vacanță/suspendare) → `Creeaza` întoarce 400;
   operatorul poate trimite **manual un consilier** (viceprimar) ca emitent (prefix „p. Primar" în PDF).
   Secretarul-contrasemnatar se derivă din `CinESecretarulUatLa(DataEmitere)`; ◆ dacă e null → **400
   conștient** (NU override), fiindcă vacanța de secretar e rară / lacună de date, iar substitutul
   consilier-juridic e amânat (vezi #2). Închiderea ulterioară a gap-ului = mirror al override-ului
   emitentului (**aditiv, nu refacere**). **NU codăm art. 163** (titular vs. înlocuitor de drept) —
   principiul *defer-to-secretar*: aplicația oferă structura + auditul, decizia o ia omul responsabil legal.
2. **Contrasemnătură refuzată — modelare minimală în Faza 4, 3 cazuri distincte.** Câmpuri pe
   `Dispozitie` + endpoint de consemnare a refuzului (obiecție de legalitate motivată), fiindcă starea
   gatează `Semneaza` (primarul poate emite pe răspundere proprie peste refuz). ◆ Cele 3 cazuri NU se
   confundă: (1) secretar contrasemnează normal; (2) **refuz** → fără contrasemnătură, obiecția în
   registru; (3) **secretar absent** → substitut consilier-juridic = **AMÂNAT** (cere tip de funcție nou
   sau pick manual de Persoana; nu-l construim acum, nu lipim „consilier juridic" pe cazul de refuz).
   „Registrul refuzurilor" public (Anexa 1) = **Faza 9**.
3. **Override publicare individuală — în Faza 4.** Chiar dacă portalul public e Faza 9, starea
   internă (`EstePublicat`/MOL) + calea de override pe individuale trebuie corecte de la început.
   ◆ Mecanismul = **soft-409 + confirmare** (paritar `ConfirmaCuRelatiiActive` din `HclController.Invalidare`),
   NU un 409 dur care interzice complet.
4. **Etichete act-aware (`MotivInvalidare`).** ◆ `EtichetaDispozitie(this MotivInvalidare?)` remapează
   `Retractat` → „Revocat de primar (emitent)" **ȘI** `AbrogatHclUlterior` → „Abrogată prin dispoziție
   ulterioară" (cuvântul „hotărâre" din eticheta globală e greșit pe o dispoziție; valoarea e selectabilă
   manual via `Enum.IsDefined` chiar dacă abrogarea-ca-relație e Faza 7); restul deleagă la globalul HCL,
   neatins.
5. **Dispoziția de convocare — Pas 12, izolat.** Rămâne în Faza 4 ca pas final, deferabil în
   sesiune separată dacă timpul o cere.

---

## Întrebări — rezolvate (defer-to-secretar acolo unde rămân interpretabile)

Niciuna nu blochează implementarea; le rezolvăm prin *structură + audit*, decizia substanțială
rămânând a secretarului (la fel ca la HCL, S57):

1. **Strictețea gărzii de revocare** pe Individual intrat în circuit → **REZOLVAT: blocaj 409**
   (paritar gărzile dure HCL), cu mesaj care trimite la „Anulat de instanță". Avertisment-permisiv ar
   reintroduce exact portița închisă în S57.
2. **`Inexistent` pentru o individuală:** invalidare vs. stare anterioară intrării în vigoare —
   **defer-to-secretar**: clasificarea o face secretarul la marcare (carry din S57, punct de avocat).
3. **Registrul comunicărilor la prefect:** comun cu HCL sau separat → **REZOLVAT: separat**, ca
   **alegere de design** (paritate + simplitate), **NU mandat legal**. ◆ Atenție: Anexa 1 lit. c/d
   separă registrele de *evidență/numerotare a actelor* (hotărâri vs. dispoziții), NU registrul de
   *comunicări către prefect* (art. 197), pe care legea nu-l reglementează expres ca separat sau comun.
   Reconfirmăm doar dacă practica arată o secvență unică de ieșiri.
4. **`reclamant` la `AnulatInstanta` + sub-fluxul de reanaliză după atacare:** rămân **amânate**
   (paritate cu HCL Etapa 3 / S57).
5. **Verificarea juridică finală** (avocat de drept administrativ) — planificată de echipă la
   încheierea aplicației, ca pas de asigurare, nu precondiție.

---

## Construcția pe pași (backend), teste-verzi-la-fiecare-pas

Lucrăm ca în S57: fiecare pas = cod + teste verzi înainte de a trece mai departe (NU amânăm
testele). Mă opresc după fiecare pas, arăt rezultatul testelor, nu trec mai departe până nu e
verde. Migrație EF la orice schimbare de schemă, aplicată manual pe DB de dev în Development
(`dotnet ef database update`). Domeniu în română; multi-tenant + soft-delete peste tot.
Realist: **Faza A–B = o sesiune**, restul în 1–2 sesiuni următoare.

### Faza A — Generalizări (zero domeniu nou; testele HCL/PV = plasa de siguranță)

**Pas 1 — Rename lifecycle enum.** `StatusHclRedactional → StatusActRedactional` (Enums + toate
referințele + eticheta din `ExtensiiEnumuri`). Pur mecanic, valorile int neschimbate → **fără
migrație**, JSON neschimbat (membrii Draft/Numerotat/Semnat rămân). Primul fiindcă de-riscă tot ce
urmează. **Checkpoint:** build curat + **toată suita HCL verde** (dovedește zero regresie).

**Pas 2 — Numerotare generică.** `IActNumerotat` + serviciu generic + fațada `IServiciuNumerotareHcl`
păstrată (vezi nota #4). `Hcl : IActNumerotat` (`DataReferintaNumerotare => DataAdoptare`,
◆ `[NotMapped]`/`DateTime`). `PlaceholderHcl → PlaceholderAct`. Fără migrație. **Checkpoint:**
`TesteNumerotareHcl` verzi, `HclController` neatins.

**Pas 3 — Audit generic.** `IstoricActiuneHcl → IstoricActiuneAct` (◆ weak-ref `TipAct`+`ActId`, drop
FK, **scos din soft-cascadă**; vezi nota #6). `TipAct { Hcl=1, Dispozitie=2 }`. `TipActiuneHcl →
TipActiuneAct` (+ valoare nouă `PublicareIndividualaOverride=2`, lângă `AnulareMol=1`). ◆ Migrație
**hand-edited** (RenameTable/RenameColumn + AddColumn backfill + drop FK). **Checkpoint:** comportament
audit HCL neschimbat + migrație aplicată pe dev.

### Faza B — Stratul de date Dispoziție

**Pas 4 — Entități + schemă.** Entitate `Dispozitie : EntitateDeBaza, IEntitateCuTenant, IActNumerotat`:
- `Numar?`, `AnNumerotare?`, `TipDispozitie`, `Titlu`, `Continut?`, ◆ `DataEmitere` (**`DateTime`**),
  `DataIntrareInVigoare?` (`DateOnly`), `Status (StatusActRedactional)`.
- Variantă semnată: `CaleStocareSemnat?`, `NumeFisierSemnat?`, `MarimeSemnat?`, `HashSha256Semnat?`,
  `DataIncarcareSemnat?`.
- Publicare: `EstePublicat`, `DataPublicareMol?` (`DateOnly`), `PublicataDe?`, `AIntratInCircuit`.
- Contrasemnătură refuzată: `ContrasemnaturaRefuzata`, `ObiectieLegalitateSecretar?`,
  `RefuzContrasemnareDe?`, `DataRefuzContrasemnare?`.
- Invalidare (reuse `MotivInvalidare`): `DataInvalidare?`, `MotivInvalidare?`, `RefInvalidare?`,
  `MotivInvalidareAltulText?`, `InvalidatDe?`.
- `InstitutieId`; navigări `Semnatari`, `Comunicari`; `DataReferintaNumerotare => DataEmitere` (`[NotMapped]`).

`SemnatarDispozitie : EntitateDeBaza, IEntitateCuTenant`: `DispozitieId`, XOR `PersoanaId?`/`ConsilierId?`
(Consilier necesar pt. viceprimar-înlocuitor), `RolSemnatar (RolSemnatarDispozitie { Emitent=1,
SecretarContrasemnatura=2 })`, `DataSemnare`, `OrdineAfisare`, `InstitutieId`. Enum-uri noi:
`TipDispozitie { Normativ=1, Individual=2 }`, `RolSemnatarDispozitie`.

`AppDbContext`: DbSets `Dispozitii`, `SemnatariDispozitie` (+ `ComunicariDispozitiePrefect` la Pas 10);
FK Institutie + FK Dispozitie cu **Restrict** (paritar HCL); index unic filtrat
`(InstitutieId, AnNumerotare, Numar)` pe `Dispozitii` (`[EsteSters]=0 AND [Numar] IS NOT NULL`) — registru
propriu, ambele tipuri în aceeași secvență; filtered-unique max 1 `Emitent` activ + max 1
`SecretarContrasemnatura` activ per dispoziție; check constraints (exactly-one(Persoana,Consilier) +
◆ `SecretarContrasemnatura ⇒ Persoana`; `Emitent ⇒ Persoana SAU Consilier`); soft-cascadă
`case Dispozitie h:` → `SemnatariDispozitie` (+ `ComunicariDispozitiePrefect` la Pas 10) — **NU auditul**.
Migrație tabele noi → aplic pe dev. **Checkpoint:** numerotare pe registrul de dispoziții (lacune/nr.
ars/concurență), paritar `TesteNumerotareHcl`, izolare prin tenant nou (`ProvisioneazaInstitutieAsync`).

### Faza C — Lifecycle (`DispozitiiController`, incremental)

**Pas 5 — Creează + conținut.** `Creeaza(tip, titlu, dataEmitere, emitentConsilierId?)`: derivă emitent
(`CinEPrimarulLa`, 400 dacă null + override manual consilier) + secretar (`CinESecretarulUatLa`,
◆ 400 conștient dacă null), creează cei 2 semnatari, scaffold `Continut` via `GeneratorDispozitie`.
`EditeazaContinut` / `RegenereazaContinut` (blocate pe Semnat). `AtribuieNumar` / `SugestieNumar` (prin
serviciul generic). `GeneratorDispozitie` (paralel, type-aware): antet „PRIMARUL …", preambul „În temeiul
art. 155 + art. 196 alin. (1) lit. b) OUG 57/2019 … DISPUNE:", **fără** vot/cvorum; ◆ secțiunea de
semnături în **markdown** („PRIMAR / Contrasemnează pentru legalitate, SECRETAR GENERAL", cu „p. PRIMAR"
la înlocuitor); baner de invalidare reutilizat. **Checkpoint:** ciclul Creează → editează/regenerează →
numerotează.

**Pas 6 — Semnare + contrasemnătură refuzată.** `Semneaza`: gardă de completitudine — exact 1 Emitent +
(1 SecretarContrasemnatura **SAU** `ContrasemnaturaRefuzata=true` cu `ObiectieLegalitateSecretar`
completată), paritar `HclController.Semneaza`. ◆ `RefuzContrasemnare` (nou): consemnează obiecția motivată
(autor + dată) **ȘI soft-șterge rândul `SecretarContrasemnatura`** (eliberează filtered-unique-ul; altfel
garda îl numără și ramura „SAU refuz" nu se activează). **Checkpoint:** gărzi de semnare + calea de refuz
(soft-delete secretar + emite peste refuz).

**Pas 7 — Variantă semnată.** `POST/GET/DELETE {id}/Semnat`: ◆ extrag helper de file-handling
(validare/stocare/hash/replace) la a 3-a folosire (PV+HCL+Disp), cu testele PV+HCL ca plasă; gărzile de
freeze pe `AIntratInCircuit` rămân inline per-controller (paritar HCL). ◆ **CRITIC — Mentenanță:** adaug
`Dispozitii.CaleStocareSemnat` la dicționarul de scan din `MentenantaController.CalculeazaOrfaniDocumenteAsync`
(lângă Documente/PV/HCL). Fără asta, cleanup-ul ar șterge fizic PDF-urile de personal semnate (impact pe
probe + arhivare). **Checkpoint:** upload/replace/delete + freeze pe latch + **un** test de mentenanță
(`Cleriq.Tests/CLAUDE.md` regula 8).

**Pas 8 — Invalidare + revocare + delete.** `Invalidare` / `AnuleazaInvalidare`: reuse `MotivInvalidare`
(gardă `Enum.IsDefined` + Altul cu text obligatoriu, paritar HCL) + **regula de revocare** (`Retractat`
oricând la Normativ; **blocat 409** la Individual intrat în circuit → trimite la „Anulat de instanță").
◆ `EtichetaDispozitie` remapează `Retractat` **ȘI** `AbrogatHclUlterior` (label dedicat dispoziției;
globalul HCL neatins); același cuvânt în banner-ul de invalidare din `GeneratorDispozitie`. `Delete`:
matrice de gărzi ordonate cu early-return (paritar HCL — Comunicări/Semnat/Publicat/Invalidat). **Checkpoint:**
invalidare cu Altul + regula de revocare (409 pe Individual în circuit) + gărzi delete.

### Faza D — Publicare / Comunicare / PDF

**Pas 9 — Publicare + MOL + latch.** Normativ → liber (paritar HCL): `PublicareMol` setează latch
`AIntratInCircuit`; „Anulează MOL" = metadată + motiv obligatoriu + rând `IstoricActiuneAct(AnulareMol)`,
blocat după comunicarea la prefect. ◆ **Individual = nepublic implicit + override deliberat:**
`Publicare`/`PublicareMol` cer `ConfirmaPublicareIndividuala=true` + `Motiv` ⇒ altfel **soft-409 +
confirmare** (paritar `ConfirmaCuRelatiiActive`, NU 409 dur). La override: rând
`IstoricActiuneAct(PublicareIndividualaOverride, motiv)` + avertisment că anonimizarea e sarcina
secretarului (NU se procesează automat — defer-to-secretar). **Checkpoint:** Normativ liber; Individual
blocat fără confirmare, permis cu override + audit; flux MOL + latch + „Anulează MOL" (paritar
`TesteAnulareMolHcl`).

**Pas 10 — Comunicare prefect.** `ComunicareDispozitiePrefect` + `ServiciuComunicareDispozitiePrefect`
(paralel HCL): registru propriu `(InstitutieId, AnRegistru, NumarOrdine)`, 10 zile lucrătoare, latch la
prima comunicare. Controller nested `/api/Dispozitii/{dispozitieId}/Comunicari`. ◆ Alertă T-3 = **query de
dashboard, NU worker**: metodă `ObtineDispozitiiUrgentDeComunicat` + endpoint subțire
`GET /api/Dispozitii/UrgentDeComunicat` (paritar `HclDashboardController`) ca să fie asertabil prin API;
card-ul FE se amână (decizie de FE, ~Faza 5). ◆ Registru **separat** de HCL (alegere de design, nu mandat
legal — vezi Întrebări #3). Migrație tabel nou. **Checkpoint:** comunicare ambele tipuri + latch + endpoint
T-3.

**Pas 11 — PDF.** Extrag `GeneratorPdfAct` (bază comună: page/margin/text-style/watermark-hook/header/
content/footer) din `GeneratorPdfProcesVerbal` + `GeneratorPdfHcl`; refactor ambele să o folosească
(interfețele + DI neschimbate, testele lor ca plasă). ◆ `GeneratorPdfDispozitie` = **doar watermark** pe
stare (DRAFT/INVALIDAT) peste bază — semnăturile „Primar/Secretar" stau în **markdown** (`GeneratorDispozitie`,
Pas 5), NU în PDF (corectează research linia 243). **Checkpoint:** randare PDF (smoke prin endpoint).

### Faza E — Bonus (opțional, posibil sesiune separată)

**Pas 12 — Dispoziția de convocare.** La trimiterea convocării de către primar (`ConvocareController` /
`WorkerConvocari`), auto-generez o `Dispozitie` **Individuală / de organizare**, numerotată în registrul
general, legată de ședință (◆ FK `SedintaId?` **Restrict, exclus din cascada `case Sedinta`** din
`AppDbContext` — un act numerotat nu moare cu ședința, paritar HCL azi), cu „proiectul ordinii de zi" în
conținut, **fără** publicare automată în MOL. Doar pentru convocările făcute de primar (art. 133 alin. (1),
(2) lit. a) și c) — **NU** pentru convocarea de 1/3 consilieri (art. 133 alin. (2) lit. b)). Izolat, la
final, ca să nu blocheze Pașii 1–11.

---

## Ce NU intră în Faza 4 (limite de scop)

- **Portalul public** pentru dispoziții normative = **Faza 9** (rute publice + decizia SSR). Faza 4
  face doar starea internă de publicare (`EstePublicat` + MOL), nu endpoint-urile publice.
- **Substitutul consilier-juridic** la contrasemnătură (cazul secretar absent, art. 23 alin. (3) eMOL) =
  **amânat** — cere tip de funcție nou sau pick manual de Persoana; în Faza 4 modelăm doar refuzul.
- **Erata pe dispoziție** + **relațiile între dispoziții** (modifică/abrogă) = **Faza 7** (pattern
  unificat PV/HCL/Dispoziție). Consecință: abrogarea unei dispoziții normative prin act nou nu se
  modelează ca relație în Faza 4.
- **Registrul electronic** cronologic + gap-detection UI + **card-ul FE de alertă T-3 dispoziții** = **Faza 5**.
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
