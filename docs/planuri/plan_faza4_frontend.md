Plan Faza 4 (frontend) — Modul C: Dispoziții primar (administrare internă)

> Input autoritativ: `docs/rezumate/rezumat_60.md` (suprafața de backend livrată — endpoint-uri,
> decizii, capcane) + `docs/planuri/plan_faza4_backend.md` (deciziile de domeniu) +
> `docs/research/research_dispozitii.md` (§2 normativ/individual, §4 latch, §6 invalidare/revocare,
> §7 convocare, §8 GDPR). Convenții: `cleriq-web/CLAUDE.md` + `docs/frontend.md`.
> **Paritatea = ecranele HCL (Modul A)**, inspectate în cod, NU presupuse. Le ADAPTĂM, nu le reinventăm.
>
> **Scop:** ecranele Angular de administrare internă (secretar/admin) a dispozițiilor, paralele cu HCL.
> Portalul public al cetățeanului = **Faza 9** (NU intră aici). Backend-ul Modulului C e COMPLET
> (Pașii 1–12, 339 teste verzi) — FE-ul doar consumă suprafața existentă.
>
> **◆ Recenzie de plan (sesiunea de dezvoltare, înainte de cod) — integrată mai jos, marcată ◆.**
> Verificată punct cu punct față de suprafața de backend reală (`DispozitiiController`,
> `ComunicariDispozitiiPrefectController`, `DispozitieDto`) + piesele HCL de paritate din cod.
> Cele 4 decizii deschise din §9 sunt **rezolvate**. Schimbări majore față de versiunea inițială:
> (1) enum-ul de status = **redenumire** FE, nu enum paralel; (2) comunicările = **copie paralelă**
> (nu generalizare), dar copia **adaugă output-ul care lipsește în HCL** (fix de staleness);
> (3) Delete pe act = **expus** cu boolean-ul exact; (4) 400-urile de la `Creeaza` = mesaj brut inline,
> **fără string-matching**. Plus corecții factuale față de DTO (autori neexpuși, `DataIntrareInVigoare`
> nesetat) și eticheta de invalidare pe listă.

---

## 1. Ce reutilizăm din infrastructura FE existentă (fără copiere)

Aceste piese sunt deja transversale/shared — le folosim direct, nu le duplicăm:

