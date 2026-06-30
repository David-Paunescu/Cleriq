# Analiză juridică — Completitudinea enum-ului `MotivInvalidare` pentru HCL (Cleriq)

## TL;DR
- **Cele 4 motive actuale NU sunt exhaustive juridic** și una dintre ele este greșit denumită. Lipsesc cel puțin: caducitatea (expirarea termenului / dispariția obiectului), inexistența pentru nepublicare în MOL și ipoteza anulării de instanță la cererea propriei autorități emitente (art. 1 alin. (6) Legea 554/2004) — cu o limitare importantă pentru HCL normative.
- **Formularea „Anulat de prefect" este greșită juridic.** Prin tutela administrativă (art. 123 alin. (5) din Constituție, art. 255 Cod administrativ, art. 3 Legea 554/2004), prefectul NU anulează — el doar *verifică legalitatea* și *poate ataca* actul în instanță; actul atacat este *suspendat de drept*, iar INSTANȚA de contencios administrativ este singura care anulează.
- **Recomandare:** reformulați lista — separați „atacat de prefect" (stare temporară de suspendare, NU invalidare) de „anulat de instanță"; adăugați `Caduc`, `Inexistent` și `Altul` cu câmp text liber.

---

## Key Findings

### (a) Toate modurile juridice de încetare a efectelor / invalidare a unui act administrativ

Doctrina română de drept administrativ (Iorgovan, Drăganu, R.N. Petrescu, Podaru, Vedinaș) și legislația identifică următoarele moduri prin care un act administrativ încetează să producă efecte:

1. **Anularea** — desființarea actului pe cale jurisdicțională, de către instanța de contencios administrativ, pentru nelegalitate; produce, de regulă, efecte retroactiv (ex tunc). *[Text de lege: Legea 554/2004, art. 1, art. 8, art. 18.]*
2. **Revocarea (retractarea)** — manifestarea de voință a organului emitent de a-și retrage propriul act, pentru nelegalitate sau inoportunitate (la nivel local: retractarea unui HCL de către consiliul însuși). Este regula în materia actelor administrative, dar cunoaște excepții (mai jos). *[Doctrină + Legea 554/2004 art. 7 alin. (1).]*
3. **Abrogarea** — scoaterea din vigoare a unui act normativ ca urmare a intrării în vigoare a unei norme noi; este un *eveniment legislativ*. *[Text de lege: Legea 24/2000, art. 58, art. 64.]*
4. **Caducitatea** — încetarea efectelor printr-un *fapt exterior voinței* organului emitent: împlinirea termenului (act temporar), dispariția obiectului sau a destinatarului, realizarea condiției, epuizarea conținutului, desuetudinea. Operează ex nunc, de drept. *[Doctrină: Ovidiu Podaru, „Caducitatea actului juridic în dreptul public", Hamangiu, 2019.]*
5. **Suspendarea** — încetarea **temporară** a efectelor (nu definitivă) — deja modelată ca relație în Cleriq.
6. **Inexistența** — sancțiune pentru lipsa unui element esențial; în special **nepublicarea** atrage inexistența pentru actele a căror obligativitate depinde de aducerea la cunoștință publică.

### (b) Cazuri care LIPSESC din cele 4 valori actuale

**Caducitatea (expirarea termenului) — LIPSEȘTE și este foarte relevantă practic.** Numeroase HCL au caracter temporar: de pildă hotărârile privind impozitele și taxele locale, care prevăd expres „prezenta hotărâre intră în vigoare începând cu anul fiscal" respectiv (formulare uzuală în HCL de taxe — ex. HCL nr. 557/2025 Sfântu Gheorghe, art. 17: „Prezenta hotărâre intră în vigoare începând cu anul fiscal 2026"), precum și bugetul local anual. Un HCL cu durată determinată își încetează efectele la împlinirea termenului, fără niciun act ulterior. Doctrina (Podaru) reține caducitatea ca o *stare* a actului care determină ieșirea din vigoare printr-un fapt exterior, distinctă de anulare, revocare și abrogare — „caducitatea (...) reuneşte toate cazurile de dispariţie a obiectului sau a destinatarului actului, ajungerea la termen, precum şi multe alte cazuri".

