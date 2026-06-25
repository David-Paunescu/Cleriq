# Cleriq — Probleme observate

Toate problemele de mai jos sunt **non-blocante pentru MVP**. Sunt documentate aici pentru a nu fi redescoperite și pentru a fi adresate în sesiuni dedicate de polish.

## Polish / cosmetice

- **Dublu mesaj la status 0 pe login** — interceptorul dă snackbar pe status 0 inclusiv pe rutele auth, iar login afișează și inline.
- **Persistare filtru în URL la liste** — revenire cu filtre intacte (opțional).
- **Suprapunere text coloana Tip peste iconița info pe tab Documente sub 790px viewport** — soluție probabilă: `ViewEncapsulation.None` sau stiluri global pentru tabele Material.
- **Editarea unui punct cu Rezultat setat e permisă** — semantic OK (corectare typo), eventual mesaj informativ post-MVP.
- **Închiderea votului fără voturi reușește tehnic** și produce "Respins, 0-0-0". Backend corect, UX-ul ar putea avertiza înainte.
- **`app.spec.ts` default picat** după înlocuirea componentei inițiale — la strategia teste frontend.
- **Buton refresh pe tab Prezență** — util doar la multi-user simultan; pe single-user pare inutil.
- **Pagina de detalii NU reîncarcă la "Editează → Renunță"** dacă forma nu a salvat — comportament corect.

## Capcane cunoscute (workaround-uri active)

- **Tooltip pe `disabled` cross-browser broken** (Chrome/Edge/Safari) — `mouseenter` nu se declanșează pe disabled. `matTooltipDisabledInteractive` nu a funcționat în mediul nostru. **Workaround activ**: "snackbar avertizant la click" pentru pre-condiții tranzitorii.
- **Windows 11 + Edge/Chrome file picker** cu `accept=".pdf"` ascunde implicit non-PDF, dar user poate schimba la "All files". **Defense in depth**: validare client + gardă backend rămân ambele necesare.
- **Auto-save retry infinit pe eroare persistentă** (pattern Transcriere + PV) — effect retransmite la 2s la nesfârșit dacă backend e oprit prelungit. Acceptabil în praxis. Backoff exponențial = polish opțional.

## Decizii fundamentale deschise

### JWT nu conține FusOrar

Frontend hardcodează `Europe/Bucharest`. Corect pentru 100% din cazurile reale acum. Migrarea la prima instituție pe alt fus = claim `FusOrar` în JWT SAU endpoint "instituția mea" + extindere `AuthService.utilizator`. Atinge un singur loc.

### Dark mode

Badge-urile semantice (status ședință, status punct, status prezență, status trimitere, status convocare — 5+ feature-uri) folosesc culori hardcodate. Re-evaluare arhitectură la decizia de implementare — probabil tokens M3 cu paletă semantică custom.

### Strategie teste frontend

Vitest instalat (din scaffold-ul Angular), nu folosit. Sesiune dedicată — decizii: ce testăm (logică pură vs componente vs E2E), nivel de coverage țintit, mocking strategy.

## Probleme moștenite (acceptate / amânate)

- **Audit cine-fields NULL** pe rânduri pre-s4 — non-blocant.
- **PV stale pe portal după regenerare** — design acceptat (cache TTL 1h pe varianta publică finalizată).
- **`SaveChanges` audit+cascadă sincron** — trade-off acceptat; revizuire la load test.
- **Idempotency wrapper Whisper** — succes "pierdut" la SaveChanges eșuat post-răspuns OK → reprocesare, cost GPU dublu. Acceptat pentru pilot.
- **Duplicat `StocareDocumenteDisk` + `StocareAudioDisk`** (~175 linii) — refactor `StocareDiskBase` la a 3-a stocare.
- **Test integration pentru wrapper Whisper** — bug-ul Form-vs-Query din s25 ar fi fost prins instant. Vezi `teste.md` pentru pattern-ul `HandlerHttpFals`.
- **Cod orphaned hotwords** — câmp mort funcțional în `GeneratorPromptTranscriere`; cleanup minor la următoarea atingere.
- **Nume audio la download fără extensie corectă** — îmbunătățire prin câmp `NumeFisierOriginal` pe `Transcriere`.
- **Hot Reload VS** nu aplică schimbări de constructor/câmpuri — capcană de mediu, NU bug. Stop → Rebuild → Start.
- **CI Linux** — testele de mentenanță depind de `File.SetCreationTimeUtc`; pe Linux birth time nu e setabil. Sesiune dedicată pre-deployment.

## Flaky cunoscut în teste

- **`TesteRateLimiting.RutePublice_PesteLimita_429CuRetryAfter_ApoiFereastraNouaPermite`** — sensibil la timing (GC/build paralel întârzie un milisecund critic). Re-rulat izolat → verde. Non-blocant. Soluție viitoare: mărire marjă sau retry intern în test.
