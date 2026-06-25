# Cleriq — Frontend

## Stack tehnologic

Angular v22 (zoneless = default, fără provider explicit, fără zone.js), Angular Material (Azure/Blue, M3), SCSS, standalone components + signals, Reactive Forms (NU Signal Forms — reevaluare ~1 an), ESLint (angular-eslint), TypeScript strict. **Fără SSR** — decizia pentru portalul public vine la sesiunea lui dedicată.

`LOCALE_ID = 'ro'` + `registerLocaleData(localeRo)` în `app.config.ts`. `index.html` cu `lang="ro"` (accesibilitate) + title Cleriq.

**Strategie versiuni**: fiecare major Angular are ~18 luni suport → update major ~1/an, în sesiune dedicată (`ng update` + lint + build + smoke). Pinning prin `package-lock.json` comis. NU se îngheață versiunea ani de zile (securitate + achiziții publice).

## Convenții cod

**Limba**: domeniu în română, termeni Angular în engleză. Mesajele de eroare către utilizator — în română.

**Naming**:
- Componente fără sufix în numele fișierului (stil CLI v22): `login.ts`, `sedinta-form.ts`, `prezenta-tab.ts`
- Infrastructura `core/` cu sufixe clasice: `.service.ts`, `.guard.ts`, `.interceptor.ts`
- Interfețe TS camelCase, oglinda JSON-ului API

**Reactive Forms = standardul tuturor formularelor**. `async/await` cu `firstValueFrom` (NU `subscribe`). Stare în signals. Interceptorul de erori rămâne strict transversal + rethrow; tratarea de business stă în componente.

**Orice pagină nouă = lazy `loadComponent`**. Guard pe roluri se adaugă la prima rută care îl cere (acum `rolGuard` din `core/auth/` e cod mort temporar — primul consumator real va fi la SuperAdmin UI).

## Structură proiect

```
cleriq-web/src/
├── app/
│   ├── core/                  # transversale, singleton
│   │   ├── auth/              # AuthService, authGuard, authInterceptor, rolGuard
│   │   ├── http/              # eroareInterceptor, erori.ts (extrageMesajEroare)
│   │   ├── modificari/        # ModificariNesalvateService + ghidModificariNesalvate
│   │   └── config.ts          # FUS_INSTITUTIE
│   ├── shared/                # reutilizabile fără stare
│   │   ├── confirmare/        # ConfirmareDialog
│   │   ├── data.ts, enums.ts, etichete.ts, text.ts
│   ├── features/X/            # per entitate
│   │   ├── X.models.ts
│   │   ├── X.service.ts
│   │   ├── X.permisiuni.ts    # oglindă matrice backend
│   │   └── x-componenta/      # .ts + .html + .scss
│   └── layout/shell/          # o singură intrare
├── environments/              # dev: apiUrl https://localhost:7006 / prod: '' (relative)
└── index.html
```

**Servicii API per feature** (`features/X/x.service.ts`) returnează `Promise<T>` (`firstValueFrom` intern). Core = doar transversale. Metodele se adaugă când au consumator (ex: `ConsilieriService` NU are `Detalii` — lista conține tot, dialogul primește rândul).

**Componente shared** sub `shared/`. Template+styles inline acceptate pentru componente minuscule (ex: `ConfirmareDialog`).

## Auth + interceptori