- `core/modificari/` — `ModificariNesalvateService` + `ghidModificariNesalvate` (auto-save + guard
  „modificări nesalvate"). Detaliul de dispoziție se înregistrează cu id `dispozitie-continut-${id}`.
- `shared/confirmare/ConfirmareDialog` (`DateConfirmare` cu `periculos: true`) — toate confirmările.
- `shared/data.ts` (`formateazaDataOra` / `formateazaDataScurta` / `formateazaDataDoar`),
  `shared/text.ts` (`normalizeazaPentruCautare`), `core/http/erori.ts` (`extrageMesajEroare`).
- `core/auth/AuthService` (`areRol` / `areOricareRol`) pentru gărzile de rol (Admin / Secretar).
- Pattern-urile din `docs/frontend.md`: editor cu auto-save + indicator de salvare, hub cu `mat-tab-group`,
  „computed pentru button primary/stroked switch", „single hidden input + N butoane", download prin blob,
  „snackbar avertizant la click", badge-uri semantice.
- ◆ **Pattern „tab → părinte refetch"** (`output` din tab → hub-ul face `incarca()`): deja folosit de
  `ConvocariTab` (`sedintaSchimbata`) și `semnatari-tab` (`actualizat`). Îl folosim pentru comunicări
  (vezi §5 fix de staleness).

---

## 2. Harta de paritate HCL → Dispoziție (REUTILIZARE vs. PARALEL) — recomandat, cu motiv scurt

**Principiu:** reutilizăm ce e *identic ca mecanică și date* (numerotare, „Anulează MOL", varianta
semnată); paralelizăm ce *diferă ca formă de DTO sau ca reguli de domeniu* (listă, hub, semnatari,
publicare, comunicări); excludem ce ține de altă fază. ◆ Am renunțat la generalizarea comunicărilor
(vezi §5): copie paralelă disciplinată, nu abstracție prematură mid-feature.

| # | Piesă HCL | Decizie | Motiv scurt |
|---|---|---|---|
| 1 | `hcl.models.ts` | **PARALEL** `dispozitii.models.ts` | DTO diferit: fără vot/majoritate/punctOrdineZi; cu `tipDispozitie`, contrasemnătură, `contrasemnaturaRefuzata`+`obiectieLegalitateSecretar`, `sedintaId`, `motivInvalidareEticheta`. |
| 2 | `hcl.service.ts` | **PARALEL** `dispozitii.service.ts` | Altă bază URL + endpoint-uri proprii (`RefuzContrasemnare`; FĂRĂ `Semnatari` CRUD, FĂRĂ `Relatii`). |
| 3 | `hcl.permisiuni.ts` | **PARALEL** `dispozitii.permisiuni.ts` | Oglindă a gărzilor dispoziției: `poateRefuzaContrasemnare` în loc de `poateGestionaSemnatari`; completitudinea la semnare = emitent + (contrasemnătură **SAU** refuz motivat). |
| 4 | `hcl-lista` (+filtre) | **PARALEL** `dispozitii-lista` (adaptare) | Aceeași structură; filtru tip = **Normativ/Individual**, buton **„Creează dispoziție"** (HCL n-are). ◆ badge-uri: tip + status + „Invalidat" (mic) + „Convocare"; **fără text de motiv invalidare** (slim DTO n-are eticheta act-aware — vezi §3). |
| 5 | `hcl-detalii` (hub) | **PARALEL** `dispozitie-detalii` (adaptare) | Același schelet (antet + editor + tab-uri), dar **4 tab-uri** (Detalii / Conținut / Semnatari / Comunicări), alte badge-uri + acțiuni. |
| 6 | Editor auto-save (în hub) | **REUTILIZARE (pattern)** | Mecanica dirty/lock/auto-save e identică; infra e deja în `core/`. Copiem structura. ◆ E a 3-a folosire (PV+HCL+Disp) — extragerea într-o componentă comună = **cleanup viitor**, nu acum (ar destabiliza 2 editoare care merg). |
| 7 | `atribuie-numar-dialog` | ◆ **REUTILIZARE prin copie paralelă** | Logica sugestie + lacune + „număr luat" e identică; diferă doar serviciul. Copie trivială sub `dispozitii/` (nu parametrizare — vezi §5). |
| 8 | `anulare-mol-dialog` | ◆ **REUTILIZARE prin copie paralelă** | DELETE-cu-body + motiv obligatoriu (≤1000) — identic; diferă doar serviciul. Copie trivială. |
| 9 | `invalidare-dialog` | **PARALEL (adaptare)** | Același enum `MotivInvalidare` + „Altul"; **FĂRĂ** `confirmaCuRelatiiActive` (relațiile = Faza 7). ◆ 409-ul de revocare-in-circuit (Individual) → mesaj generic inline (fără branch de relații). |
| 10 | `publicare-mol-dialog` | **PARALEL (adaptare)** | Data publicării + **bloc condițional „individual"** (avertisment + motiv la override). Vezi §4.A. |
| 11 | Toggle „Publică pe portal" | **PARALEL (adaptare)** | Normativ = toggle simplu (ca HCL); Individual = flux override (avertisment + motiv). Vezi §4.A. |
| 12 | `semnatari-tab` | ◆ **PARALEL (majoritar NOU)** | Dispoziția **NU** gestionează semnatari manual (Emitent+Secretar derivați la creare). Reutilizăm **doar cardul de variantă semnată**; restul e nou: afișare read-only Emitent/Secretar + acțiunea **Refuz contrasemnare** + cardul „Contrasemnătură refuzată". |
| 13 | Variantă semnată (upload/download/delete) | **REUTILIZARE (pattern)** | File-handling identic (validare PDF, freeze pe latch, confirmare înainte de replace). Stă în `semnatari-tab`. |
| 14 | `comunicari-tab` + `comunicare-dialog` + `raspuns-prefect-dialog` | ◆ **PARALEL (copie sub `dispozitii/`)** — vezi §5 | Dialogurile injectează `HclService` + fac ele apelul → generalizarea curată e mai invazivă mid-feature; drift mic (ambele module complete). **CRITIC:** copia adaugă **output-ul care lipsește în HCL** (staleness). |
| 15 | `semnatar-dialog` | **ELIMINAT** | Nu există management manual de semnatari la dispoziție. |
| — | **NOU** `creare-dispozitie-dialog` | **NOU** | Dispoziția se creează stand-alone (tip + titlu + dataEmitere + emitent override); HCL se năștea din Punct. |
| — | **NOU** `refuz-contrasemnare-dialog` | **NOU** | Specific dispoziției (obiecție de legalitate, art. 197 alin. (3)). |

**Excluse din FE Faza 4** (fiecare la faza ei — a le face acum = muncă redundantă):

| Piesă HCL | Fază | Motiv |
|---|---|---|
| `relatii-tab` + `relatie-dialog` | **Faza 7** | Relații/erată între acte — pattern unificat PV/HCL/Dispoziție. |
| `anexe-tab` + `anexa-dialog` | **în afara scopului** | Dispoziția n-are documente-anexă în Faza 4 (`DispozitieDetaliiDto` n-are `documente`). |
| `registru-comunicari` (cronologic cross-act) | **Faza 5** | Registrul electronic se face unitar acolo. Butonul „Registru" din tab se **ascunde**. |
| `hcl-dashboard.service` + `hcl-urgent-widget` (card T-3) | **Faza 5** | Endpoint-ul `GET /Dispozitii/UrgentDeComunicat` există; îl consumăm în Faza 5, nu acum. |
| Portal public normative | **Faza 9** | Suprafață publică separată (decizie SSR). |

---

## 3. Adăugiri la `shared/` (enums + etichete) — ◆ rezolvat

- ◆ **`StatusActRedactional` — REDENUMIRE, nu enum paralel.** Backend-ul a unificat la Pas 1
  (`StatusHclRedactional → StatusActRedactional`, un singur enum pentru toate actele). Pe FE
  **redenumim** `StatusHclRedactional → StatusActRedactional` în `shared/enums.ts` +
  `etichetaStatusHcl → etichetaStatusActRedactional` în `shared/etichete.ts`. Motiv: un al doilea enum
  FE cu aceleași valori = drift garantat; redenumirea oglindește backend-ul (o singură sursă de adevăr).
  Valorile int rămân (`Draft=1, Numerotat=2, Semnat=3`) → JSON neschimbat.
  - ◆ **Execuție (la F1) — rename complet și curat, FĂRĂ alias.** Rename-ul tipului atinge oricum toate
    fișierele `.ts` HCL care referă enum-ul (fiecare `StatusHclRedactional.X` + adnotare de tip), deci
    alias-ul (`etichetaStatusHcl = etichetaStatusActRedactional`) ar salva doar ~2 referințe din
    `hcl-detalii.html` — economie neglijabilă pentru un nume permanent inconsecvent. Deci redenumim și
    proprietățile locale din componentele HCL + referințele din `.html`. Pas **izolat**, **grep repo-wide**
    (`shared/enums.ts`+`shared/etichete.ts` îl importă), **`.ts` ȘI `.html`**, cu HCL verde (build + lint +
    smoke) ca plasă — schimbarea e pur mecanică, iar build-ul + AOT prind orice referință scăpată.
    *Fallback (doar dacă vrei churn zero pe HCL acum):* nu redenumi — dispoziția importă
    `StatusHclRedactional` ca atare, redenumirea = cleanup ulterior. NU folosim varianta cu alias pe jumătate.
- **`TipDispozitie`** (nou, separat de `TipHcl`): `Normativ=1, Individual=2` + `etichetaTipDispozitie`.
  ◆ Corect enum nou (backend ține `TipDispozitie` separat de `TipHcl`).
- **`RolSemnatarDispozitie`** (nou): `Emitent=1, SecretarContrasemnatura=2` + `etichetaRolSemnatarDispozitie`
  („Emitent (primar)" / „Secretar general (contrasemnătură)").
- **Reutilizate ca atare** (valori identice, deja pe FE): `MotivInvalidare`, `CanalTransmiterePrefect`,
  `RaspunsPrefect`.
- **Eticheta motivului de invalidare pe DETALIU:** folosim `motivInvalidareEticheta` **venit de la
  backend** (label act-aware: „Revocat de primar (emitent)" / „Abrogată prin dispoziție ulterioară") — NU
  `etichetaMotivInvalidare` locală (care spune „Retractat"/„Abrogat prin HCL ulterior", greșit pe dispoziție).
- ◆ **Pe LISTĂ:** slim `DispozitieDto` **NU** poartă `motivInvalidareEticheta` (are doar `motivInvalidare`
  enum + `dataInvalidare`). Deci pe listă afișăm **doar un badge „Invalidat"** (fără text de motiv), ca să
  nu folosim eticheta locală greșită. E strict peste HCL (lista HCL n-are marcaj de invalidare) — ieftin.
- ◆ **Constantă GDPR partajată** (`shared/`): textul de avertisment la publicarea unei individuale
  („act de personal, posibil date cu caracter personal — anonimizarea rămâne sarcina ta") — o singură
  sursă, folosită de dialogul de portal-individual + blocul condițional din `publicare-mol-dialog` (§4.A).

---

## 4. UX specific dispoziției (diferă de HCL — tratat explicit)

### A. Publicarea unei INDIVIDUALE = override deliberat (soft-409 + confirmare + motiv)

Cunoaștem `tipDispozitie` client-side → **ramificăm proactiv** (nu așteptăm întâi un 409):

- **Normativ:** „Publică pe portal" = toggle simplu (ca HCL, `comutaPublicare`). „Publică în MOL" = dialogul
  de dată simplu.
- **Individual:** ambele căi trec printr-un **avertisment + motiv obligatoriu** (textul = ◆ constanta GDPR
  partajată din §3):
  - *Portal:* la click pe „Publică pe portal" → deschidem un dialog de avertisment cu câmp **Motiv**
    (obligatoriu) → `publica(id, true, { confirmaPublicareIndividuala: true, motiv })`.
  - *MOL:* `publicare-mol-dialog` primește un **bloc condițional** (afișat doar dacă `tip===Individual`) cu
    același avertisment + Motiv, trimise ca `confirmaPublicareIndividuala` + `motiv`.
- **Plasă defensivă:** tratăm și soft-409-ul backend (`{ mesaj, necesitaConfirmarePublicareIndividuala:true }`)
  în caz că ajungem acolo — pe 409 fără branch structurat, mesajul cade pe eroarea inline generică.
- **Depublicarea** (Individual) e liberă (backend gatează doar când `estePublicat=true`) → „Retrage de pe
  portal" rămâne toggle simplu la ambele tipuri.

**Recomandare:** dialogul de portal-individual (avertisment + motiv obligatoriu ≤1000) e structural identic
cu `anulare-mol-dialog` → ◆ **adaptăm forma aceluia**, nu o componentă complet nouă (o singură formă de
validare). `publicare-mol-dialog` include blocul condițional. ◆ Ambele dialoguri au nevoie de `tipDispozitie`
în `MAT_DIALOG_DATA` (date dialog = `{ dispozitieId, tip }`) ca să deseneze condiționat blocul individual.
Ambele citesc constanta GDPR partajată (§3) + validează motivul la fel.

### B. Ireversibilitatea latch-ului (`aIntratInCircuit`)

Oglindă strictă a HCL (`poateInlocuiSemnat`/`poateStergeSemnat = !aIntratInCircuit`):
- După intrarea în circuit (MOL / comunicare la prefect): **Înlocuiește / Șterge** varianta semnată devin
  indisponibile → afișăm o notă explicativă în cardul variantei semnate („Dispoziție intrată în circuit —
  varianta semnată e înghețată definitiv. Corecțiile se fac prin erată, Faza 7.").
- **„Anulează MOL"** dispare din meniu odată ce există comunicări la prefect (`poateAnulaMol = !areComunicari`)
  — dialogul returnează oricum 409 dacă e forțat; îl ascundem proactiv + tratăm 409-ul defensiv.
- ◆ **Staleness:** latch-ul e aprins de tab-ul Comunicări → antetul + `semnatari-tab` trebuie reîmprospătate
  după add/delete de comunicare (altfel butoanele de variantă semnată/„Anulează MOL" rămân stale → 409).
  Fix = output din `comunicari-tab` → hub `incarca()` (vezi §5 + §6).

### C. Contrasemnătură REFUZATĂ (primarul emite pe răspundere proprie)

- Câtă vreme `status != Semnat` și `!contrasemnaturaRefuzata` (rol Admin/Secretar) → acțiune
  **„Refuză contrasemnarea"** (în `semnatari-tab`) → `refuz-contrasemnare-dialog` cu **obiecție de legalitate**
  (obligatorie, ≤2000) → `POST {id}/RefuzContrasemnare` → detalii reîntoarse (rândul de secretar soft-șters,
  `contrasemnaturaRefuzata=true`).
- După refuz: tab-ul afișează Emitent + un card **„Contrasemnătură refuzată"** (obiecția + data), iar garda
  client de semnare (`semnatariCompletiDispozitie`) permite acum semnarea (emitent + refuz motivat).
- ◆ **Fără autor pe ecran:** `DispozitieDetaliiDto` **NU** expune `RefuzContrasemnareDe` (nici `InvalidatDe`,
  nici `PublicataDe`). Nu extindem DTO-ul — autorul e deja în log-ul imutabil de audit (`IstoricActiuneAct`),
  iar defer-to-secretar nu cere autor pe ecran (paritar HCL, care nici el nu afișează autori).
- Nu există „undo refuz" (endpoint inexistent) — acțiune one-way până la semnare; o comunicăm în confirmarea
  dialogului.

### D. Emitent = viceprimar înlocuitor (override „p. Primar")

- `creare-dispozitie-dialog` are un câmp **„Emitent înlocuitor de drept (viceprimar)"** →
  `emitentConsilierId`. Când e gol, backend-ul derivă primarul din `CinEPrimarulLa(dataEmitere)`.
- ◆ **Tratarea 400-urilor de la `Creeaza` — fără string-matching.** Backend-ul întoarce ambele cazuri ca
  `BadRequest("text")` (primar lipsă / secretar lipsă). Parsarea mesajului românesc e fragilă → în loc de
  asta: câmpul de emitent-înlocuitor e **mereu vizibil** (opțional) în dialog, iar pe **orice** 400
  afișăm **mesajul brut de la backend inline**. Utilizatorul fie adaugă un emitent și reîncearcă, fie merge
  la „Funcții oficiale" (mesajul îi spune care).
- Selectorul de consilier: `ConsilieriService.lista()` (există, întoarce toți consilierii). **Recomandare:**
  listăm toți consilierii cu eticheta „(viceprimar înlocuitor de drept)"; filtrarea strictă la viceprimari
  (via mandate) = enhancement ulterior, nu blocant (backend acceptă orice consilier).

### E. Dispoziția de convocare (Draft Individual legat de o ședință)

- Se creează **automat de backend** la trimiterea convocării (`sedintaId != null`). FE-ul o **afișează**, nu o
  creează:
  - în **listă**: badge „Convocare" (`sedintaId != null`);
  - în **detaliu**: rând „Dispoziție de convocare — ședința #{sedintaId}" cu **link** la `/sedinte/:id`.
- Fără publicare automată în MOL; apare ca orice Draft Individual în fluxul normal (numerotare + semnare).
  Notă: fiind Draft (fără `AnNumerotare`), **nu apare sub filtrul de an** din listă până e numerotată
  (comportament paritar HCL — backend filtrează pe `AnNumerotare`).

---

## 5. ◆ Decizia „Comunicări prefect" — PARALEL (copie), generalizarea = cleanup ulterior

Tab-ul + cele 2 dialoguri de comunicări operează peste 4 metode CRUD + un DTO cvasi-identic
(`ComunicareDispozitiePrefectDto` = `ComunicareHclPrefect` cu `dispozitieId` în loc de `hclId`).
Generalizarea (`SursaComunicariPrefect`) părea curată, dar în cod: **dialogurile injectează `HclService`
și fac ele apelul HTTP**, nu doar tab-ul. Ca să generalizezi curat ar trebui ori să treci sursa prin
`MAT_DIALOG_DATA` (un service ca obiect — neobișnuit), ori să refactorezi dialogurile să devină „proaste"
(întorc form-values, tab-ul cheamă sursa) = atingi și mai mult cod HCL mid-feature.

**Recomandare: copie paralelă sub `dispozitii/`** (tab + 2 dialoguri). Endpoint-urile diferă doar prin URL;
driftul e mic fiindcă ambele module sunt complete. Generalizarea rămâne un **cleanup dedicat opțional**
(sesiune separată, cu fluxul HCL de comunicări ca plasă).

- ◆ **CRITIC — copia NU e oarbă:** `comunicari-tab` din HCL **nu are `output`** → prima comunicare aprinde
  `AIntratInCircuit` pe backend, dar hub-ul rămâne cu `actiuni()` stale → butoanele de variantă semnată /
  „Anulează MOL" rămân active → **409** (exact ce interzice `docs/frontend.md`). Copia paralelă pentru
  dispoziție **adaugă un `output`** (ex. `schimbat`) → hub-ul face `incarca()`.
  **Pe adăugare ȘI pe ștergere:** la ștergerea ULTIMEI comunicări, `poateAnulaMol` trebuie să redevină
  posibil (`areComunicari→false` pe backend), în timp ce latch-ul rămâne (nu se resetează). Deci refetch-ul
  contează pe ambele mutații.
- Butonul „Registru" se **ascunde** (registrul cronologic cross-act = Faza 5).

**`atribuie-numar-dialog` + `anulare-mol-dialog`:** ◆ tot **copie paralelă** (trivială — diferă strict
serviciul apelat), nu parametrizare cu token/`input`.

---

## 6. Oglinda de permisiuni `dispozitii.permisiuni.ts` (gărzi client = oglindă backend)

`ActiuniDispozitie` (mirror al gărzilor din `DispozitiiController`):

- Redactare: `poateEditaContinut` / `poateRegenera` (Admin/Secretar + `!Semnat`), `poateAtribuiNumar`
  (Admin/Secretar + `Draft`), `poateSemna` (Admin/Secretar + `Numerotat` + `semnatariCompletiDispozitie`),
  `poateDescarcaPdf` (oricine), `esteReadOnly` (`!Admin/Secretar`).
- Stări legale: `poatePublica`/`poateDepublica` (după `EstePublicat` + `Status>=Numerotat`),
  `poatePublicaMol` (`Semnat` + fără MOL), `poateAnulaMol` (**Admin** + are MOL + `!areComunicari`),
  `poateInvalida` (`!invalidat`), `poateAnulaInvalidare` (**Admin** + invalidat).
- Semnatari/refuz: `poateRefuzaContrasemnare` (Admin/Secretar + `!Semnat` + `!contrasemnaturaRefuzata`) —
  **înlocuiește** `poateGestionaSemnatari` din HCL.
- Variantă semnată (latch): `poateIncarcaSemnat` / `poateInlocuiSemnat` / `poateStergeSemnat` /
  `poateDescarcaSemnat` — identic HCL (`Semnat`, fișier, `!aIntratInCircuit`, Admin la ștergere).
- ◆ **Două capcane de nume (aceleași ca la HCL) — de fixat din start, altfel oglinda driftează tăcut:**
  (1) `esteSemnat` (bool DTO = are PDF semnat atașat) **≠** `status === Semnat` (ciclu de viață); DTO-ul are
  ambele câmpuri distinct. (2) `poateIncarcaSemnat` **NU** verifică `!aIntratInCircuit` — prima atașare e
  permisă și după intrarea în circuit (gardă backend: freeze doar când există DEJA fișier,
  `DispozitiiController.cs:336`; doar `poateInlocui`/`poateSterge` verifică latch-ul). Un mirror naiv care
  adaugă latch-ul la Încarcă rupe upload-ul legitim post-circuit.
- ◆ `poateSterge` (act întreg): boolean exact din matricea controllerului (liniile 547-571):
  **`Admin && !areComunicari && (invalidat || (!Semnat && !Publicat))`**. Atenție la precedență: invalidat
  → OK **chiar dacă e Semnat/Publicat** (un mirror naiv `!Semnat && !Publicat` ar ascunde greșit butonul pe
  o dispoziție invalidată-și-semnată). Se expune în meniul ⋮, cu confirmare `periculos: true`.
- `semnatariCompletiDispozitie(d)`: exact 1 Emitent + (1 SecretarContrasemnatura **SAU**
  `contrasemnaturaRefuzata && obiectieLegalitateSecretar`). Oglindă a gărzii `Semneaza`. Notă: rândul de
  secretar soft-șters la refuz **nu** apare în `semnatari` (filtrul global îl exclude) → se numără corect.

`ActiuniComunicari` (mirror `ComunicariDispozitiiPrefectController`): `poateAdauga` (Admin/Secretar +
`Status>=Numerotat`), `poateEdita` (Admin/Secretar), `poateSterge` (Admin) — identic HCL.

◆ **Staleness:** `comunicari-tab` emite `output` → hub `incarca()` pe add/delete (vezi §5) — ca `actiuni()`
și `semnatari-tab` să vadă latch-ul proaspăt.

---

## 7. Construcția pe pași (FE) — context mic, verificare la fiecare pas

**Cadență de verificare (FE):** după fiecare pas — `npm run build` + `npm run lint` curat + **smoke manual**
în app pe fluxul atins (rulez eu, îți arăt rezultatul). Pentru logica pură recomand **un spec Vitest** pe
`dispozitii.permisiuni.ts` (oglinda gărzilor — high-value, ieftin). ◆ Specul fixează explicit **exact
locurile unde oglinda driftează fără eroare de compilare**: `poateIncarcaSemnat` permis post-circuit (fără
latch); `esteSemnat` vs `status===Semnat`; `poateSterge` = true pe invalidat+semnat; `semnatariCompleti` pe
calea de refuz (secretar soft-șters + obiecție). NU forțăm test-per-componentă (nu e cultura FE aici).

### Faza FE-A — Fundație (fără ecrane)
- **F1 — ◆ redenumire enum (complet, fără alias) + enums/etichete noi.** Redenumire
  `StatusHclRedactional → StatusActRedactional` (+ `etichetaStatusHcl → etichetaStatusActRedactional`),
  **complet** (inclusiv proprietăți locale HCL + referințe `.html`), **izolat, grep repo-wide `.ts`+`.html`,
  HCL verde** (§3). Apoi enum-urile noi `TipDispozitie`, `RolSemnatarDispozitie` + etichetele + constanta
  GDPR partajată. Checkpoint: build + lint + smoke HCL (dovadă zero regresie pe redenumire).
- **F2 — models + service + permisiuni.** `dispozitii.models.ts` (oglindă `DispozitieDto` /
  `DispozitieDetaliiDto` / `SemnatarDispozitieDto` / `ComunicareDispozitiePrefectDto`), `dispozitii.service.ts`
  (toate endpoint-urile din §8), `dispozitii.permisiuni.ts` (§6). Checkpoint: build + lint + spec permisiuni.
- **F3 — rută + nav.** `/dispozitii` + `/dispozitii/:id` (`canDeactivate: [ghidModificariNesalvate]` pe
  detaliu; segmente fixe înaintea `:id`). Element de meniu „Dispoziții" în `shell` (lângă „Hotărâri";
  icon propus: `description`). Checkpoint: navigarea încarcă un shell gol.

### Faza FE-B — Listă + creare + hub schelet
- **F4 — `dispozitii-lista`.** Filtre an/status/**tip Normativ|Individual** (server-side, re-fetch), căutare
  client-side (titlu/număr). ◆ Badge-uri: tip + status + „Invalidat" (mic, fără text motiv) + indicator
  „Convocare". Checkpoint: listă + filtre.
- **F5 — `creare-dispozitie-dialog`.** Tip + Titlu + DataEmitere (default azi) + emitent override
  **mereu vizibil**; ◆ pe orice 400 → mesaj brut inline, fără string-matching (§4.D) → navighează la
  `/dispozitii/:id`. Checkpoint: creare + cele 2 căi de eroare (primar/secretar lipsă).
- **F6 — `dispozitie-detalii` (schelet).** Antet cu badge-uri (Tip / Status / Invalidat / Publicat /
  Publicat MOL / Convocare / Contrasemnătură refuzată) + acțiuni (Atribuie număr / Semnează / PDF); tab
  **Detalii** (`<dl>`) + tab **Conținut** (editor auto-save, reuse pattern). ◆ NU afișa `DataIntrareInVigoare`
  (backend nu-l setează — mereu null). Reuse `atribuie-numar-dialog` (copie, §5). ◆ **Prerechizită de date
  dev:** calea semnabilă cere mandat de primar ȘI de secretar valide „azi" (altfel `Creeaza` dă 400) — de
  confirmat mandatele seedate înainte de smoke. Checkpoint: Creează → editează → numerotează → PDF.

### Faza FE-C — Semnatari + refuz + variantă semnată
- **F7 — `semnatari-tab`.** Afișare read-only Emitent + Secretar/refuz (card „Contrasemnătură refuzată" =
  obiecție + dată, **fără autor**); secțiunea **variantă semnată** (upload/download/delete, freeze pe latch —
  reuse cardul). Checkpoint: upload/replace/delete + freeze.
- **F8 — refuz + semnare.** `refuz-contrasemnare-dialog` + garda client `semnatariCompletiDispozitie` +
  butonul Semnează. Checkpoint: refuz → semnare peste refuz; și calea normală (cu contrasemnătură).

### Faza FE-D — Stări legale
- **F9 — invalidare.** `invalidare-dialog` (adaptat, fără relatii) + „Anulează invalidarea" în meniul ⋮.
  Eticheta motivului = `motivInvalidareEticheta` de la backend. ◆ 409-ul de revocare-in-circuit (Individual) →
  mesaj generic inline. Checkpoint: invalidare cu „Altul" + revocare (409 pe Individual în circuit) + anulare.
- **F10 — publicare + MOL + delete.** Toggle portal (Normativ direct / Individual dialog cu constanta GDPR) +
  `publicare-mol-dialog` (cu bloc individual) + `anulare-mol-dialog` (copie, §5) + acțiunea Delete
  (◆ boolean exact din §6). Checkpoint: Normativ liber; Individual cu avertisment+motiv; MOL + latch +
  „Anulează MOL" (ascuns după comunicare); Delete pe draft/invalidat.

### Faza FE-E — Comunicări prefect
- **F11 — comunicări.** ◆ Copie paralelă sub `dispozitii/` (tab + 2 dialoguri, §5) **CU output-ul de
  staleness** (emit pe add+delete → hub `incarca()`); tab „Comunicări" în hub; butonul Registru ascuns.
  Checkpoint: CRUD comunicare + răspuns prefect + **latch-ul se reflectă imediat în antet/semnatari** +
  re-smoke flux HCL de comunicări.

### Faza FE-F — Polish convocare
- **F12 — convocare.** Badge „Convocare" + link la ședință în listă și detaliu (§4.E). Checkpoint: o dispoziție
  de convocare (creată de backend la trimiterea convocării) se afișează corect + link funcțional.

---

## 8. Suprafața de backend → metode de serviciu (mapare FE → API)

`DispozitiiController` (`/api/Dispozitii`):
- `GET ?an&status&tip&skip&take` → `lista(filtre)`; `GET {id}` → `detalii(id)`; `GET {id}/Pdf` (blob) →
  `descarcaPdf(id)`.
- `POST` → `creeaza(dto)` (◆ întoarce `DispozitieDetaliiDto` complet, nu slim); `PUT {id}/Continut` →
  `editeazaContinut`; `POST {id}/RegenereazaContinut` → `regenereazaContinut`.
- `GET {id}/SugestieNumar` → `sugestieNumar`; `POST {id}/AtribuieNumar` → `atribuieNumar` (409 lacune /
  număr luat, tratat în dialog).
- `POST {id}/Semneaza` → `semneaza`; `POST {id}/RefuzContrasemnare` → `refuzaContrasemnare(id, obiectie)`.
- `POST {id}/Semnat` (multipart) → `incarcaSemnat`; `GET {id}/Semnat` (blob) → `descarcaSemnat`;
  `DELETE {id}/Semnat` → `stergeSemnat`.
- `POST {id}/Invalidare` → `invalideaza`; `DELETE {id}/Invalidare` → `anuleazaInvalidare`.
- `PUT {id}/Publicare` → `publica(id, estePublicat, confirma?, motiv?)`; `PUT {id}/PublicareMol` →
  `publicaMol(id, data, confirma?, motiv?)`; `DELETE {id}/PublicareMol` (body `{motiv}`) → `anuleazaMol`.
- `DELETE {id}` → `sterge(id)` (§6 — boolean exact).

`ComunicariDispozitiiPrefectController` (`/api/Dispozitii/{id}/Comunicari`): `GET/POST/PUT/DELETE` →
`lista/adauga/actualizeaza/sterge` (◆ copie paralelă în `dispozitii.service.ts`, nu sursă generică).

`DispozitiiDashboardController` (`GET /api/Dispozitii/UrgentDeComunicat?prag`): **NU se consumă în Faza 4**
(card T-3 = Faza 5).

**Capcane de mapare (din rezumat S60 + ◆ recenzie):**
- `DataEmitere` request = `DateOnly` („yyyy-MM-dd", din `<input type="date">`, fără conversie de fus);
  răspuns = `DateTime` (afișare cu `formateazaDataOra`). `DataPublicareMol` = `DateOnly`.
- Publicarea individualelor răspunde **soft-409** `{ mesaj, necesitaConfirmarePublicareIndividuala:true }`
  (NU 409 dur) — se retrimite cu `confirmaPublicareIndividuala=true` + `motiv`.
- `AtribuieNumar` 409: `{mesaj, lacune}` sau `{mesaj, sugestieAlternativa}` (paritar HCL — reuse dialog).
- Mutațiile întorc `DispozitieDetaliiDto` complet → set direct în hub (un singur round-trip).
- ◆ `DispozitieDetaliiDto` **NU** expune autori (`RefuzContrasemnareDe`/`InvalidatDe`/`PublicataDe`) →
  nu afișăm autori (paritar HCL). `DataIntrareInVigoare` e în DTO dar **nu e setat de controller** → mereu
  null, nu construim UI pe el.

---

## 9. ◆ Decizii — REZOLVATE

1. **Numele enum-ului de status pe FE.** **REZOLVAT: redenumire completă, fără alias**
   `StatusHclRedactional → StatusActRedactional` (+ eticheta + proprietăți locale HCL + referințe `.html`),
   izolat la F1, grep repo-wide `.ts`+`.html`, HCL verde. Oglindește backend-ul (unificat la Pas 1) → o
   singură sursă. NU enum paralel (drift), NU alias pe jumătate (nume inconsecvent). *Fallback:* dacă vrei
   churn zero pe HCL acum, nu redenumi deloc (dispoziția importă `StatusHclRedactional`) + cleanup ulterior.
2. **Comunicări: generalizare vs. paralel.** **REZOLVAT: paralel** (copie sub `dispozitii/`), fiindcă
   dialogurile injectează `HclService` + fac ele apelul → generalizarea e mai invazivă mid-feature; drift mic.
   **Copia adaugă output-ul de staleness** care lipsește în HCL (add+delete → hub refetch). Generalizarea
   (`SursaComunicariPrefect`) = cleanup ulterior opțional.
3. **Butonul Delete pe dispoziție.** **REZOLVAT: expus** în meniul ⋮ (Admin), util pentru curățarea
   draft-urilor greșite (tip greșit). Boolean exact: `Admin && !areComunicari && (invalidat || (!Semnat &&
   !Publicat))` — atenție la precedența „invalidat → OK chiar pe semnat".
4. **Selectorul de emitent înlocuitor.** **REZOLVAT: toți consilierii** cu etichetă clară, **mereu vizibil**;
   pe 400 → mesaj brut inline, **fără string-matching**. Filtrarea la viceprimari (via mandate) = enhancement
   ulterior.

**În afara Fazei 4 (de urmărit ca tichete separate):**
- Bug-ul geamăn din HCL — `comunicari-tab` HCL n-are output → aceeași scăpare de staleness (latch fără
  refetch la părinte). Task dedicat, nu aici.
- ◆ **Authz GDPR pe individuale (backend, preexistent):** `GET {id}` / `{id}/Pdf` / `{id}/Semnat` n-au
  gardă de rol — orice utilizator autentificat (inclusiv un consilier) poate deschide o dispoziție de
  personal + PDF-ul ei semnat. `research_dispozitii.md` §8 cere control de acces intern pe individuale.
  FE-ul doar oglindește backend-ul (nu introduce el problema), dar merită un tichet de authz backend.

---

## 10. Filozofie de implementare (din planurile anterioare, încă validă)

Planul e ghid, nu script. La fiecare piesă: citesc componenta/serviciul HCL de paritate, identific pattern-ul,
adaptez la Dispoziție. Verific build + lint + smoke după **fiecare** pas, nu doar la final. Copiile paralele
(comunicări, atribuie-numar, anulare-mol) le fac cu fluxul HCL existent ca referință — și **adaug output-ul
de staleness** unde HCL îl ratează (nu copiem bug-uri). Regula de aur: **oglindesc fidel gărzile backend în
`permisiuni.ts`** (driftul = bug) și **tratez explicit ce e specific dispoziției** (normativ vs. individual,
latch, refuz, emitent înlocuitor, convocare) — restul e paritate HCL disciplinată.
