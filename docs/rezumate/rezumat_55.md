Rezumat sesiune S55 — Frontend Faza 3 Modul A: FE3 (comunicări prefect + registru + relații + anexe + widget T-3)

## Context
FE1 (S53) + FE2 (S54) livrate. S55 = a treia și ultima sub-sesiune frontend Modul A, peste
hub-ul HCL existent. Paginile publice HCL rămân Faza 9. Plan: `docs/planuri/plan_faza3_frontend.md`
(secțiunea FE3). Lucrat pe bucăți, build/lint între fiecare, smoke test live doar la final.

## Decizii confirmate (recomandate de mine + recenzie încrucișată cu sesiunea S54)
1. **Autocomplete Relații** = filtrare pe client din `GET /Hcl`, dar cu `take=200` (vezi „Probleme
   descoperite" #1) + TODO endpoint de căutare server-side peste ~200 HCL.
2. **Sursă tab Anexe** = `DocumenteService` EXTINS (nu `HclDetalii.documente`) — sub-resursă
   self-contained, refolosește upload + download blob.
3. **Widget T-3** = o singură listă color-coded, depășitele evidențiate roșu sus, prag 3 fix.
4. Împărțire în 5 bucăți; smoke test live doar la final (nu după fiecare pas).

## Pas 0 — backend: ZERO atingeri (confirmat empiric)
Toate cele 5 endpoint-uri (Comunicări, Registru, Relații, Documente-anexe, UrgentDeComunicat) sunt
self-contained și întorc DTO-urile proprii; tab-urile FE3 își gestionează **lista proprie**
(ca `documente-tab`/`convocari-tab`), fără dependență de starea `HclDetalii` partajată. **Niciun
fișier `.cs` modificat.** Badge „nr. comunicări" pe antet — respins intenționat (ar fi fost singura
atingere de backend).

## FE3 frontend (livrat — `npm run build` + `lint` verzi pe fiecare bucată)
1. **Tab Comunicări prefect**: `comunicari-tab` (self-contained, input `hclId`+`status`) +
   `comunicare-dialog` (adăugare) + `raspuns-prefect-dialog` (editare răspuns prefect). Gardă
   „≥ Numerotat" (oglindă proactivă pe `actiuni` + reactiv din 409). Ștergere = doar **Admin**.
2. **Pagină Registru**: `registru-comunicari` + `RegistruComunicariService` (nou) — tabel cronologic
   cross-HCL, filtru an, paginare prev/next (truc `size+1`). Rută lazy `hcl/registru-comunicari`
   ÎNAINTEA lui `hcl/:id`. Buton „Registru comunicări" în tab-ul Comunicări (doar Admin/Secretar).
3. **Tab Relații**: `relatii-tab` (2 secțiuni — sursă cu ștergere + țintă read-only) +
   `relatie-dialog` (XOR intern via autocomplete / act extern, comutat reactiv cu `toSignal`).
   Ștergere doar din sursă (oglindă backend).
4. **Tab Anexe**: `anexe-tab` + `anexa-dialog`, prin `DocumenteService` EXTINS
   (`ContextDocument.hclId`; `DateUpload` + `tipDocumentHcl`/`numarOrdinAnexa`; `Document` + 3
   câmpuri HCL; `ActualizareDocument` cu cele 2 câmpuri **opționale**). `numarOrdinAnexa` apare
   condiționat pe tipul Anexă; imutabilitate pe HCL Semnat (tip + nr. disabled, cu hint).
5. **Widget T-3 pe Acasă**: `hcl-urgent-widget` + `HclDashboardService` (nou) — listă color-coded
   (roșu `<0`, portocaliu `0–1`, verde `≥2`), fiecare rând link la HCL, randat **doar Admin/Secretar**
   (Acasă e vizibilă și consilierilor → evită 403).
- Helper nou `shared/data.ts`: **`formateazaDataDoar`** (DateOnly fără shift de fus).
- Permisiuni noi în `hcl.permisiuni.ts`: `actiuniComunicari`, `actiuniRelatii`, `actiuniAnexe`.
- Hub-ul HCL are acum 6 tab-uri: Detalii · Conținut · Semnatari · Comunicări · Relații · Anexe.