**`AuthService`** (singleton):
- 2 signals: `utilizator` (readonly), `esteAutentificat` (computed)
- `token` (getter) și `refreshToken` în localStorage (chei `cleriq.token`, `cleriq.refreshToken`)
- **Decodare JWT proprie**: `base64url` + `TextDecoder('utf-8')` pentru diacritice în `NumeComplet`. Claim-uri scurtate (`nameid`, `email`, `role`) cu fallback pe URI-urile lungi `.NET` (`http://schemas.xmlsoap.org/...`, `http://schemas.microsoft.com/...`).
- **Single-flight pe refresh**: `Promise` partajat (`refreshInFlight`) — N×401 simultane = 1 apel `/refresh`. Resetat în `.finally()`.
- **Recuperare cursă multi-tab**: la refresh eșuat, recitește localStorage; dacă alt tab a rotit deja, adoptă sesiunea lui (decodează tokenul din storage, setează `utilizator`, returnează `true`).
- `incarcaDinStocare()` în constructor: token expirat/corupt → curățat silențios. Refresh token-ul NU se șterge când access-ul e expirat (cazul normal de revenire după pauză).
- `logout` = fire-and-forget POST către backend + curățare locală + navigare la `/login`. Eșec offline → familia rămâne vie server-side până la expirare (trade-off acceptat).
- `areRol(rol)` / `areOricareRol(...roluri)` pentru meniu + guards.

**`authInterceptor`**: Bearer atașat DOAR pe `{apiUrl}/api` (tokenul nu se scurge spre URL-uri externe).

**`eroareInterceptor`**: tratează transversalele + rethrow mereu:
- Status 0 → snackbar "Serverul nu poate fi contactat"
- 401 pe rute non-`/api/Auth/` → `refresh` → retry O DATĂ cu tokenul nou → `logout` la eșec (inclusiv 401 pe retry)
- 401 pe rute `/api/Auth/` → re-aruncă (backend răspunde 401 la parolă greșită — login-ul tratează inline)
- 403 → snackbar "Nu ai permisiunea necesară"
- 400/404/409 → re-aruncă; componentele tratează cu mesajele backend

**`authGuard` async**: încearcă refresh înainte de redirect la login (scenariul "consilier revine după 3 săptămâni"). Eșec → redirect cu `queryParam redirect=originalUrl`.

## Routing

Lazy `loadComponent` peste tot. `shell` ca parent cu copii pentru rutele autentificate.

**Segmente fixe ÎNAINTEA celor cu parametru**: `sedinte/noua` și `sedinte/:id/editeaza` ÎNAINTE de `sedinte/:id` (altfel `noua`/`editeaza` ar fi interpretate ca id).

**`canDeactivate: [ghidModificariNesalvate]`** pe rute cu editor (acum `sedinte/:id` care conține tab Transcriere și tab PV). Pattern reutilizabil pentru orice rută cu sub-componente care editează.

## Pattern-uri Angular 22 zoneless

**`toSignal(form.controls.X.valueChanges, { initialValue: form.controls.X.value })`** = PATTERN OBLIGATORIU pentru reactivitate template pe form values. NU `form.controls.X.value` direct în `@if` (nu se actualizează fără zone.js). Aplicabil la orice dialog cu toggle/select care ascunde/arată câmpuri condiționat.

**`BreakpointObserver`**: media query EXPLICIT pe lățime (`'(max-width: 959.98px)'`), NICIODATĂ `Breakpoints.Handset` (combină lățime+orientare → flip-flop real la rotații portrait/landscape). Convertit la signal cu `toSignal(...pipe(map(r => r.matches)), { initialValue: false })`.

**`effect` cu `onCleanup(clearTimeout)`** — pattern Angular signal-native pentru debounce, fără RxJS:
```typescript
effect((onCleanup) => {
  if (!this.esteDirty() || this.seSalveaza()) return;
  const timer = setTimeout(() => this.salveazaImediat(true), DEBOUNCE_MS);
  onCleanup(() => clearTimeout(timer));
});
```

**Effect sync DOM↔signal pentru `<textarea>`**: `viewChild` ref + setare `nativeElement.value` DOAR dacă diferă de signal. Pattern `[value]="signal()"` resetează cursor la fiecare keystroke — INTERZIS pentru textarea editate.

## Pattern-uri arhitecturale

**Formular shared creare/editare** cu mode dedus din rută (prezența `:id`). Generalizare a pattern-ului dialog. STANDARD pentru toate entitățile complexe. Ex: `SedintaForm` servește atât `/sedinte/noua` cât și `/sedinte/:id/editeaza`. Dialog rămâne pentru entități mici cu puține câmpuri (ex: Consilier — 4 câmpuri).