**Nulitatea constatată / inexistența pentru nepublicare — LIPSEȘTE.** Doctrina reține clasificarea nulitate absolută / relativă / inexistență, dar — spre deosebire de dreptul civil — distincția absolut/relativ are relevanță practică redusă în contencios: Legea 554/2004 nu diferențiază între acțiunile în nulitate absolută și relativă, instanța putând anula în tot sau în parte „indiferent de motiv". Relevantă concret pentru un HCL este **inexistența pentru nepublicare**. Codul administrativ, **art. 198 alin. (1)-(2)** (text de lege citat): „(1) Hotărârile şi dispoziţiile cu caracter normativ devin obligatorii de la data aducerii lor la cunoştinţă publică. (2) Aducerea la cunoştinţă publică (...) se face în termen de 5 zile de la data comunicării oficiale către prefect." Sancțiunea inexistenței pentru nepublicare este consacrată jurisprudențial: prin **Decizia CCR nr. 513 din 24.09.2019** (M. Of.), Curtea reține că fostul art. 3 alin. (2) din HG nr. 358/1991 prevedea expres că nepublicarea în Monitorul Oficial „atrage inexistența" actelor normative, iar un act administrativ „lovit de inexistenţă nu se bucură de prezumţia de legalitate şi nici de executarea din oficiu". (A se vedea și jurisprudența ICCJ în materia hotărârilor nepublicate — Decizia ICCJ nr. 3883/2021.)

**Revocarea în condițiile legii (art. 1 alin. (6) Legea 554/2004) — parțial neacoperită, cu o limitare esențială.** Regula este revocabilitatea, dar un HCL care **a intrat în circuitul civil și a produs efecte juridice** NU mai poate fi revocat de consiliu. Textul de lege (art. 1 alin. (6), citat verbatim): „Autoritatea publică emitentă a unui act administrativ unilateral nelegal poate să solicite instanţei anularea acestuia, în situaţia în care actul nu mai poate fi revocat întrucât a intrat în circuitul civil şi a produs efecte juridice. (...) Acţiunea poate fi introdusă în termen de un an de la data emiterii actului." 

> **Atenție — limitare jurisprudențială pentru HCL NORMATIVE:** prin **Decizia ÎCCJ (Completul pentru dezlegarea unor chestiuni de drept) nr. 74/2023, publicată în M. Of. nr. 12 din 8 ianuarie 2024**, s-a stabilit că autoritatea publică emitentă a unui act administrativ unilateral **cu caracter normativ NU poate solicita instanței anularea acestuia** în temeiul art. 1 alin. (6). Prin urmare, ipoteza „anulare de instanță la cererea propriei autorități emitente" este disponibilă **numai pentru HCL individuale**, nu și pentru cele normative (care rămân oricând revocabile de consiliu). Aceasta are impact direct asupra sub-câmpului `reclamant` propus mai jos.

**Alte cauze** (împlinirea obiectului/scopului, imposibilitatea executării, desuetudinea, dispariția obiectului, efectul unei legi noi care abrogă implicit) — toate subsumabile fie caducității, fie abrogării implicite, fie acoperite de valoarea-rezervă `Altul`.

### (c) Formularea „Anulat de prefect" este GREȘITĂ — corectare obligatorie

**Temeiuri de lege citate:**
- **Art. 123 alin. (5) din Constituția României** (verbatim, citat și de CCR în Decizia nr. 643/07.11.2024 și Decizia nr. 44/18.02.2025): „Prefectul poate ataca, în faţa instanţei de contencios administrativ, un act al consiliului judeţean, al celui local sau al primarului, în cazul în care consideră actul ilegal. Actul atacat este suspendat de drept."
- **Art. 255 Cod administrativ (OUG 57/2019)** — titlul oficial „Atribuții privind verificarea legalității", **având DOAR două alineate** (verbatim, confirmat din legislatie.just.ro / Lege5): „(1) Prefectul verifică legalitatea actelor administrative ale consiliului judeţean, ale consiliului local şi ale primarului. (2) Prefectul poate ataca actele autorităţilor prevăzute la alin. (1) pe care le consideră ilegale, în faţa instanţei competente, în condiţiile legii contenciosului administrativ." **Codul nu folosește nicăieri formularea că prefectul „anulează"; exclusiv „verifică legalitatea" / „poate ataca".** (Concordant: art. 200 — actele locale „supuse controlului de legalitate exercitat de către prefect conform prevederilor art. 255"; art. 249 alin. (4) — prefectul „asigură verificarea legalității (...) și poate ataca în fața instanței".)
- **Art. 3 Legea 554/2004**: prefectul poate ataca direct actele autorităților locale; „Până la soluţionarea cauzei, actul atacat (...) este suspendat de drept."
- **Natura de tutelă administrativă** este confirmată de ÎCCJ — Decizia (DCD) nr. 11/2015 (M. Of. nr. 501 din 8 iulie 2015).

**Concluzie:** prefectul exercită o tutelă administrativă „moderată" — declanșează controlul, dar **anularea aparține exclusiv instanței**. Trebuie distinse două stări diferite:
- **Actul atacat de prefect și suspendat de drept** — efect **temporar**, NU invalidare definitivă → de modelat ca relație/stare (similar suspendării), nu ca `MotivInvalidare`.
- **Actul efectiv anulat de instanță** (eventual la sesizarea prefectului) — invalidare definitivă → intră în enum.

