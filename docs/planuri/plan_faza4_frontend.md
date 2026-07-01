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

---

## 2. Harta de paritate HCL → Dispoziție (REUTILIZARE vs. PARALEL) — recomandat, cu motiv scurt

**Principiu:** reutilizăm ce e *identic ca mecanică și date* (numerotare, „Anulează MOL", comunicări prefect,
varianta semnată); paralelizăm ce *diferă ca formă de DTO sau ca reguli de domeniu* (listă, hub, semnatari,
publicare); excludem ce ține de altă fază. Rule-of-two: generalizăm la a 2-a folosire doar unde abstracția e
curată (prezentare peste CRUD), NU unde ar fi leaky.

| # | Piesă HCL | Decizie | Motiv scurt |
|---|---|---|---|
| 1 | `hcl.models.ts` | **PARALEL** `dispozitii.models.ts` | DTO diferit: fără vot/majoritate/punctOrdineZi; cu `tipDispozitie`, emitent/contrasemnătură, `contrasemnaturaRefuzata`+`obiectie`, `sedintaId`, `motivInvalidareEticheta`. |
| 2 | `hcl.service.ts` | **PARALEL** `dispozitii.service.ts` | Altă bază URL + endpoint-uri proprii (`RefuzContrasemnare`; FĂRĂ `Semnatari` CRUD, FĂRĂ `Relatii`). |
| 3 | `hcl.permisiuni.ts` | **PARALEL** `dispozitii.permisiuni.ts` | Oglindă a gărzilor dispoziției: `poateRefuzaContrasemnare` în loc de `poateGestionaSemnatari`; completitudinea la semnare = emitent + (contrasemnătură **SAU** refuz motivat). |
| 4 | `hcl-lista` (+filtre) | **PARALEL** `dispozitii-lista` (adaptare) | Aceeași structură; filtru tip = **Normativ/Individual**, badge de tip, buton **„Creează dispoziție"** (HCL n-are — se naște din Punct), indicator „Convocare". |
| 5 | `hcl-detalii` (hub) | **PARALEL** `dispozitie-detalii` (adaptare) | Același schelet (antet + editor + tab-uri), dar **4 tab-uri** (Detalii / Conținut / Semnatari / Comunicări), alte badge-uri + acțiuni. |
| 6 | Editor auto-save (în hub) | **REUTILIZARE (pattern)** | Mecanica dirty/lock/auto-save e identică; infra e deja în `core/`. Copiem structura, nu inventăm. |
| 7 | `atribuie-numar-dialog` | **REUTILIZARE (cvasi-verbatim)** | Logica sugestie + lacune + „număr luat" e identică; diferă doar serviciul. Vezi §5 (parametrizare service). |
| 8 | `anulare-mol-dialog` | **REUTILIZARE (logică identică)** | DELETE-cu-body + motiv obligatoriu (≤1000) — identic; diferă doar serviciul. |
| 9 | `invalidare-dialog` | **PARALEL (adaptare)** | Același enum `MotivInvalidare` + „Altul"; **FĂRĂ** `confirmaCuRelatiiActive` (relațiile = Faza 7). |
| 10 | `publicare-mol-dialog` | **PARALEL (adaptare)** | Data publicării + **bloc condițional „individual"** (avertisment + motiv la override). |
| 11 | Toggle „Publică pe portal" | **PARALEL (adaptare)** | Normativ = toggle simplu (ca HCL); Individual = flux override (avertisment + motiv). Vezi §4.A. |
| 12 | `semnatari-tab` | **PARALEL (adaptare grea)** | Dispoziția **NU** gestionează semnatari manual (Emitent+Secretar derivați la creare); tab-ul afișează read-only + acțiunea **Refuz contrasemnare** + secțiunea variantă semnată. |
| 13 | Variantă semnată (upload/download/delete) | **REUTILIZARE (pattern)** | File-handling identic (validare PDF, freeze pe latch, confirmare înainte de replace). Stă în `semnatari-tab`. |
| 14 | `comunicari-tab` + `comunicare-dialog` + `raspuns-prefect-dialog` | **REUTILIZARE prin generalizare** (recomandat) — vezi §5 | CRUD identic peste un DTO cvasi-identic; abstracția e curată (prezentare peste 4 metode). Alternativă acceptată: paralel. |
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

## 3. Adăugiri la `shared/` (enums + etichete) — recomandat

