# Ghid testare manuală — Faza 3 (Modul A: HCL + Comunicare Prefect)

> **Pentru sesiunea de testare manuală.** Acesta NU conține pașii click-cu-click — e harta a
> CE trebuie testat, cu rezultatele așteptate, ca în sesiunea respectivă să te pot ghida pas cu pas.
> Acoperă **toată Faza 3**, nu doar S55: FE1 (spina actului), FE2 (semnatari + stări legale +
> variantă semnată), FE3 (comunicări + registru + relații + anexe + widget T-3).

## Cum lucrăm în sesiunea de testare
- **Tu testezi manual în browserul tău** (`localhost:4200`); **eu te ghidez** pas cu pas: îți spun
  exact unde să dai click și **ce ar trebui să vezi** (rezultat așteptat), iar tu îmi confirmi sau
  îmi spui ce ai văzut. (Diferit de S55, unde am condus eu un browser headless de preview.)
- Mergem **pe secțiuni**, în ordinea ciclului de viață al unui HCL (e cea mai eficientă).
- Pentru fiecare scenariu bifăm: **merge / nu merge / observație**. Ce nu merge → notez și decidem
  dacă reparăm pe loc sau separat.
- La final fac un mic raport cu ce a trecut și ce a apărut.

## Pregătire mediu (verific la început)
- [ ] SQL Server + Redis pornite.
- [ ] Backend pe `https://localhost:7006` (profil `https`).
- [ ] Frontend `ng serve :4200` (CORS dev permite doar `:4200`).
- [ ] Login funcțional: `admin.slobozia@cleriq.ro` / `AdminSlobozia1!`.
- [ ] **De confirmat:** există conturi de **Secretar** și **Consilier** pentru testarea pe roluri?
      Dacă nu, testăm matricea de permisiuni doar parțial (Admin) sau creăm conturi întâi.

## Date de test necesare (pentru a acoperi tot ciclul)
Multe gărzi depind de starea HCL-ului, deci avem nevoie de HCL-uri în **toate** stările:
- [ ] **HCL Draft** — pentru: editare conținut + auto-save, gestionare semnatari, atribuire număr,
      adăugare anexe/relații pe draft. (Acum: id=4 Draft, dar are deja motiv art.140 setat — ideal
      generăm unul nou, curat.)