**Pagina hub cu `mat-tab-group`** de la primul tab (chiar dacă e solo). Crește aditiv pe măsură ce adăugăm funcționalitate. Tab "Detalii" este DISPLAY semantic `<dl>`; butoanele de acțiuni stau în antet, NU în tab.

**Servicii API per feature** returnează `Promise<T>` cu `firstValueFrom` intern.

**Permisiuni client în `features/X/X.permisiuni.ts`** ca oglindă strictă matrice backend (`Valideaza*` + gărzile PUT/DELETE/POST). Frontend NU mai face cereri care ar primi 409. Drift între backend și frontend = bug de oglindire.

**Validări la nivel client NU se duplică unde sursa unică e backend**:
- Telefonul NU se validează pe client — sursa unică = `Telefon.Normalizeaza` (backend)
- Dublarea regex-ului ar crea două surse divergente

## Editor cu auto-save (`ModificariNesalvateService`)

**Infrastructură reutilizabilă** în `core/modificari/`:
- `ModificariNesalvateService` singleton cu pattern `register`/`unregister` peste un `Map<id, ProprietarStareModificari>`. Expune `areModificariNesalvate()` agregat.
- `ghidModificariNesalvate: CanDeactivateFn` care întreabă serviciul + deschide `ConfirmareDialog` (`periculos: true`).
- Înregistrare cu `id` unic per componentă (`transcriere-tab-${sedintaId}`, `proces-verbal-tab-${sedintaId}`). Coexistă în Map — `areModificariNesalvate` returnează `true` dacă ORICE proprietar are modificări.

**Editor în componentă**:
- Textarea cu `viewChild('textareaEditor')` + effect sync DOM↔signal (cursor protejat)
- Signal `valoareEditor` + signal `ultimaValoareSalvata`. Computed `esteDirty = valoareEditor() !== ultimaValoareSalvata()`.
- Auto-save debounce 2s prin effect cu `onCleanup(clearTimeout)`
- `Ctrl+S` via `window.addEventListener('keydown')` cu restricție `tabActiv()`, cleanup în `ngOnDestroy`
- `visibilitychange` → save silent fire-and-forget la hidden
- Hint sub editor: "Auto-save la 2 secunde · Ctrl+S pentru salvare imediată"

**`salveazaImediat(silent: boolean)`** cu guard dublu `if (!esteDirty() || seSalveaza()) return`. Garanție vizibilitate min 600ms pentru indicatorul "Se salvează..." — DOAR dacă `!esteDirty()` post-save (nu blocăm tastarea continuă).

**CRITIC după save**: dacă endpoint-ul backend returnează entitatea părinte (ex: `editeazaContinut` returnează `Transcriere`, NU `TranscriereContinut`), pattern obligatoriu:
```typescript
this.transcriere.set(rezultat);
this.continut.update(c => c ? { ...c, continutEditat: valoare } : null);
```

**Force-save înainte de Finalizare / Publicare**: dacă `esteDirty()`, apelăm `salveazaImediat(true)` și verificăm `eroareSalvare() || esteDirty()` — la eșec, snackbar + abort. NU se trimite endpoint-ul de finalizare/publicare cu save în eroare.

**Lock signal care blochează MULTIPLE căi paralele** (`actiuneInCurs`, `publishInCurs`):
- Textarea
- Butoanele
- Auto-save effect
- `visibilitychange` handler
- `Ctrl+S` handler

Inventariere completă obligatorie — orice cale uitată = race condition.

**Eliminat butonul "Salvează" manual**. Pe save rapid (~50ms localhost), butonul devine disabled instant — zgomot. Auto-save 2s + Ctrl+S + indicator clar sunt suficiente.

## Indicator stare salvare

**Slot fix** `min-width: 160px` în antetul editorului (stânga). Butoanele din dreapta nu se mai mișcă la schimbarea stării.