## Probleme / capcane descoperite în execuție
1. **Plafonul `GET /Hcl` = 50 implicit.** `HclService.lista()` nu trimitea `take` → autocomplete-ul
   de la Relații ar fi ratat silențios HCL-urile vechi (o relație poate ținti orice an). Fix fără
   backend: `take`/`skip` pe `FiltreHcl` + `lista()`; autocomplete cere `take=200`. **TODO**: peste
   ~200 HCL/instituție → endpoint de căutare server-side (după nr/an/titlu). (Verificat în
   `HclController.cs:45-59`.)
2. **Capcană backend „`take`/`size` > 200 → reset la 50"** (`HclController` + `RegistruComunicariPrefectController`).
   De aceea autocomplete cere exact 200, iar Registru cere 51 (`size+1` pentru „are pagină
   următoare"). Cerând 201 primești silent 50 — exact bug-ul de evitat.
3. **DateOnly ≠ DateTime.** Helperele din `shared/data.ts` aplică `Europe/Bucharest` → pe un
   DateOnly ar adăuga o oră parazită (pe fusuri în urma UTC ar muta și ziua — la noi, înaintea UTC,
   doar oră parazită). Rezolvat cu `formateazaDataDoar` (ancorat la miezul nopții UTC) + `<input
   type="date">` direct, fără conversie de fus.
4. **Regresie evitată:** `ActualizareDocument` a primit `tipDocumentHcl`/`numarOrdinAnexa` ca
   **opționale**, ca `document-dialog` (ședință/punct) să rămână neatins.
5. **Anexe pe HCL Semnat:** am blocat în UI atât tipul cât și numărul anexei (backend blochează
   strict doar schimbarea numărului). E o restricție ușor mai strictă decât backend, dar sigură
   (evită 409 indirecte din schimbarea tipului Anexă↔non-Anexă) și corectă juridic. Drift minor
   intenționat.
6. **Capcană mediu:** `curl` în Git Bash nu citește fișiere cu cale MSYS (`/c/...`) la `-F @`
   (exit 26) — necesită cale Windows (`C:/...`).
7. **File picker OS nu e automatizabil** în browserul headless de preview → upload-ul real de octeți
   la Anexe l-am verificat semănând o anexă prin API (`POST /api/Documente`) + validând randare /
   editare / ștergere prin UI.

Niciun bug funcțional în codul FE3 prins la smoke test — totul a mers din prima.

## Smoke test live (tenant Slobozia, date reale, zero erori consolă, zero cereri eșuate)
- **Widget T-3**: HCL 6/2026 „Expiră azi" (portocaliu, 29.06.2026), link OK.
- **Comunicări**: add (2/2026) + editare răspuns (badge verde „Acceptat") + ștergere.
- **Registru**: 1 (HCL 5/2026) + 2 (HCL 6/2026), filtru an, paginare, `Location.back()`.
- **Relații**: autocomplete intern (HCL curent **exclus**) + act extern (XOR) + **oglindire**
  verificată pe HCL 5/2026 (apare ca țintă read-only, fără ștergere) + ștergere din sursă.
- **Anexe**: randare + condiționare nr. ordine + imutabilitate Semnat (tip+nr disabled, denumire
  editabilă, PUT fără 409) + ștergere.
- Toate datele de test șterse prin UI (testând și fluxurile de ștergere) → **mediul restaurat exact
  ca la început**.

## Note de mediu / date dev (pentru sesiunea următoare)
- Dev: SQL + Redis pornite de user; backend pornit de user pe profilul `https` → `localhost:7006`;
  preview `ng serve :4200` (CORS dev doar `:4200`). `.claude/launch.json` „cleriq-web" deja
  configurat (`npm --prefix cleriq-web start`).
- Sesiunea de login persistă în `localStorage` (`cleriq.token`) → preview-ul a pornit deja
  autentificat (Admin Slobozia).
- Date HCL **neschimbate** față de S54: id=1 → **5/2026** (Semnat, Publicat, MOL; are comunicarea
  **1/2026** preexistentă în registru), id=2 → **6/2026** (Semnat; candidatul T-3 „expiră azi"),
  id=3 → 7/2026 (Semnat), id=4 → Draft. Comunicarea de test 2/2026 a fost ștearsă.

## Următoarea sesiune
Frontend-ul **Modulului A (HCL) e complet** (FE1 + FE2 + FE3). Rămâne pentru **Faza 9**: paginile
publice HCL (portal extins + decizia SSR) — `PublicHclController` există deja în backend. Vezi
`docs/roadmap.md`.