- [ ] **HCL Numerotat (ne-semnat)** — pentru: semnare (gardă completitudine art.140), adăugare
      comunicare (gardă „≥ Numerotat").
- [ ] **HCL Semnat** — pentru: variantă semnată, publicare MOL, invalidare, imutabilitate anexe.
      (Acum: 5/6/7-2026.)
- [ ] **Două HCL-uri pentru relații** (unul ține de celălalt) + un act extern.
- [ ] **HCL adoptat acum >10 zile lucrătoare, fără comunicare** — pentru widgetul T-3
      (acum 6/2026 e candidat „expiră azi").
- **Recomandare:** testăm pe un **HCL nou generat** (ciclu complet Draft→Numerotat→Semnat) ca să nu
  poluăm 5/6/7-2026; la final curățăm datele de test. Lanț de generare (din rezumat_54): pe ședința
  id=4 → POST punct (ProiectHCL) → vot → `/Inchide` → `/api/Hcl/Genereaza`. Sau, mai natural, prin UI
  din tab-ul Ordine de zi al unei ședințe.

---

## Ce trebuie testat — checklist pe secțiuni

### A. Acces, listă, roluri
- [ ] Meniu „Hotărâri" → ruta `/hcl`; lista se încarcă.
- [ ] Filtre listă: an / status / tip (server-side, re-fetch) + căutare client (titlu/număr,
      diacritice-insensibil).
- [ ] Navigare în detalii (`/hcl/:id`) + buton înapoi.
- [ ] **Matrice permisiuni pe rol** (Admin vs Secretar vs Consilier) — punctul transversal cel mai
      important. De verificat că Secretarul NU vede acțiunile **doar-Admin**:
      ștergere comunicare, anulare MOL, anulare invalidare, ștergere variantă semnată.
      Și că Consilierul vede totul **read-only** (fără butoane de mutație; fără widget T-3 pe Acasă).

### B. FE1 — spina actului (S53)
- [ ] **Generare HCL** dintr-un punct adoptat (tab Ordine de zi al ședinței): butonul „Generează HCL"
      apare doar pe punctul potrivit; după generare → „Vezi HCL" + navigare. (Pe punct neadoptat: nu.)
- [ ] **Tab Conținut** — editor: auto-save la 2s, `Ctrl+S` salvare imediată, indicatorul de salvare
      (stările: se salvează / salvat la HH:MM / modificări nesalvate / eroare), „Regenerează din date".
- [ ] **Guard modificări nesalvate** la părăsirea paginii cu editări nesalvate.
- [ ] **Atribuire număr** (dialog): sugestie pre-completată; **confirmare la lacune** (numerotare
      neconsecutivă) — cele 3 răspunsuri 409; după numerotare, placeholderul din conținut e înlocuit
      și statusul devine Numerotat.
- [ ] **Semnare** (Numerotat→Semnat): blocată dacă semnatarii nu sunt compleți (vezi C); confirmare;
      după semnare conținutul devine read-only.
- [ ] **Descărcare PDF** (prin blob; nume fișier corect).

### C. FE2 — semnatari + stări legale + variantă semnată (S54)
- [ ] **Tab Semnatari** (doar pe ≠ Semnat editabil): adăugare prin dialog cu **XOR rol→câmp**
      (Secretar UAT → Persoană; Președinte/Alternativ → Consilier); ștergere.
- [ ] **Flux art. 140**: fără președinte → nota că trebuie ≥2 semnatari alternativi + câmp „motivul
      lipsei semnăturii președintelui" (salvare separată). Semnarea e permisă doar când lista e
      completă (1 Secretar + președinte SAU ≥2 alternativi + motiv).
- [ ] Validări backend tratate reactiv (ex. consilier care nu era prezent la ședință → mesaj).
- [ ] **Card „Variantă semnată" (PDF scanat)** — apare pe Semnat: upload, descărcare, înlocuire
      (confirmare înainte de file picker), ștergere (Admin). **Garda B**: prima atașare permisă chiar
      și după publicarea MOL; înlocuire/ștergere **blocate după MOL**.
- [ ] **Antet — stări legale**: Publică pe portal ↔ Retrage (confirmare la retragere); badge „Publicat".
- [ ] **Publicare MOL** (dialog dată) → badge „Publicat MOL"; **Anulare MOL** (doar Admin).
- [ ] **Invalidare** (dialog: motiv enum + referință) → badge „Invalidat"; **confirmare relații
      active** (dacă HCL-ul are relații → 409 cu listă, se cere confirmare); **Anulare invalidare**
      (doar Admin).

### D. FE3 — comunicări prefect + registru (S55)
- [ ] **Gardă „≥ Numerotat"**: pe un HCL Draft, butonul „Adaugă comunicare" e ascuns + mesaj
      „disponibil după numerotare".
- [ ] **Adăugare comunicare** (dialog: dată trimitere = azi implicit, canal, nr. înregistrare,
      observații) → rând nou cu nr. de ordine din registru.
- [ ] **Editare răspuns prefect** (dialog: răspuns enum + dată + obiecții + nr/dată confirmare +
      observații) → badge color-coded (Acceptat verde / Respins roșu / Cere clarificări portocaliu /
      „—" neînregistrat).
- [ ] **Ștergere comunicare** — **doar Admin** (Secretarul nu vede coșul).
- [ ] **Registru comunicări** (buton din tab, doar Admin/Secretar) → pagină cronologică cross-HCL;
      filtru an; paginare prev/next; click pe rând → HCL-ul respectiv; back revine pe tab.

### E. FE3 — relații (S55)
- [ ] **Adăugare relație internă**: autocomplete din lista de HCL-uri (caută după nr/titlu;
      **HCL-ul curent exclus**); tip relație.
- [ ] **Adăugare relație externă**: comutare XOR pe „Act extern" + text referință (max 300).
- [ ] **Oglindire**: relația apare pe HCL-ul țintă în secțiunea „Hotărâri care vizează această
      hotărâre", **read-only (fără ștergere acolo)**.
- [ ] **Ștergere doar din sursă** (oglindă backend) — șterge și oglindirea de pe țintă.
- [ ] Duplicat pe același tip → mesaj (409 reactiv); relație cu sine → blocat.

### F. FE3 — anexe (S55)
- [ ] **Adăugare anexă** (dialog: fișier, denumire, tip document HCL, descriere, ordine);
      **`Nr. ordine anexă` apare doar pentru tipul „Anexă"** (reactiv).
- [ ] Duplicat de nr. ordine anexă → 409 reactiv.
- [ ] Descărcare (blob), editare metadate, ștergere.
- [ ] **Imutabilitate pe HCL Semnat**: la editare, tipul + nr. ordine sunt **dezactivate** (cu hint);
      denumirea/descrierea/ordinea rămân editabile.
- [ ] Adăugarea unei anexe noi e permisă și pe Semnat (backend o acceptă).

### G. FE3 — widget T-3 pe Acasă (S55)
- [ ] Widgetul „HCL urgent de comunicat" apare pe Acasă **doar pentru Admin/Secretar** (nu Consilier).
- [ ] Cod culoare pe zile rămase: **roșu** depășit (`<0`), **portocaliu** `0–1`, **verde** `≥2`;
      text corect („Depășit cu N zile" / „Expiră azi" / „N zile lucrătoare rămase").
- [ ] Fiecare rând = link la HCL.
- [ ] Un HCL **dispare** din widget după ce i se adaugă o comunicare; **reapare** dacă i se șterge.
- [ ] Stare goală („Nicio hotărâre…") când nu există candidați.

### H. Transversale (de urmărit pe parcurs)
- [ ] **Date/fus**: datele `DateOnly` (comunicări, termene T-3) se afișează „dd.MM.yyyy" **fără oră
      parazită**; datele cu oră (adoptare) cu fusul instituției.
- [ ] **Gărzi reactive**: orice 400/409 apare ca mesaj clar (inline în dialog / snackbar la acțiuni
      din listă), nu ca eroare brută.
- [ ] **Consolă fără erori** + **fără cereri eșuate** pe fluxurile principale.
- [ ] **Lazy routes** se încarcă (Registru, detalii HCL).

---

## Rolul meu (Claude) în sesiunea de testare
1. Citesc acest ghid + confirm mediul (secțiunile de pregătire).
2. Confirm/ajut la pregătirea datelor de test (ideal un HCL nou pentru ciclul complet).
3. Te conduc **pas cu pas** prin secțiunile A→H: pentru fiecare scenariu îți spun ce să faci și
   **ce trebuie să vezi**; tu îmi confirmi.
4. Notez rezultatele; la ce nu merge, diagnostichez și propun fix (acum sau separat).
5. La final: raport scurt (trecut / probleme) — și, dacă vrei, curățăm datele de test.

## Note
- **Nu testăm acum** — acest document e doar ghidul; testele efective se fac în sesiunea următoare.
- Acoperă tot Modul A (FE1+FE2+FE3). **Paginile publice HCL NU intră** (sunt Faza 9).
- Referințe utile: `docs/rezumate/rezumat_53/54/55.md`, `docs/planuri/plan_faza3_frontend.md`.