- **`StatusActRedactional`** (nou în `shared/enums.ts`): `Draft=1, Numerotat=2, Semnat=3`. **Recomandare:**
  enum nou paralel (NU reutilizăm `StatusHclRedactional` prin nume, deși valorile coincid) — dispoziția
  rămâne self-contained și lizibilă, fără să scurgem „Hcl" în cod. Cost: 2 enum-uri cu aceleași valori
  (acceptabil — e același lifecycle juridic). + `etichetaStatusActRedactional` (Draft/Numerotat/Semnat).
- **`TipDispozitie`** (nou): `Normativ=1, Individual=2` + `etichetaTipDispozitie`.
- **`RolSemnatarDispozitie`** (nou): `Emitent=1, SecretarContrasemnatura=2` + `etichetaRolSemnatarDispozitie`
  („Emitent (primar)" / „Secretar general (contrasemnătură)").
- **Reutilizate ca atare** (valori identice, deja pe FE): `MotivInvalidare`, `CanalTransmiterePrefect`,
  `RaspunsPrefect`.
- **Eticheta motivului de invalidare:** pe **detaliu** folosim `motivInvalidareEticheta` **venit de la
  backend** (label act-aware: „Revocat de primar (emitent)" / „Abrogată prin dispoziție ulterioară") — NU
  `etichetaMotivInvalidare` locală (care spune „Retractat"/„Abrogat prin HCL ulterior", greșit pe dispoziție).
  Reuse curat: backend-ul a rezolvat deja remaparea; FE doar afișează câmpul.

---

## 4. UX specific dispoziției (diferă de HCL — tratat explicit)

### A. Publicarea unei INDIVIDUALE = override deliberat (soft-409 + confirmare + motiv)

Cunoaștem `tipDispozitie` client-side → **ramificăm proactiv** (nu așteptăm întâi un 409):

- **Normativ:** „Publică pe portal" = toggle simplu (ca HCL, `comutaPublicare`). „Publică în MOL" = dialogul
  de dată simplu.
- **Individual:** ambele căi trec printr-un **avertisment + motiv obligatoriu**:
  - *Portal:* la click pe „Publică pe portal" → deschidem un dialog de avertisment („Dispoziție cu caracter
    individual — act de personal, posibil date cu caracter personal. **Anonimizarea rămâne sarcina ta.**") cu
    câmp **Motiv** (obligatoriu) → `publica(id, true, { confirmaPublicareIndividuala: true, motiv })`.
  - *MOL:* `publicare-mol-dialog` primește un **bloc condițional** (afișat doar dacă `tip===Individual`) cu
    același avertisment + Motiv, trimise ca `confirmaPublicareIndividuala` + `motiv`.
- **Plasă defensivă:** tratăm și soft-409-ul backend (`{ necesitaConfirmarePublicareIndividuala: true }`) în
  caz că ajungem acolo — paritar cu `invalidare-dialog` (catch 409 → avertisment inline → retry cu flag).
- **Depublicarea** (Individual) e liberă → „Retrage de pe portal" rămâne toggle simplu la ambele tipuri.

**Recomandare:** un singur dialog reutilizabil `publicare-individuala-dialog` (avertisment + motiv) pentru
calea „portal"; iar `publicare-mol-dialog` include blocul condițional. Motiv: un singur loc pentru textul
GDPR + o singură validare de motiv.

### B. Ireversibilitatea latch-ului (`aIntratInCircuit`)