**4 stări exclusive prioritizate**:
1. Eroare salvare → `stare-eroare` (roșu)
2. Dirty persistent (>10s) și nu se salvează → `stare-dirty` (portocaliu)
3. Are dată ultimă salvare → `stare-salvat` (verde) cu "Salvat la HH:MM" + spinner discret în timpul save (ora se actualizează la final)
4. Se salvează fără dată anterioară → "Se salvează..."

**Container fix 14×14px** pentru icon și spinner în indicator:
```scss
.icon-indicator,
.indicator-stare mat-spinner {
  flex-shrink: 0;
  width: 14px !important;   // Material setează width/height inline → !important obligatoriu
  height: 14px !important;
  font-size: 14px;
}
```

**Effect dirty persistent cu DUBLU timer**:
- 2s pentru save acțiune
- 10s pentru semnal vizual "Modificări nesalvate"
- La fiecare keystroke AMBELE se anulează (onCleanup)

Dirty persistent >10s = semnal de anomalie (auto-save eșuat silent), NU zgomot la tastare rutinieră.

**Indicator continuu cu spinner discret, NU tranziție între texte**. "Salvat la HH:MM" mereu vizibil; spinner ia locul check-ului în timpul save-ului. Mai puțin zgomot vizual la operații rapide.

## Publish flow snapshot

Pattern aplicat la Transcriere, reutilizabil pentru orice tab cu publicare distinctă de editare draft.

**Computed `starePublicare`**: `'nepublicat'` / `'publicat-curent'` / `'publicat-modificari'`
- `publicat-modificari` = `ContinutEditat !== ContinutPublicat`, **comparație FĂRĂ `.trim()`** — whitespace diferit = stare diferită = update legitim

**Badge stare publicare** în antetul editorului (3 culori + 3 iconițe semantice).

**Buton publish cu 2 etichete** (NU 3):
- "Publică pentru consilieri" la `nepublicat`
- "Actualizează versiunea publicată" la celelalte 2 (disabled la `publicat-curent`, enabled portocaliu la `publicat-modificari`)

Eliminat "Republică" — stare intermediară 0-utilitate (buton mereu disabled între publicare și prima modificare).

**Permisiune calculată DIN starea conținutului**, nu doar din status. Semnătura `actiuniPermise(status, continut, esteAdmin, ...)` primește și `continut` — oglindește exact garda backend "editat ne-vid".

**POST `/Publica` idempotent** = primul publish + republicare în același endpoint. Mai simplu decât 2 endpoint-uri separate.

**`continutPentruConsilier` citește `continutPublicat`**, NU `continutEditat`. Bug-ul de privacy "secretarul tastează, consilierul vede instant" eliminat.

**`starePublicare` citește DOAR din `continut()`**, audit (`dataPublicare`) citește din `transcriere()`. Separarea celor 2 signal-uri rămâne pentru perf.

## Hub Ședință cu 8 tab-uri

Toate tab-urile sunt în `features/sedinte/sedinta-detalii/`. `mat-tab-group` ține componentele cu `display: none` (NU le distruge). `ngOnDestroy` NU se apelează la comutare tab.

