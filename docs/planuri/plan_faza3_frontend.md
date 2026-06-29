# Plan Faza 3 — Frontend Modul A (HCL + Comunicare Prefect)

## Context
Backend complet (S49–S52). UI internă (secretar/admin) pentru Modul A, în 3 sub-sesiuni
(context mic per sesiune). **Paginile publice HCL = Faza 9** (portal extins + decizia SSR) —
`PublicHclController` există în backend, dar UI-ul public NU intră aici.

## Stare
- **FE1 — LIVRAT (S53)** ✅ — spina actului: schelet + listă + generare din punct + hub
  (Detalii + Conținut cu auto-save) + numerotare + semnare + PDF. Backend pas 0 + tot FE1;
  `dotnet test` 265 verzi, `npm run build`/`lint` verzi, smoke test live OK. Commit făcut.
- **FE2 — sesiunea următoare** — semnatari + stări legale + variantă semnată.
- **FE3** — exterior: comunicări prefect + registru, relații, anexe, widget T-3.

## Decizii confirmate (S53)
1. **„Generează HCL" în `PuncteTab`** (pe punctul adoptat) — nu în antetul ședinței.
2. **Etichetă meniu „Hotărâri"** (rută `/hcl`).
3. **Widget T-3 pe `Acasă`** (se implementează la FE3).
4. **`hclId: int?` pe `PunctOrdineZiDto`** — ascunde „Generează", arată „Vezi HCL" + navigare.

## Convenții stabilite în FE1 (reutilizate la FE2/FE3)
- **Mutațiile consumate de hub (starea `HclDetalii` partajată) întorc `HclDetaliiDto`** prin
  helperul `ReincarcaCuIncludeAsync` din `HclController` → hub-ul face uniform
  `this.hcl.set(rezultat)`. (Sub-resursele cu tab propriu — Comunicări/Relații/Anexe — fac
  excepție: au listă proprie, vezi FE3.)
- **Precondiții neverificabile pe client → reactive** (snackbar din 400/409), NU disabled mut.
- `hcl.permisiuni.ts` = oglindă strictă a gărzilor pe status.
- Editor auto-save 1:1 cu `ProcesVerbalTab`; download/upload prin blob (cleanup `revokeObjectURL`);
  confirmare `periculos` la ireversibile; erori în dialog inline / acțiuni directe snackbar.
- **Disciplină „pas 0"**: orice atingere de backend întâi, fiecare cu testul ei →
  `dotnet test` full verde înainte de a scrie frontend.
- **Smoke test la final**, reutilizând tenant-ul Slobozia (seed) + HCL 7/2026 (deja Semnat).
  Lanțul de date se face rapid prin API (script Node), apoi verificare UI prin preview pe :4200
  (CORS dev permite doar `http://localhost:4200`).

## FE1 — livrat (rezumat scurt)
- `shared/enums.ts` + `shared/etichete.ts`: toate cele 8 enum-uri HCL + etichete.
- `features/hcl/`: `hcl.models.ts`, `hcl.service.ts`, `hcl.permisiuni.ts` (+ `semnatariCompleti`).
- `hcl-lista` (rută `/hcl`) + meniu „Hotărâri"; `hcl-detalii` hub (Detalii + Conținut editor);
  `atribuie-numar-dialog` (pre-fill `SugestieNumar` + cele 3 răspunsuri 409).
- Hook în `PuncteTab` (`poateGeneraHcl` + Generează/Vezi HCL).
- Backend pas 0: 4 mutații FE1 → `HclDetaliiDto`, `hclId` pe DTO punct, `GET /Hcl/{id}/SugestieNumar`.

---

## FE2 — semnatari + stări legale + variantă semnată (detaliat)

A doua parte din Modul A, peste hub-ul existent. Tot consolă internă.