Denumirea corectă: valoarea „Anulat de prefect" trebuie **înlocuită cu „Anulat de instanță"**, opțional cu un sub-câmp privind cine a sesizat instanța (prefect / terț vătămat / autoritate emitentă, cu limitările de mai jos).

**Confirmare procedurală:** Codul administrativ **NU mai prevede** procedura de reanalizare/revocare prealabilă pe care prefectul o solicita emitentului (fostul **art. 26 alin. (2) din Legea nr. 340/2004**, abrogată prin OUG nr. 57/2019, M. Of. nr. 555 din 5 iulie 2019). Astăzi prefectul atacă direct.

---

## Details

### Delimitarea „eveniment legislativ" (relație) vs. „invalidare/încetare" (enum)
**Legea 24/2000, art. 58 alin. (1)** (text citat): „După intrarea în vigoare a unui act normativ, pe durata existenţei acestuia pot interveni diferite evenimente legislative, cum sunt: modificarea, completarea, abrogarea, republicarea, suspendarea sau altele asemenea." Acestea sunt deja modelate ca **relații** între acte în Cleriq — corect. Enum-ul `MotivInvalidare` trebuie să acopere doar cauzele de **încetare definitivă / invalidare** a unui HCL determinat.

**Atenție conceptuală — redundanță:** „Abrogat prin HCL ulterior" este simultan (i) un eveniment legislativ deja modelat ca relație și (ii) o cauză de încetare a efectelor. Pentru a evita dubla înregistrare, recomand ca acest motiv să fie **derivat automat** din existența relației de abrogare, nu introdus manual ca `MotivInvalidare` independent.

### Lista finală propusă pentru enum-ul `MotivInvalidare`

Recomand **reformularea completă** (opțiunea iii din întrebare), cu acoperire exhaustivă a cauzelor de încetare definitivă:

| Valoare enum | Denumire UI | Temei juridic | Tip temei |
|---|---|---|---|
| `AnulatDeInstanta` | „Anulat de instanța de contencios administrativ" | Legea 554/2004 art. 1, 8, 18; Constituție art. 123 alin. (5) | **Text de lege** |
| `RetractatDeEmitent` | „Revocat/retractat de consiliul emitent" | Legea 554/2004 art. 7 alin. (1); principiul revocabilității | Text de lege + doctrină |
| `AbrogatPrinHCLUlterior` | „Abrogat prin HCL ulterior" *(derivat din relație)* | Legea 24/2000 art. 64 | **Text de lege** |
| `Caduc` | „Caducitate (expirarea termenului / dispariția obiectului)" | Podaru, 2019 | Doctrină |
| `Inexistent` | „Inexistent (nepublicat în MOL / lipsă element esențial)" | Cod adm. art. 198; CCR Dec. 513/2019 | Text de lege + jurisprudență |
| `Altul` | „Altul (motiv text liber)" — câmp `motivAltulText` obligatoriu | — (cazuri neprevăzute) | Interpretare proprie |

**Detalierea valorilor:**

1. **`AnulatDeInstanta`** — acoperă anularea indiferent de cine a sesizat instanța. Recomand un sub-câmp opțional `reclamant` (Prefect / Terț vătămat / Autoritate emitentă / Altul), **dar cu două limitări juridice care trebuie codate în reguli de validare:**
   - autoanularea de către emitent (`reclamant = Autoritate emitentă`, în temeiul art. 1 alin. (6) Legea 554/2004) este admisibilă **numai pentru HCL individuale**, NU pentru HCL normative — *Decizia ÎCCJ nr. 74/2023 (M. Of. nr. 12/2024)*;
   - UAT prin primar **nu** poate ataca propriile HCL — prin RIL/HP publicat în M. Of. nr. 773 din 16 octombrie 2015 s-a stabilit că „unitatea administrativ-teritorială, prin autoritatea sa executivă, respectiv primarul, nu are dreptul de a ataca în faţa instanţei de contencios administrativ hotărârile adoptate de (...) consiliul local".