**Tab-urile** (în ordinea afișată):
1. **Detalii** — DISPLAY `<dl>` cu datele ședinței. Butoane status (Începe / Finalizează / Anulează / Editează / Șterge) în ANTETUL ședinței, NU în tab.
2. **Ordine de zi** (`PuncteTab`) — CRUD puncte + tranziții (Închide vot / Amână / Retrage). Default ordine la adăugare = `MAX + 1`. `TipVot` imutabil dacă există voturi (gard backend, 409).
3. **Prezență** (`PrezentaTab`) — dropdown status per consilier + card cvorum live cu breakdown + bulk "Marchează toți prezenți" și "Resetează prezența". Cvorum calculat DOAR pe lista completă (filtrul afectează doar afișarea, NU calculul legal). `OraSosire` ASCUNSĂ complet (câmp orfan backend; activare = pachet întreg).
4. **Voturi** (`VoturiTab`) — accordion cu 5 moduri body comutate pe rol + status punct: `rezultat-inchis`, `tabel-management`, `chip-self`, `mesaj-secret`, `mesaj-neprezent`. `VotulMeu` din claim `ConsilierId` (server-side privacy).
5. **Documente** (`DocumenteTab`) — pre-populare smart la upload + toggle vizibilitate. Pattern download via blob.
6. **Convocări** (`ConvocariTab`) — polling 3s tab-aware + reset complet + retry per rând. Emite `output sedintaSchimbata` pentru refetch ședință din hub.
7. **Transcriere** (`TranscriereTab`) — upload cu progress + viewer brut cu segment-uri clickabile (seek audio) + editor cu auto-save + publish flow snapshot. Drag-drop pe zonă goală. Polling tab-aware pe statusuri `InAsteptare`/`InProces`.
8. **Proces Verbal** (`ProcesVerbalTab`) — generare draft + editor cu auto-save + finalizare ireversibilă + upload PDF semnat extern. PV finalizat = ireversibil (OUG 57/2019). Card verde "Variantă semnată" cu metadate + acțiuni proprii când e atașat.

**Pattern polling tab-aware**: signal `indexTabActiv` în `sedinta-detalii` + input `tabActiv` în componenta tab. Effect activează/dezactivează polling în funcție de `tabActiv() && existaCondiție()`.

**Pattern schimbare status ședință din tab**: `output sedintaSchimbata` emis din componenta tab → metodă publică `reincarcaSedinta()` în hub care refetch ședința. Aplicabil la orice tab care declanșează tranziție backend (Convocare→Convocată, etc.).

**Aprobare oficială PV (s45)**: componenta `features/proces-verbal/aprobare-dialog/` cu dropdown filtrat (Convocata/InDesfasurare/Finalizata, exclus sedinta proprie, sortat descrescător), 2 avertismente independente la selecție (cronologic anterior + status Convocata), confirmare finală sub buton. Pattern "dialog cu warning-uri condiționate la selecție" reutilizabil pentru orice acțiune cu pre-condiții slabe vs hard guards.

Card status extins cu badge "Aprobat oficial" verde + linie detaliată (data + ședință) + meniu ⋮ "Anulează aprobarea" (Admin only). Pattern reutilizabil pentru viitoare stări legale ortogonale (HCL emis, dispoziție publicată).

`actiuniPermise` extins cu parametru `areAprobare` — oglinda fidelă a gărzilor backend D7+D8.

## Date / timp

**`FUS_INSTITUTIE = 'Europe/Bucharest'`** centralizat în `core/config.ts`. JWT NU conține `FusOrar` (decizie acceptată; migrare la prima instituție pe alt fus = un singur loc: claim FusOrar în JWT SAU endpoint "instituția mea" + extindere `AuthService.utilizator`).

**5 funcții utilitare** în `shared/data.ts`:
- `inputLocalLaUtcIso(input)` — picker `datetime-local` → ISO UTC pentru API
- `utcLaInputLocal(iso)` — UTC din API → string `datetime-local`
- `formateazaDataOra(iso)` — afișare lungă
- `formateazaDataScurta(iso)` — afișare scurtă în liste
- `indicatorFus(iso)` — sufix UTC+N pentru afișare

**Toate folosesc `Intl.DateTimeFormat` cu `timeZone` explicit** — NICIODATĂ `new Date()` local (browser-ul nu trebuie să decidă fusul). Internal helper `offsetFusOrar` calculează offset-ul prin diferența `Date.UTC(formatToParts)` − `date.getTime()`.

**Picker `datetime-local` nativ HTML5** + conversie explicită UTC↔fus instituție. Hint vizibil "Fus orar instituție: Europe/Bucharest".

## Download autenticat