Oglindă strictă a HCL (`poateInlocuiSemnat`/`poateStergeSemnat = !aIntratInCircuit`):
- După intrarea în circuit (MOL / comunicare la prefect): **Înlocuiește / Șterge** varianta semnată devin
  indisponibile → afișăm o notă explicativă în cardul variantei semnate („Dispoziție intrată în circuit —
  varianta semnată e înghețată definitiv. Corecțiile se fac prin erată, Faza 7.").
- **„Anulează MOL"** dispare din meniu odată ce există comunicări la prefect (`poateAnulaMol = !areComunicari`)
  — dialogul returnează oricum 409 dacă e forțat; îl ascundem proactiv + tratăm 409-ul defensiv.

### C. Contrasemnătură REFUZATĂ (primarul emite pe răspundere proprie)

- Câtă vreme `status != Semnat` și `!contrasemnaturaRefuzata` (rol Admin/Secretar) → acțiune
  **„Refuză contrasemnarea"** (în `semnatari-tab`) → `refuz-contrasemnare-dialog` cu **obiecție de legalitate**
  (obligatorie, ≤2000) → `POST {id}/RefuzContrasemnare` → detalii reîntoarse (rândul de secretar soft-șters,
  `contrasemnaturaRefuzata=true`).
- După refuz: tab-ul afișează Emitent + un card **„Contrasemnătură refuzată"** (obiecția + data + autorul),
  iar garda client de semnare (`semnatariCompletiDispozitie`) permite acum semnarea (emitent + refuz motivat).
- Nu există „undo refuz" (endpoint inexistent) — acțiune one-way până la semnare; o comunicăm în confirmarea
  dialogului.

### D. Emitent = viceprimar înlocuitor (override „p. Primar")

- `creare-dispozitie-dialog` are un câmp opțional **„Emitent înlocuitor de drept (viceprimar)"** →
  `emitentConsilierId`. Când e gol, backend-ul derivă primarul din `CinEPrimarulLa(dataEmitere)`.
- **Tratarea 400-urilor de la `Creeaza`:**
  - „Nu există primar valid la data emiterii..." → afișăm inline + **revelăm/încurajăm** selectorul de emitent
    înlocuitor (sau trimitem la „Funcții oficiale").
  - „Nu există Secretar UAT valid..." → inline, fără override posibil (substitutul consilier-juridic e amânat) →
    trimitem la „Funcții oficiale".
- Selectorul de consilier: `ConsilieriService.lista()` (există). **Recomandare:** listăm toți consilierii cu
  eticheta „(viceprimar înlocuitor de drept)"; filtrarea strictă la viceprimari (via mandate) = enhancement
  ulterior, nu blocant (backend acceptă orice consilier).

### E. Dispoziția de convocare (Draft Individual legat de o ședință)

- Se creează **automat de backend** la trimiterea convocării (`sedintaId != null`). FE-ul o **afișează**, nu o
  creează:
  - în **listă**: badge „Convocare" + (opțional) subtitlu „ședința #{sedintaId}";
  - în **detaliu**: rând „Dispoziție de convocare — ședința #{sedintaId}" cu **link** la `/sedinte/:id`.
- Fără publicare automată în MOL; apare ca orice Draft Individual în fluxul normal (numerotare + semnare).

---

## 5. Decizia de generalizare a „Comunicări prefect" (recomandat) + numerotare

Tab-ul + cele 2 dialoguri de comunicări operează pur peste 4 metode CRUD + un DTO cvasi-identic
(`ComunicareDispozitiePrefectDto` = `ComunicareHclPrefect` cu `dispozitieId` în loc de `hclId`; corpurile de
creare/actualizare sunt deja act-neutre pe backend). **Recomandare: generalizare la a 2-a folosire.**

- Introducem un model neutru `ComunicarePrefect` (fără id-ul actului părinte — tab-ul nu-l folosește) + o
  interfață mică `SursaComunicariPrefect` (`lista/adauga/actualizeaza/sterge`), implementată de `HclService`
  **și** `DispozitiiService`.
- Mutăm `comunicari-tab` + `comunicare-dialog` + `raspuns-prefect-dialog` într-un loc partajat
  (`features/comunicari-prefect/` sau `shared/`); tab-ul primește `actId` + sursa ca `input`.
- **Motiv:** evită ~350 de linii duplicate + driftul (un bug reparat o dată); abstracția NU e leaky (spre
  deosebire de backend, unde comunicarea a rămas paralelă din motive de concurență — aici e doar prezentare).
- **Costul conștient:** atinge cod HCL existent → re-verificăm fluxul HCL de comunicări după refactor
  (build + lint + smoke). Butonul „Registru" se ascunde când sursa e dispoziție (registrul = Faza 5).
- **Fallback acceptat** (dacă vrem context mic strict): copie paralelă sub `dispozitii/`. O marchez ca a
  doua opțiune, nu prima.

**`atribuie-numar-dialog` + `anulare-mol-dialog`:** aceeași alegere în mic — fie le parametrizăm cu serviciul
(un `input`/token), fie copie paralelă. Recomand parametrizare cu serviciul (diferența e strict metoda
apelată). Dacă preferăm zero-atingere pe HCL, copie paralelă (trivială).

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
- `poateSterge` (act întreg): **Admin** + oglinda matricei de gărzi (fără comunicări; invalidat→OK;
  Semnat→blocat; Publicat→blocat). **Vezi decizia deschisă (7.3).**
- `semnatariCompletiDispozitie(d)`: exact 1 Emitent + (1 SecretarContrasemnatura **SAU**
  `contrasemnaturaRefuzata && obiectie`). Oglindă a gărzii `Semneaza`.

`ActiuniComunicari` (mirror `ComunicariDispozitiiPrefectController`): `poateAdauga` (Admin/Secretar +
`Status>=Numerotat`), `poateEdita` (Admin/Secretar), `poateSterge` (Admin) — identic HCL.

---

## 7. Construcția pe pași (FE) — context mic, verificare la fiecare pas

**Cadență de verificare (FE):** după fiecare pas — `npm run build` + `npm run lint` curat + **smoke manual**
în app pe fluxul atins (rulez eu, îți arăt rezultatul). Pentru logica pură recomand **un spec Vitest** pe
`dispozitii.permisiuni.ts` (oglinda gărzilor — high-value, ieftin). NU forțăm test-per-componentă (nu e
cultura FE aici).

### Faza FE-A — Fundație (fără ecrane)
- **F1 — enums + etichete.** `StatusActRedactional`, `TipDispozitie`, `RolSemnatarDispozitie` + etichetele
  (§3). Checkpoint: build + lint.
- **F2 — models + service + permisiuni.** `dispozitii.models.ts` (oglindă `DispozitieDto` /
  `DispozitieDetaliiDto` / `SemnatarDispozitieDto` / `ComunicareDispozitiePrefectDto`), `dispozitii.service.ts`
  (toate endpoint-urile din §8), `dispozitii.permisiuni.ts` (§6). Checkpoint: build + lint + spec permisiuni.
- **F3 — rută + nav.** `/dispozitii` + `/dispozitii/:id` (`canDeactivate: [ghidModificariNesalvate]` pe
  detaliu; segmente fixe înaintea `:id`). Element de meniu „Dispoziții" în `shell` (icon propus: `description`).
  Checkpoint: navigarea încarcă un shell gol.

### Faza FE-B — Listă + creare + hub schelet
- **F4 — `dispozitii-lista`.** Filtre an/status/**tip Normativ|Individual** (server-side, re-fetch), căutare
  client-side (titlu/număr), badge de tip + status, indicator „Convocare". Checkpoint: listă + filtre.
- **F5 — `creare-dispozitie-dialog`.** Tip + Titlu + DataEmitere (default azi) + emitent override opțional;
  tratarea 400-urilor primar/secretar (§4.D) → navighează la `/dispozitii/:id`. Checkpoint: creare + cele 2
  căi de eroare.
- **F6 — `dispozitie-detalii` (schelet).** Antet cu badge-uri (Tip / Status / Invalidat / Publicat /
  Publicat MOL / Convocare / Contrasemnătură refuzată) + acțiuni (Atribuie număr / Semnează / PDF); tab
  **Detalii** (`<dl>`) + tab **Conținut** (editor auto-save, reuse pattern). Reuse `atribuie-numar-dialog`
  (§5). Checkpoint: Creează → editează → numerotează → PDF.

### Faza FE-C — Semnatari + refuz + variantă semnată
- **F7 — `semnatari-tab`.** Afișare read-only Emitent + Secretar/refuz; secțiunea **variantă semnată**
  (upload/download/delete, freeze pe latch — reuse pattern). Checkpoint: upload/replace/delete + freeze.
- **F8 — refuz + semnare.** `refuz-contrasemnare-dialog` + garda client `semnatariCompletiDispozitie` +
  butonul Semnează. Checkpoint: refuz → semnare peste refuz; și calea normală (cu contrasemnătură).

### Faza FE-D — Stări legale
- **F9 — invalidare.** `invalidare-dialog` (adaptat, fără relatii) + „Anulează invalidarea" în meniul ⋮.
  Eticheta motivului = `motivInvalidareEticheta` de la backend. Checkpoint: invalidare cu „Altul" + revocare
  (409 pe Individual în circuit, tratat inline) + anulare.
- **F10 — publicare + MOL + delete.** Toggle portal (Normativ) + flux override individual (§4.A) +
  `publicare-mol-dialog` (cu bloc individual) + `anulare-mol-dialog` (reuse) + acțiunea Delete (§7.3).
  Checkpoint: Normativ liber; Individual cu avertisment+motiv; MOL + latch + „Anulează MOL" (ascuns după
  comunicare).

### Faza FE-E — Comunicări prefect
- **F11 — comunicări.** Generalizare `SursaComunicariPrefect` (§5) SAU paralel; tab „Comunicări" în hub;
  butonul Registru ascuns (Faza 5). Checkpoint: CRUD comunicare + răspuns prefect + re-verificare flux HCL.

### Faza FE-F — Polish convocare
- **F12 — convocare.** Badge „Convocare" + link la ședință în listă și detaliu (§4.E). Checkpoint: o dispoziție
  de convocare (creată de backend la trimiterea convocării) se afișează corect + link funcțional.

---

## 8. Suprafața de backend → metode de serviciu (mapare FE → API)

`DispozitiiController` (`/api/Dispozitii`):
- `GET ?an&status&tip&skip&take` → `lista(filtre)`; `GET {id}` → `detalii(id)`; `GET {id}/Pdf` (blob) →
  `descarcaPdf(id)`.
- `POST` → `creeaza(dto)`; `PUT {id}/Continut` → `editeazaContinut`; `POST {id}/RegenereazaContinut` →
  `regenereazaContinut`.
- `GET {id}/SugestieNumar` → `sugestieNumar`; `POST {id}/AtribuieNumar` → `atribuieNumar` (409 lacune /
  număr luat, tratat în dialog).
- `POST {id}/Semneaza` → `semneaza`; `POST {id}/RefuzContrasemnare` → `refuzaContrasemnare(id, obiectie)`.
- `POST {id}/Semnat` (multipart) → `incarcaSemnat`; `GET {id}/Semnat` (blob) → `descarcaSemnat`;
  `DELETE {id}/Semnat` → `stergeSemnat`.
- `POST {id}/Invalidare` → `invalideaza`; `DELETE {id}/Invalidare` → `anuleazaInvalidare`.
- `PUT {id}/Publicare` → `publica(id, estePublicat, confirma?, motiv?)`; `PUT {id}/PublicareMol` →
  `publicaMol(id, data, confirma?, motiv?)`; `DELETE {id}/PublicareMol` (body `{motiv}`) → `anuleazaMol`.
- `DELETE {id}` → `sterge(id)` (vezi 7.3).

`ComunicariDispozitiiPrefectController` (`/api/Dispozitii/{id}/Comunicari`): `GET/POST/PUT/DELETE` →
`lista/adauga/actualizeaza/sterge` (prin `SursaComunicariPrefect`).

`DispozitiiDashboardController` (`GET /api/Dispozitii/UrgentDeComunicat?prag`): **NU se consumă în Faza 4**
(card T-3 = Faza 5).

**Capcane de mapare (din rezumat S60):**
- `DataEmitere` request = `DateOnly` („yyyy-MM-dd", din `<input type="date">`, fără conversie de fus);
  răspuns = `DateTime` (afișare cu `formateazaDataOra`). `DataPublicareMol` = `DateOnly`.
- Publicarea individualelor răspunde **soft-409** `{ mesaj, necesitaConfirmarePublicareIndividuala:true }`
  (NU 409 dur) — se retrimite cu `confirmaPublicareIndividuala=true` + `motiv`.
- `AtribuieNumar` 409: `{mesaj, lacune}` sau `{mesaj, sugestieAlternativa}` (paritar HCL — reuse dialog).
- Mutațiile întorc `DispozitieDetaliiDto` complet → set direct în hub (un singur round-trip).

---

## 9. Decizii deschise (recomandarea mea + ce aștept de la tine)

1. **Numele enum-ului de status pe FE.** *Recomand:* enum nou `StatusActRedactional` (paralel, valori
   identice) — dispoziția rămâne lizibilă, zero atingere pe HCL. Alternativă: reutilizăm `StatusHclRedactional`
   direct (mai puțin cod, dar „Hcl" scurs în dispoziție).
2. **Comunicări: generalizare vs. paralel.** *Recomand:* generalizare (`SursaComunicariPrefect`, §5) — o
   singură sursă de adevăr. Atinge cod HCL (re-verificăm). Fallback: copie paralelă.
3. **Butonul Delete pe dispoziție.** HCL **nu** expune ștergerea actului întreg în UI. Backend-ul dispoziției
   **are** matricea de gărzi. *Recomand:* îl expunem în meniul ⋮ (Admin), util pentru curățarea draft-urilor
   greșite (ex. tip greșit) — o mică abatere de la paritatea HCL, cu gărzile oglindite. Confirmă dacă vrei.
4. **Selectorul de emitent înlocuitor.** *Recomand:* toți consilierii cu etichetă clară acum; filtrarea la
   viceprimari (via mandate) = enhancement ulterior.

---

## 10. Filozofie de implementare (din planurile anterioare, încă validă)

Planul e ghid, nu script. La fiecare piesă: citesc componenta/serviciul HCL de paritate, identific pattern-ul,
adaptez la Dispoziție. Verific build + lint + smoke după **fiecare** pas, nu doar la final. Generalizările
(comunicări) le fac cu fluxul HCL existent ca plasă (re-smoke). Regula de aur: **oglindesc fidel gărzile
backend în `permisiuni.ts`** (driftul = bug) și **tratez explicit ce e specific dispoziției** (normativ vs.
individual, latch, refuz, emitent înlocuitor, convocare) — restul e paritate HCL disciplinată.