2. **`RetractatDeEmitent`** — revocare proprie a consiliului, înainte de intrarea actului în circuitul civil; corespunde valorii actuale „Retractat" (care poate fi păstrată ca denumire).
3. **`AbrogatPrinHCLUlterior`** — păstrat, dar marcat ca **derivabil** din relația de abrogare deja modelată, pentru a evita redundanța.
4. **`Caduc`** — pentru HCL temporare (buget anual, impozite/taxe pe anul fiscal) și acte al căror obiect a dispărut.
5. **`Inexistent`** — pentru HCL normative neaduse la cunoștință publică / nepublicate în MOL, sau lovite de lipsa unui element esențial.
6. **`Altul`** — cu câmp text liber **obligatoriu**, pentru cazuri neprevăzute (imposibilitatea executării, desuetudine, efectul unei legi noi de abrogare implicită etc.). Acoperă golul actual (enum-ul nu are azi „Altul", deși alte enum-uri din aplicație îl au).

**Notă de modelare:** starea „atacat de prefect și suspendat de drept" **NU** intră în acest enum — este o suspendare temporară, deci de modelat ca relație/stare separată, întrucât nu reprezintă o ieșire **definitivă** din vigoare.

---

## Recommendations

**Etapa 1 — imediat (corectitudine juridică, fără ambiguitate):**
- Redenumiți „Anulat de prefect" → **„Anulat de instanță"**. Este o eroare juridică certă; prefectul nu anulează niciodată direct. Corectați indiferent de restul deciziilor.
- Tratați „atacarea de către prefect + suspendarea de drept" ca **stare/relație temporară**, nu ca motiv de invalidare definitivă.

**Etapa 2 — termen scurt (completitudine):**
- Adăugați `Caduc`, `Inexistent` și `Altul` (+ câmp text liber). Acestea acoperă cazurile cu cea mai mare probabilitate practică (HCL temporare pe buget/taxe; HCL nepublicate în MOL).
- Marcați `AbrogatPrinHCLUlterior` ca derivat din relația de abrogare, pentru a evita dubla înregistrare.

**Etapa 3 — opțional (granularitate), doar dacă echipa juridică o cere:**
- Adăugați sub-câmpul `reclamant` la `AnulatDeInstanta`, cu regulile de validare din tabel (blocați `reclamant = Autoritate emitentă` pentru HCL normative — ÎCCJ 74/2023; blocați primarul ca reclamant împotriva propriilor HCL — HP 2015).

**Prag/benchmark care schimbă recomandarea:** dacă din date reiese că peste ~10–15% dintre HCL gestionate sunt acte temporare (taxe/impozite/buget), `Caduc` devine *must-have*, nu *nice-to-have*. Dacă aplicația nu va înregistra niciodată cine a sesizat instanța, renunțați la sub-câmpul `reclamant` și păstrați o singură valoare `AnulatDeInstanta`.

**De confirmat obligatoriu cu un avocat (zone de incertitudine juridică):**
- (a) dacă `Inexistent` trebuie tratat ca *invalidare* sau ca o *stare anterioară* intrării în vigoare (actul inexistent, teoretic, nu a produs niciodată efecte) — are implicații asupra modului în care îl marcați în fluxul Draft→…→Publicat;
- (b) gestionarea exactă a redundanței „Abrogat prin HCL ulterior" (relație vs. motiv);
- (c) regimul procedural concret al inexistenței pentru nepublicare în MOL la nivel local (jurisprudența privește mai ales actele centrale nepublicate în Monitorul Oficial al României).

---

## Caveats
- **Sursa fiecărei afirmații:** citatele de articole (Constituție art. 123(5); Cod administrativ art. 198, art. 255; Legea 24/2000 art. 58, 64; Legea 554/2004 art. 1, 3, 7) sunt **text de lege** preluat din surse oficiale (legislatie.just.ro, Constituția României, Lege5). Deciziile CCR 513/2019, 643/2024, 44/2025 și deciziile ÎCCJ (DCD) 11/2015, 74/2023 și HP din 2015 sunt **jurisprudență**. Structura propusă a enum-ului, încadrarea cazurilor și recomandările de design sunt **interpretare proprie**.
- Distincția nulitate absolută/relativă are relevanță practică redusă în contenciosul administrativ român (Legea 554/2004 nu o reflectă) — de aceea propun o singură valoare de anulare de instanță, nu valori separate pe tipuri de nulitate.
- **Caducitatea NU este reglementată expres la nivel normativ** în dreptul român; este o construcție doctrinară (Podaru). Este implementabilă în software și utilă practic, dar fără temei legal text-expres — de semnalat ca atare în documentația produsului.
- Limitarea din **Decizia ÎCCJ nr. 74/2023** (emitentul nu își poate cere anularea propriului act normativ) este recentă și trebuie reflectată în regulile de validare dacă adăugați sub-câmpul `reclamant`; recomand confirmarea de către un avocat a aplicării ei la HCL.
- Inexistența pentru nepublicare este susținută de doctrină și de jurisprudență, dar regimul ei procedural rămâne o zonă de incertitudine — necesită confirmarea unui avocat.