**`window.open(url)` NU trece prin `authInterceptor` → 401**. Folosim mereu:
```typescript
const blob = await firstValueFrom(
  this.http.get(url, { responseType: 'blob' }));
const u = URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = u;
a.download = numeFisier;
document.body.appendChild(a);
a.click();
document.body.removeChild(a);
URL.revokeObjectURL(u);
```

**Cleanup obligatoriu `URL.revokeObjectURL`** la:
- Unsubscribe (audio player cu `obtineBlobAudio`)
- Upload nou (curăță audio vechi)
- Ștergere

## Upload cu progres (Transcriere)

**`HttpRequest` cu `reportProgress: true`** wrapped în `Observable<ProgresUpload>`. Eveniment `HttpEventType.UploadProgress` → procent + bytes. `HttpEventType.Response` → rezultat final.

```typescript
return new Observable<ProgresUpload>(observer => {
  const sub = this.http.request<T>(cerere).subscribe({
    next: (event) => { /* progres / response */ },
    error: (err) => observer.error(err)
  });
  return () => sub.unsubscribe();
});
```

Buton cancel = `subscriere.unsubscribe()`.

## Convenții UX

**Erori în dialog → inline** (dialogul rămâne deschis); **acțiuni directe din listă → snackbar**.

**Snackbar standard 4s**; **8s pentru rezultate cu cifre/tally** (ex: Închidere vot — "Adoptat. 7 Pentru, 3 Împotrivă, 2 Abținere (prag necesar: 6 din 11)").

**Confirmare cu `periculos: true`** pentru ireversibile (Finalizează, Anulează, Șterge, Reset, Resetare versiune brută, Retragere publicare).

**Confirmare ÎNAINTE de file picker** pentru replace document oficial (PV semnat). Pattern complementar la confirmarea POST-acțiune din pattern uzual.

**După mutație în listă: reload de la server**, NU patch local (sortarea = SQL Server, colare diferită de JS).

**Pe pagina de detalii: setare directă din răspunsul API** la tranziție (entitatea actualizată = un singur round-trip).

**Acțiunea principală vizibilă** (iconiță directă) **vs acțiuni rare în meniu `⋮`**. Distructive în meniu (Șterge, Reset, Resetează la versiunea brută). Edit + acțiunea cheie vizibile, restul ascunse.

**Subtitlu pe rândul de listă** cu metadate concatenate (`Tip · TipVot · Majoritate`) — reduce coloane vizibile.

**Bulk action**: secvențial cu contor "X/N" pe buton, respectă filtrul curent. Fără minimum visibility time artificial (sub 300ms invizibil pe localhost — comportament onest). Include `tip: 'prezent' | 'reset'` ca progresul să se afișeze pe butonul activ.

**Pessimistic update cu lock per rând** (Set chei) — spinner mic + dropdown dezactivat doar pe rândul în curs.

**Computed signals client-side pentru calcule derivate** (cvorum din lista de prezențe — zero apeluri suplimentare la backend dacă datele sunt deja avute).

**`normalizeazaPentruCautare`** (NFD + strip diacritice — "stefan" găsește "Ștefan") în `shared/text.ts`. Refolosibil la toate listele cu căutare.

**Badge-uri semantice cu culori hardcodate** (status ședință, status punct, status prezență, status trimitere). Re-evaluare arhitectură la implementare dark mode.

**"Snackbar avertizant la click"** în loc de disabled+tooltip — pentru butoane care depind de pre-condiție temporară (dirty, în curs). Click triggerează snackbar care explică blocarea. Reutilizabil pentru orice acțiune cu pre-condiție tranzitorie.

**State machine UI per status entity**: 5 stări vizuale pe rol+status la PV (empty / editor / read-only Consilier pe Draft / read-only Finalizat / variantă semnată). Aplicabil la orice tab cu citire+editare diferențiată pe rol și status.

**Card secundar condițional pe stare entity** — verde pentru "ceva oficial există atașat" (cu metadate + acțiuni proprii). Reutilizabil la ActNormativ (HCL emis), erată, alte documente cu statut special.

