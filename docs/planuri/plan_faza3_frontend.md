# Plan Faza 3 — Frontend Modul A (HCL + Comunicare Prefect)

## Context
Backend complet (S49–S52, 262 teste verzi). Urmează **UI internă** (secretar/admin) pentru
Modul A. Pattern-urile sunt în `cleriq-web/CLAUDE.md` + `docs/frontend.md`; `frontend.md`
prevede deja hook-uri pentru HCL (card „HCL emis", stări legale ortogonale).

**Scop — ce NU intră aici:** paginile publice HCL (cetățean) sunt **Faza 9** (portal extins +
decizia SSR). `PublicHclController` există în backend, dar UI-ul public vine la Faza 9.
Aici facem doar consola internă.

## Suprafața de acoperit (API din S50–S52)
Listă HCL (filtre an/status/tip) · detalii · Genereaza (din punct adoptat) · Conținut
edit/regen · AtribuieNumar (lacune / număr luat / număr ars) · Semneaza · Semnatari (art.140 +
MotivLipsaPresedinte) · Invalidare±anulare · Publicare · PublicareMol · PDF generat + variantă
semnată (POST/GET/DELETE, varianta B) · Comunicări prefect + Registru · Relații · Anexe ·
Dashboard `UrgentDeComunicat` (T-3).

## Decompunere recomandată — 3 sub-sesiuni
Mărimea modulului ≈ cea din backend (4 sesiuni). Pentru context mic per sesiune, 3 părți:

- **FE1 — spina actului**: schelet feature + listă + generare din punct + hub detalii
  (Detalii + Conținut cu auto-save) + numerotare + semnare + PDF.
- **FE2 — semnatari + stări legale + variantă semnată**: tab Semnatari (flux art.140),
  acțiuni antet Publicare / PublicareMol / Invalidare, card „Variantă semnată" (upload/replace/
  delete, garda B).
- **FE3 — exterior**: tab Comunicări prefect + Registru cronologic, tab Relații, tab Anexe,
  widget Dashboard T-3 pe `acasa`.

Ordinea respectă dependențele: FE1 livrează scheletul (service/models/permisiuni + hub) pe
care FE2/FE3 doar adaugă tab-uri/acțiuni. Plata pattern-urilor (editor, badges) se face în FE1.

## FE1 — sesiunea viitoare (detaliat)

**Schelet** `features/hcl/`: `hcl.models.ts` (oglindă `HclDto`/`HclDetaliiDto`/sub-DTO-uri),
`hcl.service.ts` (`Promise<T>` via `firstValueFrom`), `hcl.permisiuni.ts` (oglindă strictă a
gărzilor: cine poate genera/numerota/semna/edita/șterge + tranzițiile pe status). Enum-urile
HCL (`StatusHclRedactional`, `TipHcl`, `RolSemnatar`, `TipRelatieHcl`, `MotivInvalidare`,
`CanalTransmiterePrefect`, `RaspunsPrefect`, `TipDocumentHcl`) + etichete → `shared/enums.ts` +
`shared/etichete.ts`.

**Listă** `hcl/hcl-lista` (rută `hcl`): filtre an/status/tip, badge status (Draft/Numerotat/
Semnat) + badge ortogonal „Invalidat", subtitlu metadate concatenate (pattern listă). Reload de
la server după mutație. Adaugă „Hotărâri" în meniul `shell`.

**Generare din punct**: buton „Generează HCL" în `PuncteTab`, vizibil pe punct `ProiectHCL` cu
`Rezultat = Adoptat` și fără HCL existent. Precondițiile backend (președinte de ședință setat +
Secretar UAT valid la dată) → oglindite în permisiuni + „snackbar avertizant la click" când
lipsesc, NU disabled mut. La succes → navigare la `hcl/:id`.

**Hub detalii** `hcl/hcl-detalii` (rută `hcl/:id` + `canDeactivate: [ghidModificariNesalvate]`):
`mat-tab-group`. Acțiunile (Atribuie număr / Semnează / Descarcă PDF) stau în **antet**, nu în
tab (pattern hub). Badge-uri status în antet.
- **Tab Detalii**: `<dl>` cu titlu, tip, dată adoptare, vot snapshot, majoritate, status.
- **Tab Conținut**: editor cu auto-save (`ModificariNesalvateService`, id `hcl-continut-${id}`)
  — reutilizare 1:1 din `ProcesVerbalTab` (textarea + effect sync DOM↔signal, debounce 2s,
  Ctrl+S, indicator stare). Gardă editare = `Status != Semnat`. Buton „Regenerează" (confirmare
  `periculos`, suprascrie editările).

**Acțiuni antet FE1**:
- **Atribuie număr**: dialog cu input număr; tratează cele 3 răspunsuri 409 — lacune
  (`{lacune}` → confirmare „lasă liber 1,2,3,4?”), număr luat / ars (`{sugestieAlternativa}` →
  oferă sugestia). Erori inline în dialog.
- **Semnează**: confirmare; oglindește garda completitudine semnatari (mesaj clar dacă lipsesc).
- **Descarcă PDF**: download via blob (pattern `URL.createObjectURL` + revoke).

**Rute** (segmente fixe înaintea `:id`): `hcl` → listă; `hcl/:id` → hub (cu `canDeactivate`).

## FE2 / FE3 — schiță (detaliere la sesiunea lor)
- FE2: `semnatari-tab` (CRUD + dialog adăugare cu XOR persoană/consilier, flux art.140 cu
  `MotivLipsaPresedinte`); acțiuni antet Publicare (toggle) / PublicareMol (dată) / Invalidare
  (dialog cu confirmare relații active); card „Variantă semnată" paritar PV (garda B:
  prima atașare post-MOL OK, replace/delete blocate). Badge „Publicat MOL" / „Invalidat".
- FE3: `comunicari-tab` (CRUD + update răspuns prefect) + pagină Registru cronologic
  (`api/RegistruComunicariPrefect`); `relatii-tab` (intern/extern, ștergere din sursă);
  `anexe-tab` (reutilizează pattern Documente + `TipDocumentHcl`/`NumarOrdinAnexa`); widget
  „HCL urgent de comunicat" (T-3) pe `acasa`, cu cod culoare pe zile rămase (negativ = depășit).

## Decizii de confirmat (la începutul FE1)
1. **Generarea HCL din `PuncteTab`** (recomandat — punctul adoptat e contextul natural) vs un
   buton separat în antetul ședinței.
2. **Eticheta de meniu**: „Hotărâri" (recomandat, limbaj cetățean) vs „HCL".
3. **Widget T-3 pe `acasa`** (recomandat — e prima pagină) vs pagină dedicată.

## De citit la începutul sesiunii (paritate)
`docs/frontend.md` (editor auto-save, hub tab-uri, publish flow, download blob, badges),
`features/proces-verbal/proces-verbal-tab` (act cu editor + variantă semnată), `sedinta-detalii`
(hub), `features/sedinte/sedinte-lista` + `sedinta-form` (listă + formular), `features/
functii-oficiale` (paritate dialoguri/serviciu) și controllerele HCL din backend pentru contractul exact.

## Notă de mărime
Faza 3 totală ajunge la ~7 sesiuni (4 backend + 3 frontend), peste estimarea inițială de 3–5 —
e modulul cel mai mare și cel mai mare time-saver. Dacă vrei mai compact, FE2+FE3 pot fi comasate
într-o sesiune mai lungă.
