Rezumat sesiune S56 — Testare manuală Faza 3 (Modul A: HCL)

## Context
Sesiune de **testare manuală** (nu dezvoltare) a întregului Modul A — HCL (FE1+FE2+FE3,
livrate în S53–S55). Eu (user) am testat în browser pe `localhost:4200`, Claude m-a ghidat
pas cu pas pe secțiunile A→H din `docs/planuri/plan_testare_manuala_faza3.md`. Tenant Slobozia.
Paginile publice HCL (Faza 9) NU au intrat. Ghidul de testare a fost respectat integral.

## Pregătire date de test
- Conturi confirmate: Admin, **Secretar**, **Consilier** (Ion Popescu) — toate prezente → matricea
  de permisiuni s-a putut testa complet.
- Generat un HCL de test **8/2026** dus prin tot ciclul **Draft→Numerotat→Semnat** (ca să nu poluăm
  5/6/7-2026). Punct adoptat pregătit prin API pe ședința 4; prezență adăugată temporar pentru Vasile
  (pentru ramura „≥2 semnatari alternativi" art.140). Tot ce s-a creat a fost **șters la final**.

## Ce am testat (toate secțiunile — TRECUT ✅)
- **A** acces/listă/filtre/căutare/navigare + **matrice roluri** (Admin vs Secretar vs Consilier).
- **B** generare HCL din punct adoptat, editor auto-save + Ctrl+S, gardă modificări nesalvate,
  atribuire număr (lacune 409 + nr. luat 409), semnare, PDF.
- **C** semnatari (XOR rol→câmp, flux art.140 cu motiv + prezență), variantă semnată (garda B:
  prima atașare permisă post-MOL, înlocuire/ștergere blocate post-MOL), stări legale
  (publicare portal, MOL, invalidare) + anulările (doar Admin).
- **D** comunicări prefect (gardă ≥Numerotat, badge răspuns color-coded), registru cross-HCL,
  ștergere doar-Admin.
- **E** relații intern (autocomplete, HCL curent exclus) / extern (XOR), oglindire read-only,
  ștergere din sursă (dispare oglindirea), duplicat 409, invalidare cu „relații active 409".
- **F** anexe (nr. ordine condiționat de tip, duplicat 409, download/editare/ștergere,
  imutabilitate tip+nr pe HCL Semnat).
- **G** widget T-3 (toate 3 culori roșu/portocaliu/verde + texte, link, dispare/reapare la
  comunicare, stare goală, ascuns la Consilier).
- **H** date/fus (DateOnly fără oră; adoptarea cu fusul instituției), gărzi reactive, consolă, lazy.

## Bug-uri reparate în sesiune — Frontend (live, hot-reload)
1. **Bug UTC** „(UTC+2:59.99073…)" în antetul Ședinței → `offsetFusOrar` întorcea minute fracționare
   (milisecundele datei „scurgeau" în calcul); fix `Math.round` în `shared/data.ts`. Helper partajat.
2. Etichetă dialog semnatar „Persoană (Secretar UAT)" → **„Secretar UAT"** (distincția internă
   Persoană/Consilier nu trebuie în UI).
3. Indiciul de prezență la semnatar — afișat **doar la rolul „Semnatar alternativ"** + reformulat
   („Trebuie să fie un consilier care a fost prezent la ședință (art. 140)").
4. Nota art.140 din tab Semnatari — **dispare când e îndeplinită** (2 alternativi + motiv) și apare o
   notă pozitivă; înainte rămânea ca avertisment chiar și după îndeplinire.
5. **Tab activ persistă în URL** (`?tab=`) — înainte, „back" din registru / refresh reveneau pe Detalii.
6. **Bug important:** navigarea HCL→HCL (link de relație / din registru) arăta date **stale** — Angular
   reutiliza componenta `/hcl/:id`, iar `id`+datele se citeau o singură dată. Fix: reîncărcare pe
   `route.paramMap` + reset stare (`initPentruHcl`) în `hcl-detalii`.
7. Rândul oglindă din tab Relații („Hotărâri care vizează această hotărâre") era confuz (badge la
   dreapta + „această hotărâre") → reformulat la **formă pasivă** „Modificată de", badge la stânga
   (consistent cu sursa); helper nou `etichetaTipRelatieHclPasiv`.

## Bug-uri reparate în sesiune — Backend (⚠️ necesită RESTART backend ca să se aplice/verifice)
8. Mesaj numerotare „Nr. X **tocmai a fost** atribuit altui HCL" → „Nr. X **este** atribuit…"
   (`ServiciuNumerotareHcl.cs` — mesajul venea din catch-ul de concurență, dar prindea și cazul în
   care utilizatorul tastează manual un număr existent, unde „tocmai" e greșit).
9. Scos jargonul tehnic „(PUT/POST /api/…)" din 2 mesaje către utilizator
   (`SemnatariHclController.cs` motiv-first + `HclController.cs` precondiție generare).

## De investigat (tehnic)
- **Data publicării în MOL** e stocată ca `DateTime` și convertită cu `inputLocalLaUtcIso` → posibil
  **shift de fus** (s-a stocat 28.06 pentru data aleasă 29.06). Nu e vizibilă acum (nu se afișează),
  dar **contează la paginile publice (Faza 9)**. De verificat `publicare-mol-dialog` + tipul
  `DataPublicareMol` (eventual `DateOnly`, ca la celelalte date „doar zi").

## De cercetat pe viitor (juridic) — IMPORTANT, memorii salvate
Două întrebări legale ridicate în testare, pe care le-am marcat pentru **research juridic** ulterior
(salvate și ca memorii de proiect):
- **Modificarea/anularea unui HCL deja publicat în MOL.** Un act oficial publicat și în vigoare nu ar
  trebui să poată fi alterat sau șters prin schimbarea documentului — corecturile se fac legal prin
  *îndreptarea erorilor materiale* (documentat) sau printr-un *act nou* (rectificare/modificare/abrogare);
  doar erorile de înregistrare în sistem se corectează administrativ. Funcția „Anulează publicarea MOL"
  (Admin) permite tehnic anulare → înlocuire variantă semnată → republicare, ocolind intangibilitatea
  de după MOL. De stabilit juridic ce e permis și dacă „Anulează MOL" ar trebui să ceară **motiv
  obligatoriu** salvat în audit. → memoria `mol-anulare-justificare-legal`.
- **Completitudinea motivelor de invalidare.** `MotivInvalidare` are doar 4 valori (Anulat de prefect /
  de instanță / Abrogat prin HCL ulterior / Retractat), **fără „Altul"** (deși alte enum-uri au).
  De stabilit dacă cele 4 sunt exhaustive legal sau e nevoie de „Altul" (+ eventual motiv text liber);
  observații: suspendarea/modificarea sunt modelate ca relații, nu invalidări; „Anulat de prefect" e
  imprecis (prefectul atacă, instanța anulează); posibilă lacună: caducitatea.
  → memoria `motiv-invalidare-completitudine-legal`.

## Amânate — Faza 2 (modulul Ședințe, în afara scopului de azi)
- CSS: badge „Convocată" se suprapune peste butonul „Editează" la micșorarea ferestrei (antet Ședință).
- 404 benigne în consolă pe pagina Ședinței (`GET /Sedinte/{id}/Transcriere` și `/ProcesVerbal` când nu
  există încă) — tab-urile arată stare goală corect; eventual de întors 200 gol în loc de 404.

## Clarificate (NU sunt bug-uri)
- Indicatorul „Modificări nesalvate" (semnal `dirtyPersistent` la 10s) e practic invizibil fiindcă
  auto-save-ul salvează la 2s — **identic în toate 3 editoarele** (HCL/Proces-verbal/Transcriere).
- 401 la ștergere (din consolă) = expirarea token-ului de acces (15 min) → interceptorul reîmprospătează
  automat + reia cererea (de aceea acțiunea reușește).
- Anularea publicării MOL **persistă corect** în baza de date (verificat empiric).
- „Observații interne" la răspunsul prefectului nu apar în tabel — intenționat (notițe interne;
  obiecțiile motivate, conținut substanțial, apar cu iconiță).

## Note de mediu / date
- Mediul restaurat **exact** ca la început: HCL id=1→5/2026 (Semnat, Publicat, MOL, comunicarea 1/2026),
  id=2→6/2026, id=3→7/2026, id=4→Draft; ședința 4 cu punctele 3,4; prezent doar Ion. HCL 8/2026 de test
  + punctul + voturile + prezența temporară + 2 fișiere orfane — toate șterse.
- Sesiunea a traversat miezul nopții **29→30 iunie 2026** — relevant la widgetul T-3: 6/2026 a trecut
  natural de la „Expiră azi" la „Depășit cu 1 zi". `astazi` se calculează în UTC.

## Următoarea sesiune
- **Repornire backend** + verificarea celor 2 mesaje reparate (#8 numerotare „este atribuit", #9 fără
  jargon) — ușor de reverificat pe draft-ul id=4.
- Research juridic pe cele 2 teme (memoriile de mai sus).
- La Faza 9 (pagini publice): de verificat afișarea datei MOL (vezi „De investigat").