**Single hidden input + multiple butoane de declanșare** — un singur `<input type="file" hidden>` per scop, `viewChild` + `.click()` din N butoane.

**Computed pentru button primary/stroked switch dinamic** — Material 18 nu suportă switch runtime între `mat-flat-button` și `mat-stroked-button` într-un singur element; soluția = două butoane în `@if/@else` cu computed care alege.

## Helper `extrageMesajEroare`

`core/http/erori.ts` — acoperă toate formele backend într-un singur loc:
- string (BadRequest/Conflict)
- array Identity `{description}[]`
- `ValidationProblemDetails` cu `errors: {[key]: string[]}`
- `{mesaj}` (EroareSlugDto)
- `{title}` (problem details fallback)
- status 0 → "Serverul nu poate fi contactat"
- fallback generic

## Material gotchas

**`color="warn"` NU funcționează pe M3** — override token-uri pe AMBELE generații (`--mdc-filled-button-*` ȘI `--mat-button-filled-*`). Necesar pentru butoane destructive în dialog confirmare.

**Tooltip pe `disabled` cross-browser broken** (Chrome/Edge/Safari) — limitare browser nativă: `mouseenter` nu se declanșează pe elemente disabled. `matTooltipDisabledInteractive` (Material v18+) nu a funcționat în mediul nostru. Workaround = "snackbar avertizant la click" sau wrapper `<span matTooltip>` peste buton (acceptă mouse events).

**`mat-tab-group` ține componentele cu `display: none`** — `ngOnDestroy` NU se apelează la comutare tab. Pentru polling tab-aware folosim input `tabActiv` + effect.

**Iconițe în `mat-select`**: pentru aliniere în overlay-ul `mat-option`, folosim iconiță invizibilă cu `visibility: hidden` în loc de absența ei (păstrează aliniamentul).

## Persistență opțiuni UI

**localStorage prin effect** — citire în constructor cu fallback default, save prin effect care urmărește signal-urile:
```typescript
effect(() => {
  this.afiseazaSpeaker();  // urmărește signal
  this.afiseazaTimestamp();
  this.salveazaOptiuni();
});
```

Cheie cu prefix `cleriq.X.optiuni`. Parsing tolerant cu fallback la default pe `try`/`catch` (quota exceeded / private browsing).

Pattern reutilizabil pentru orice preferință utilizator per-componentă (opțiuni vizualizare transcript, preferințe sortare, etc.).

## Erori cunoscute / cosmetice acceptate

- Dublu mesaj la status 0 pe login (interceptor + inline) — cosmetic.
- Suprapunere minoră text coloana Tip peste iconița info pe tab Documente sub 790px viewport — soluție la sesiunea polish UI (`ViewEncapsulation.None` sau stiluri globale).
- Pagina de detalii NU reîncarcă la "Editează → Renunță" dacă forma nu a salvat — comportament corect (datele nu s-au schimbat).
- Buton refresh pe tab Prezență util doar la multi-user simultan — pe single-user pare inutil.
- Editarea unui punct cu Rezultat setat e permisă — decizie semantică (corectat typo OK; doar `TipVot` e imutabil dacă există voturi).
- Închiderea votului pe punct fără voturi reușește tehnic și produce "Respins, 0-0-0" — backend corect, UX-ul ar putea avertiza înainte (post-MVP).

## Setup dev (mențiune scurtă)

Node ≥24.15 sau ≥22.22.3 (pentru CLI v22). `environment.development.ts` cu `apiUrl: 'https://localhost:7006'`. `ng serve` pe `:4200`, `ng lint`, `ng build` pentru producție.

Detalii reproducere mediu complet (backend + Redis + SQL Server + credențiale dev) în **`setup.md`**.

## Roadmap

Etapele frontend (PV semnat gardă aprobare, ActNormativ, Portal public cu decizia SSR, SuperAdmin/SMTP UI, Management conturi) sunt în **`roadmap.md` comun cu backend** — separarea ar fragmenta context-ul când planificăm o etapă.