### Pas 0 — backend (întâi, fiecare cu testul ei)
1. **Mutațiile FE2 din `HclController` → `HclDetaliiDto`** prin `ReincarcaCuIncludeAsync`
   (există deja): `Invalidare`, `AnuleazaInvalidare`, `Publicare`, `PublicareMol`,
   `AnuleazaPublicareMol`, `SeteazaMotivLipsaPresedinte`. Cele care întorc azi `NoContent`
   (`AnuleazaPublicareMol`) — fie întorc Detalii, fie hub re-fetch (aliniere la „set(rezultat)").
2. **`SemnatariHclController` (POST/DELETE semnatar) → `HclDetaliiDto`** — garda „Semnează"
   depinde de lista de semnatari, deci hub-ul trebuie să-și actualizeze starea. De verificat
   ce întorc acum (probabil `SemnatarHclDto` / listă) și aliniat.
3. `dotnet test` full verde înainte de frontend.

### Frontend
- **models**: cereri FE2 — `AdaugareSemnatar`, `InvalidareHcl`, `PublicareHcl`, `PublicareMol`,
  `MotivLipsaPresedinte`. (Răspunsurile — `SemnatarHcl`, `RelatieHcl` — există în `HclDetalii`.)
- **service**: `adaugaSemnatar/stergeSemnatar`, `publica(estePublicat)`, `publicaMol(data)`,
  `anuleazaMol`, `invalideaza(dto)`, `anuleazaInvalidare`, `seteazaMotivLipsa`, + variantă
  semnată `incarcaSemnat/descarcaSemnat/stergeSemnat` (`api/Hcl/{id}/Semnat`).
- **permisiuni**: extind `ActiuniHcl` (oglindă strictă): `poatePublica` (≥ Numerotat),
  `poateDepublica`, `poatePublicaMol` (== Semnat), `poateAnulaMol` (Admin + are MOL),
  `poateInvalida` (nu dacă deja invalidat), `poateAnulaInvalidare` (Admin + invalidat),
  `poateGestionaSemnatari` (≠ Semnat); variantă semnată = garda B (upload pe Semnat;
  replace/delete blocate după MOL; delete Admin).
- **Hub** (crește aditiv):
  - **Tab Semnatari**: listă (rol/nume/ordine, ordonat `OrdineAfisare`) + adăugare prin dialog
    cu **XOR persoană/consilier** + ștergere; editabil doar pe ≠ Semnat. **Flux art.140**:
    fără președinte → minim 2 semnatari alternativi (`SemnatarAlternativArt140`) + câmp
    `MotivLipsaPresedinte` (PUT separat). Validările backend (consilier prezent la ședință,
    motiv setat) tratate reactiv.
  - **Card „Variantă semnată"** paritar `ProcesVerbalTab`: apare pe Semnat; upload/descărcare/
    replace (confirm înainte de file picker)/delete (Admin); **garda B** (prima atașare
    post-MOL OK, replace/delete → 409 după MOL).
  - **Antet**: Publicare (toggle „Publică pe portal / Retrage de pe portal") vizibil; în meniul
    ⋮ cele rare — Publicare MOL (dialog dată), Invalidare (dialog motiv enum + referință +
    **confirmare relații active** din 409, ca la lacune-numerotare), Anulează invalidare /
    Anulează MOL (Admin).
  - **Badge-uri noi**: „Publicat", „Publicat MOL", „Invalidat" în antet (+ listă).
- **Dialoguri noi**: `semnatar-dialog` (XOR + art.140), `invalidare-dialog`
  (motiv + ref + confirmare relații active), `publicare-mol-dialog` (date picker nativ + fus).

### De citit la începutul FE2
`proces-verbal-tab` (cardul variantă semnată + garda B), `features/functii-oficiale`
(dialoguri XOR persoană/consilier), `SemnatariHclController` + `HclController`
(Invalidare/Publicare/PublicareMol/MotivLipsa) pentru contract.

---

## FE3 — exterior + widget T-3 (detaliat)

A treia parte: relația cu prefectul + acte conexe + alertă pe pagina de start.

### Pas 0 — backend
**Probabil ZERO atingeri.** Sub-resursele (Comunicări, Relații, Anexe) au controllere proprii
care întorc DTO-urile lor, iar tab-urile își gestionează **lista proprie** (self-contained, ca
`puncte-tab`/`documente-tab`) — NU depind de starea `HclDetalii` partajată. De confirmat la
început; dacă vrem badge-uri pe antet care reflectă nr. comunicări, hub-ul oricum reîncarcă.

### Contracte backend (verificate în S53)
- **Comunicări** `api/Hcl/{hclId}/Comunicari`: GET listă (desc `NumarOrdineInRegistru`);
  POST [Admin,Secretar] (`CreareComunicareDto`, gardă Status ≥ Numerotat, retry pe race) →
  `ComunicareHclPrefectDto`; PUT `/{id}` (`ActualizareComunicareDto` — update răspuns prefect;
  imutabile: nr ordine/an/dată/canal) → DTO; DELETE `/{id}` [Admin].
- **Registru** `api/RegistruComunicariPrefect?an=&page=&size=` [Admin,Secretar] → listă
  `RegistruComunicareDto` (cronologic, an default = anul curent).
- **Relații** `api/Hcl/{hclId}/Relatii`: GET → `{ relatiiSursa, relatiiTinta }`;
  POST [Admin,Secretar] (`CreareRelatieDto`, **XOR** `HclTintaId` intern / `ReferintaActExternText`)
  → `RelatieHclDto`; DELETE `/{id}` **doar din sursă** (`HclSursaId == hclId`).
- **Anexe** prin `DocumenteController`: POST `/api/Documente` cu `hclId` + `tipDocumentHcl` +
  `numarOrdinAnexa` (forțează `TipDocument=Altele`; `NumarOrdinAnexa` imutabil pe Semnat;
  duplicat anexă → 409). Documentele HCL sunt deja în `HclDetalii.documente`.
- **T-3** `api/Hcl/UrgentDeComunicat?prag=3` [Admin,Secretar] → `List<HclUrgentDto>`
  (`HclId, Numar, AnNumerotare, Titlu, DataAdoptare, DataLimitaComunicare, ZileRamase, Status`).

### Frontend
- **models**: cereri (`CreareComunicare`, `ActualizareComunicare`, `CreareRelatie`) +
  `RegistruComunicare`, `HclUrgent`. (`ComunicareHclPrefect`/`RelatieHcl`/`DocumentHcl` există.)
- **service**: extind `HclService` (comunicari/relatii pe `api/Hcl/{id}/...`, anexe prin
  Documente) + servicii noi `RegistruComunicariService` și `HclDashboardService`.
- **Hub** (tab-uri noi, self-contained):
  - **Tab Comunicări prefect**: listă (nr ordine, dată, canal, răspuns) + adăugare (dialog:
    dată, `CanalTransmiterePrefect`, nr înregistrare, observații) + editare răspuns
    (`RaspunsPrefect` + dată + obiecții) + ștergere (Admin). Gardă ≥ Numerotat (reactiv).
  - **Tab Relații**: două secțiuni — relații-sursă (cu ștergere) + relații-țintă (read-only) +
    adăugare (dialog XOR: HCL intern via autocomplete din `GET /Hcl` / text act extern +
    `TipRelatieHcl`). Ștergere doar din sursă (oglindă backend).
  - **Tab Anexe**: reutilizează pattern `DocumenteTab` (upload cu progres + listă + download
    blob) cu câmpuri HCL (`TipDocumentHcl`, `NumarOrdinAnexa`). Sursa: `HclDetalii.documente`
    sau `GET /Documente?hclId`.
- **Pagină Registru** `registru-comunicari`: tabel cronologic + filtru an + paginare.
  **Decizie de luat**: loc în meniu (sub „Hotărâri" vs intrare separată) — recomand un buton
  „Registru comunicări" în tab-ul Comunicări + opțional intrare de meniu.
- **Widget T-3 pe `Acasă`**: card „HCL urgent de comunicat" din `UrgentDeComunicat(prag=3)`,
  cod culoare pe `ZileRamase` (verde > 0, portocaliu ≈ 0, roșu < 0 = depășit), fiecare rând
  link la HCL. `acasa` e acum aproape gol → loc natural.

### De citit la începutul FE3
`features/documente/documente-tab` (upload+listă+download), `features/convocari/convocari-tab`
(tab self-contained + dialoguri), `ComunicariHclPrefectController` / `RelatiiHclController` /
`RegistruComunicariPrefectController` / `HclDashboardController`.

---

## Notă de mărime
Faza 3 totală ≈ 7 sesiuni (4 backend + 3 frontend). FE2 și FE3 pot fi comasate într-o sesiune
mai lungă dacă se dorește, dar fluxul art.140 (FE2) + cele 3 tab-uri externe (FE3) sunt
substanțiale — recomand separat pentru context mic.
