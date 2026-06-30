# Analiză juridică: Dispozițiile primarului (Modul C / Faza 4) — recomandare de design backend pentru Cleriq

> Cadru de bază: OUG 57/2019 (Codul administrativ), Legea 24/2000, Legea 52/2003, Legea 554/2004, Legea 190/2018 + RGPD, Constituția.
> Document-pereche cu `research_hcl.md` și `research_MotivInvalidare.md` — reutilizez concluziile transferabile și marchez EXPLICIT unde dispoziția primarului diferă de HCL.
>
> **Actualizare (v2):** adăugată secțiunea „Validare empirică — cum implementează eMOL" (reference implementation, 554+ primării) și **corectată o recomandare** care era prea rigidă — gardă dură (409) la publicarea individualelor → înlocuită cu „nepublic implicit + override deliberat (confirmare + motiv + audit)", conform implementării reale din eMOL. Restul concluziilor juridice rămân validate de practica eMOL.
>
> **Actualizare (v3):** rezolvate punctele de interpretare rămase — adăugat **art. 528 Cod administrativ** (regimul de intrare în vigoare al actelor de personal, lege separată pe care Cleriq NU o codează) și **principiul transversal „defer-to-secretar"** (deciziile juridic-incerte le ia secretarul, responsabilul legal — aplicația oferă structură + audit). Recomandarea de „validare cu avocat" ca precondiție a fost scoasă; verificarea juridică finală rămâne planificată de echipă la încheierea aplicației, ca pas de asigurare.

## TL;DR
- **Dispoziția primarului este un act administrativ unilateral emis de primar**, nu adoptat printr-un vot de consiliu (art. 196 alin. (1) lit. b) Cod adm.). Consecințe de model: **fără** `PunctOrdineZiId`, **fără** snapshot de vot, **fără** `TipMajoritate`. Originea și fluxul diferă; ciclul de viață (Draft → Numerotat → Semnat → Publicat/Comunicat) se transferă identic de la HCL.
- **Semnatari: primar (emitent) + secretar general (contrasemnătură pentru legalitate)** — art. 240 alin. (1) + art. 243 alin. (1) lit. a). NU președinte de ședință + secretar; cazul art. 140 (refuzul președintelui de ședință) **nu există** la dispoziție. Contrasemnătura secretarului = **aviz de legalitate ex-ante**, nu condiție de validitate prin ea însăși, dar **angajează răspunderea** semnatarului (art. 240 alin. (2)-(3)).
- **Asimetria #1 față de HCL (cea mai importantă pentru backend): în MOL se publică DOAR dispozițiile cu caracter NORMATIV** (Anexa 1, art. 1 alin. (2) lit. d). Dispozițiile **INDIVIDUALE (acte de personal) NU se publică** integral — se înscriu doar în Registrul pentru evidența dispozițiilor. La HCL, în schimb, se publică **ambele** tipuri (Anexa 1, art. 1 alin. (2) lit. c). Deci regula „Individual → nu se publică" e **text de lege**, nu doar bună practică.
- **Comunicare la prefect: AMBELE tipuri** de dispoziții (normativ + individual) se comunică prefectului, în **cel mult 10 zile lucrătoare** de la emitere (art. 197 alin. (1)). Controlul de legalitate al prefectului se aplică **tuturor** dispozițiilor (art. 200 + 255).
- **Intrarea în vigoare:** dispoziția **normativă** produce efecte de la **aducerea la cunoștință publică** (art. 198 alin. (1)), care se face în 5 zile de la comunicarea la prefect (art. 198 alin. (2)); dispoziția **individuală** produce efecte de la **comunicarea către destinatar** (art. 199). Pragul de **intangibilitate** (înghețarea variantei semnate) se transferă identic de la HCL: cel mai timpuriu moment verificabil (publicare normativ / comunicare individual).
- **Numerotare:** un **singur registru** de dispoziții per UAT per an, separat de registrul HCL, cu **ambele tipuri în aceeași secvență cronologică** (Anexa 1 — „Registrul pentru evidența dispozițiilor autorității executive"; art. 243 alin. (1) lit. d). Confirmat empiric: convocarea (nr. 1586) și rectificarea de buget (nr. 361) sunt în aceeași serie. Detectarea de lacune = feature, nu mandat legal expres.
- **Invalidare:** lista de moduri se transferă aproape integral de la `MotivInvalidare` (HCL): `AnulatDeInstanta` (NU „de prefect"), `Caduc`, `Inexistent`, `Altul`, `AbrogatPrin...` (derivat din relație). **Corectare la framing-ul din plan_faza4 #5:** distincția reală e **normativ/individual**, NU „primar vs. consiliu". Primarul își revocă dispoziția **normativă** oricând (prin act nou), la fel cum consiliul abrogă un HCL normativ; dispoziția **individuală intrată în circuit și care a produs efecte NU mai poate fi revocată de primar** — doar anulată de instanță (art. 1 alin. (6) Legea 554/2004). Singura schimbare obligatorie de etichetă: `Retractat` „de consiliul emitent" → **„Revocat de primar (emitent)"**.
- **Dispoziția de convocare** a consiliului = dispoziție a primarului (art. 134 alin. (1) lit. a), pentru ședințele ordinare și extraordinare convocate de primar (art. 133 alin. (1), alin. (2) lit. a) și c). Este un act **individual / de organizare**, numerotat în registrul general de dispoziții. **Excepție:** convocarea cerută de 1/3 din consilieri (art. 133 alin. (2) lit. b) **NU** e dispoziție — e document semnat de consilieri (art. 134 alin. (1) lit. b).
- **GDPR / Legea 190/2018:** actele de personal conțin CNP și alte numere de identificare națională → prelucrare condiționată (art. 4 Legea 190/2018: măsuri tehnice, DPO, termene de stocare, minimizare). Nepublicarea integrală a individualelor în MOL **este** mecanismul structural de protecție. Chiar și ce se publică (normativ) trebuie să respecte protecția datelor (Anexa 1, art. 5 alin. (2)).
- **Validare empirică (eMOL, 554+ primării):** analiza juridică de mai sus e confirmată cvasi-integral de implementarea eMOL Expert (semnatari, normativ/individual, comunicare prefect, numerotare unică, convocare, audit + trasabilitate funcții). **O singură corecție de recomandare:** publicarea individualelor NU se blochează dur (409), ci e **nepublic implicit + override deliberat** (confirmare + motiv + audit), pentru cazuri-limită legitime (act anonimizat, fără date personale, numire de comisie publică). **Un loc unde Cleriq poate fi peste eMOL:** eMOL nu are obiect de **erată dedicat** (corectează prin acte modificatoare generice) — erata unificată din Faza 7 e diferențiator și mai corectă juridic.

---

## Transfer rapid din `research_hcl.md` (ce se aplică IDENTIC — nu re-derivăm)

| Concluzie HCL | Se transferă la Dispoziție? | Observație |
|---|---|---|
| Intangibilitate după intrarea în circuit; varianta semnată nu se suprascrie | **DA, identic** | Pragul = publicare (normativ) / comunicare (individual) |
| Prefectul **atacă**, NU anulează; instanța anulează; act atacat = suspendat de drept | **DA, identic** | Art. 200, 255 Cod adm.; art. 123 alin. (5) Constituție; art. 3 Legea 554/2004 |
| Erata = act nou, numerotare proprie, legat de original (Legea 24/2000 art. 71) | **DA, identic** | Faza 7, pattern unificat PV/HCL/Dispoziție. **eMOL NU are erată dedicată** → diferențiator Cleriq (vezi „Validare empirică") |
| „Anulează MOL" doar înainte de producerea efectelor, rol Admin, motiv obligatoriu, audit imutabil | **DA, identic** | Vezi matricea de stări mai jos |
| Eliminarea lanțului „Anulează MOL → modifică PDF semnat → republică" pentru acte intrate în circuit | **DA, identic** | Aceleași gărzi ca HCL |
| Versionare append-only + hash criptografic per PDF publicat | **DA, identic** | Dovadă tehnică a intangibilității |
| Enum invalidare: `AnulatDeInstanta` (nu „de prefect"), `Caduc`, `Inexistent`, `Altul`, `AbrogatPrin...` derivat | **DA, cu o etichetă schimbată** | `Retractat` → „Revocat de primar" |

**Ce NU se transferă (divergențe reale):** semnatari (primar+secretar vs. președinte+secretar); originea (emitere unilaterală vs. vot); **publicarea în MOL doar pentru normativ** (vs. ambele la HCL); registru de numerotare separat; tipul nou „dispoziție de convocare"; absența cazului art. 140.

---

## Validare empirică — cum implementează eMOL (reference implementation, 554+ primării)

*[Adăugat în v2]* eMOL Expert (Big Media/Faxmedia) digitalizează exact acest flux la 554+ primării. Partea de aplicație e în spatele login-ului, dar **regulamentele de dispoziții** ale primăriilor (procesul digitalizat) și **registrele publice** (output-ul) confirmă cvasi-integral analiza juridică de mai sus și au impus o singură corecție de recomandare. Maparea pe cele 8 puncte:

- **1. Semnare — CONFIRMAT.** Regulamentele (Deta art. 7, 21) listează semnătura Primarului + contrasemnătura Secretarului General, cu formula „PRIMARUL ORAȘULUI … / Contrasemnează pentru legalitate: SECRETARUL GENERAL". Primar absent → viceprimar semnează „p. PRIMAR, [nume], Viceprimar" (Deta art. 23 alin. 2); secretar absent → contrasemnează consilierul juridic (art. 23 alin. 3). Proiectele cu avizare **refuzată** de secretar se trimit oricum primarului pentru semnare (Deta art. 20) — exact „primarul poate emite pe propria răspundere peste obiecția secretarului". → Modelează rolul contrasemnatar = viceprimar + prefix „p." în PDF, plus starea „avizare refuzată".
- **2. Normativ/individual — CONFIRMAT** (cu o corecție de implementare, mai jos). Definiția lor (Deta art. 2) e identică criteriului din research. Caracterul se stabilește în **referatul de fundamentare, la inițiere** (art. 4) → `TipDispozitie` e input la `Creeaza`, nu derivat la final.
- **3. Comunicare la prefect — CONFIRMAT.** Toate dispozițiile, 10 zile lucrătoare; obiecțiile de legalitate într-un registru special (Conop art. 18); eMOL Expert are modul de evidență a comunicării cu prefectul.
- **5. Numerotare — CONFIRMAT.** „Numerotarea dispozițiilor se face separat pe fiecare an calendaristic, începând cu numărul 1", într-un singur registru (Deta art. 22). Proiectele au numerotare separată de actele emise → exact `Numar?` nullable până la `AtribuieNumar`.
- **6. Invalidare/revocare — eMOL modelează stările căutate.** Regulamentele (Scânteiești, Ferești art. 13) definesc revocarea ca „retractarea actului administrativ valid … prin care încetează efectele". Pentru starea „atacat": la introducerea unei acțiuni în contencios, secretarul comunică de îndată, iar inițiatorul **reanalizează actul în 5 zile** cu propuneri de menținere/modificare/revocare/încetare (Conop art. 20). → Confirmă „atacat = stare temporară" și sugerează un **pas explicit de reanaliză** în state-machine.
- **7. Dispoziția de convocare — CONFIRMAT.** Primarul „va emite dispoziția de convocare" pornind de la proiectul ordinii de zi primit de la secretar (Deta, regulament HCL); ordinea de zi = anexă; numerotată în registrul general; unele UAT au subsecțiune dedicată „Dispoziții de convocare" în registrul de dispoziții; termene 5/3 zile de la comunicare.
- **8 + Audit — CONFIRMAT.** Pagina registrului are „Istoric evenimente - dispoziție" (jurnal per act) + „Autor Înregistrare" = `IstoricActiuneAct`. „Istoric evenimente - persoană" cu interval de valabilitate + documente atașate = **trasabilitatea funcțiilor** (Faza 2). Ștergere cu confirmare = gardă pe delete.

### CORECȚIE de recomandare — publicarea individualelor: NU blocaj dur, ci nepublic implicit + override deliberat

În v1 recomandasem o gardă dură (409) care interzice complet publicarea dispozițiilor individuale. **eMOL arată că abordarea corectă e mai nuanțată.** În pagina registrului există un modal de confirmare la publicarea unei individuale: aplicația avertizează că ești pe cale să faci public conținutul unei dispoziții cu caracter individual și cere confirmare („Da/Nu"). Deci:
- **Default: individualele sunt NEpublice.** Norma din Anexa 1 lit. d rămâne **text de lege** — în secțiunea „DISPOZIȚIILE AUTORITĂȚII EXECUTIVE" se publică doar normativul.
- **DAR publicarea nu e imposibilă printr-un 409 rigid** — e un **override deliberat**: avertisment + confirmare explicită + motiv + audit, disponibil rolului responsabil (secretar/Admin). Motiv: există **cazuri-limită legitime** (act individual fără date personale, act anonimizat, numirea unei comisii publice) unde secretarul, care e responsabilul legal, decide publicarea.
- Garanția de protecție a datelor se păstrează prin **default-ul nepublic + obligația de anonimizare** înainte de orice publicare a unui act cu date personale, NU prin imposibilitate tehnică.

→ În matrice și în reguli, „INTERZIS (409)" devine **„nepublic implicit; publicare = override deliberat (confirmare + motiv + audit)"**.

### Unde Cleriq poate fi PESTE eMOL — erata dedicată

eMOL **nu are un flux/obiect de erată dedicat.** Corecțiile se fac prin mecanica de eveniment legislativ (Legea 24/2000) — modificare/completare/abrogare — adică prin **acte noi**, cu legătura către original prin mențiune în titlu (titlul + nr. + data actului vizat) (Deta art. 8, 15-17). Nu există un obiect „erată" trasabil separat. → **Erata unificată (Faza 7)** ca obiect distinct (al doilea act, legat de primul, cu propriul flux și numerotare, afișat lângă originalul intact) e un **diferențiator real** și **mai corect juridic**: erata (eroare materială, Legea 24/2000 art. 71) și modificarea de fond sunt lucruri legal distincte — contopirea lor (ca la eMOL) poate masca exact greșeala din cazul Castelu (corecție de fond deghizată în „îndreptare").

---

## Key Findings

### 1. SEMNARE: cine emite, cine contrasemnează, ce efect, cazul refuzului, cazul primar absent

**Emiterea — primarul, unilateral.** *[TEXT DE LEGE]* Art. 196 alin. (1) Cod adm.: „În exercitarea atribuţiilor ce le revin, autorităţile administraţiei publice locale adoptă sau emit, după caz, acte administrative cu caracter normativ sau individual, după cum urmează: a) consiliul local şi consiliul judeţean **adoptă hotărâri**; b) **primarul** şi preşedintele consiliului judeţean **emit dispoziţii**." Temeiul material al competenței primarului de a emite dispoziții: art. 155 (atribuțiile primarului) + art. 196 alin. (1) lit. b).

**Învestirea cu formulă de autoritate — semnătura primarului.** *[TEXT DE LEGE]* Art. 240 alin. (1): „Primarul, preşedintele consiliului judeţean, respectiv preşedintele de şedinţă al consiliului local, după caz, **prin semnare, învesteşte cu formulă de autoritate** executarea actelor administrative emise sau adoptate în exercitarea atribuţiilor care îi revin potrivit legii." Pentru dispoziție, semnatarul-emitent este **primarul** (nu președintele de ședință, care semnează HCL).

**Contrasemnătura pentru legalitate — secretarul general.** *[TEXT DE LEGE]* Art. 243 alin. (1) lit. a): secretarul general „**avizează proiectele de hotărâri şi contrasemnează pentru legalitate dispoziţiile primarului**, respectiv ale preşedintelui consiliului judeţean, hotărârile consiliului local, respectiv ale consiliului judeţean, după caz". Reține distincția legiuitorului: secretarul **avizează** proiectele de hotărâri (ex-ante, înainte de vot) și **contrasemnează** dispozițiile primarului (după emitere, pe act).

**Efectul juridic al contrasemnăturii.** *[TEXT DE LEGE]* Art. 240 alin. (2) teza a II-a: „contrasemnarea sau avizarea pentru legalitate şi semnarea documentelor de fundamentare **angajează răspunderea administrativă, civilă sau penală, după caz, a semnatarilor**, în cazul încălcării legii, în raport cu atribuţiile specifice." Art. 240 alin. (3): funcționarii care fundamentează/contrasemnează/avizează cu încălcarea legii răspund. *[INTERPRETARE PROPRIE, fundamentată pe doctrină]* Contrasemnătura secretarului are valoarea unui **aviz de legalitate consultativ ex-ante**: nu „validează" actul (aprecierea oportunității și necesității aparține **exclusiv** primarului — art. 240 alin. (2) teza I), dar lipsa ei sau o contrasemnătură dată cu încălcarea legii angajează răspunderea. Pentru design: contrasemnătura este o **gardă de completitudine la semnare** (nu se poate marca „Semnat" fără ambele semnături), dar absența ei nu transformă automat actul în nul — generează însă răspundere și înregistrare în registrul refuzurilor.

**Refuzul secretarului de a contrasemna pentru nelegalitate.** *[TEXT DE LEGE]* Art. 197 alin. (3): „Comunicarea, însoţită de **eventualele obiecţii motivate cu privire la legalitate**, se face în scris de către secretarul general al unităţii/subdiviziunii administrativ-teritoriale şi se înregistrează într-un registru special destinat acestui scop." *[TEXT DE LEGE]* Anexa 1, secțiunea „ALTE DOCUMENTE", lit. a): se publică „**Registrul privind înregistrarea refuzurilor de a semna/contrasemna/aviza actele administrative**, precum [și] obiecţiile cu privire la legalitate, efectuate în scris". *[INTERPRETARE PROPRIE]* Mecanismul legal nu „blochează" emiterea (primarul poate emite dispoziția pe propria răspundere chiar peste obiecția secretarului), dar refuzul/obiecția se **consemnează în scris și devine publică**. Pentru backend: trebuie modelată **starea „contrasemnătură refuzată cu obiecție de legalitate"** ca alternativă la contrasemnare, cu text de motivare, autor, dată — distinctă de simpla absență, și expusă într-un registru al refuzurilor.

**Cazul primar absent / împiedicat — înlocuitorul de drept.** *[TEXT DE LEGE]* Art. 163 alin. (1): „În caz de **vacanţă** a funcţiei de primar, în caz de **suspendare** din funcţie a acestuia, precum şi în situaţiile de **imposibilitate de exercitare a mandatului**, atribuţiile ce îi sunt conferite prin prezentul cod sunt exercitate **de drept de viceprimar** sau, după caz, de unul dintre viceprimari, desemnat de consiliul local în condiţiile art. 152 alin. (4)". Art. 152 alin. (1): „Viceprimarul este subordonat primarului şi, în situaţiile prevăzute de lege, **înlocuitorul de drept** al acestuia, situaţie în care exercită, **în numele primarului**, atribuţiile ce îi revin acestuia."

*[ZONĂ DE ATENȚIE — distincție subtilă, dar importantă pentru derivarea emitentului]* Delegarea ordinară prin art. 157 alin. (1) („Primarul poate delega, prin dispoziţie, atribuţiile ce îi sunt conferite de lege … viceprimarului, secretarului general …") **NU** acoperă, în interpretarea doctrinară dominantă, **emiterea de dispoziții**: actul nu ar putea fi învestit cu formulă de autoritate printr-o altă semnătură decât a autorității executive (primarul), iar aprecierea oportunității/necesității revine exclusiv primarului. Concret:
- **Concediu / absență fizică temporară** (primarul își păstrează calitatea și capacitatea de a semna, inclusiv electronic) → **primarul rămâne emitentul**. Nu se transferă puterea de a emite dispoziții doar pentru că nu e la sediu.
- **Vacanță / suspendare / imposibilitate reală** (art. 163) → **viceprimarul (înlocuitor de drept) emite în nume propriu ca exercitând atribuțiile primarului**.

Implicație directă pentru `IServiciuFunctiiIstorice`: pe lângă `CinESecretarulUatLa(data)`, e nevoie de `CinEPrimarLa(data)` **și** de o noțiune de „cine exercită atribuțiile de primar la data X" (primar titular vs. înlocuitor de drept), pentru a deriva corect emitentul + contrasemnatarul la momentul emiterii.

---

### 2. NORMATIV vs. INDIVIDUAL: criteriul, ce se publică, ce NU

**Criteriul de distincție.** *[INTERPRETARE PROPRIE, fundamentată pe doctrină + structura art. 198-199]* Dispoziția **normativă** conține reguli generale și impersonale, de aplicabilitate repetată (ex.: stabilirea unor proceduri interne, măsuri de organizare cu efecte erga omnes la nivel local). Dispoziția **individuală** se adresează unor persoane determinate, pentru o situație concretă (cvasi-totalitatea actelor de personal: numire, încadrare, modificare/suspendare/încetare raport de serviciu sau de muncă, sancțiuni, concedii, delegări, detașări; dar și alte acte individuale: stabilirea unor drepturi, autorizări punctuale etc.). Codul confirmă indirect distincția prin regimuri diferite de intrare în vigoare (art. 198 pentru normativ, art. 199 pentru individual).

**Ce se publică în MOL — DOAR normativul. [TEXT DE LEGE — diferența-cheie față de HCL]** Anexa 1 la Codul administrativ, art. 1 alin. (2):
- lit. c) „**HOTĂRÂRILE AUTORITĂŢII DELIBERATIVE**", unde se publică actele adoptate de consiliu „**atât cele cu caracter normativ, cât şi cele cu caracter individual**";
- lit. d) „**DISPOZIŢIILE AUTORITĂŢII EXECUTIVE**", unde se publică actele administrative emise de primar „**cu caracter normativ**; aici se publică Registrul pentru evidenţa proiectelor de dispoziţii ale autorităţii executive, precum şi Registrul pentru evidenţa dispoziţiilor autorităţii executive".

Așadar, la dispoziții, **doar cele normative se publică integral** în MOL; **individualele NU se publică** integral — se reflectă numai în **Registrul pentru evidența dispozițiilor** (intrare cronologică: număr, dată, titlu/obiect generic, emitent). Aceasta este **text de lege**, nu doar bună practică de protecție a datelor — deși scopul evident este protejarea datelor cu caracter personal din actele de personal.

**Format de publicare a ce SE publică.** *[TEXT DE LEGE]* Anexa 1, art. 5 alin. (2) (cf. art. 197 alin. (5) Cod adm.): dispozițiile normative „se publică, pentru informare, în format electronic şi în monitorul oficial local, în format «pdf» editabil, pentru a se păstra macheta şi aspectul documentului intacte … fără să cuprindă semnăturile olografe ale persoanelor, **asigurându-se respectarea reglementărilor în materia protecţiei datelor personale**". Deci chiar și pentru normativ: PDF fără semnături olografe + conformitate cu protecția datelor.

**Implicații pentru ce se expune public (backend + Faza 9):**
- `EstePublicat` + `DataPublicareMol` au sens **doar pentru `TipDispozitie.Normativ`**. La `Individual`, publicarea în MOL/portal este **nepublică implicit**; nu un 409 dur, ci un **override deliberat** (avertisment + confirmare + motiv + audit), conform implementării eMOL — vezi secțiunea „Validare empirică". Pentru actele cu date personale: anonimizare obligatorie înainte de orice publicare.
- Registrul de dispoziții (Faza 5) listează **ambele** tipuri (e o evidență cronologică), dar pentru individuale expune doar metadate (nr., dată, obiect), nu PDF-ul integral.
- Pe portalul public (Faza 9), rutele publice servesc **doar dispozițiile normative**. Individualele nu ajung pe portal.

---

### 3. COMUNICARE LA PREFECT: ambele tipuri, termen 10 zile

**Ambele tipuri se comunică.** *[TEXT DE LEGE]* Art. 197 alin. (1): „Secretarul general al unităţii/subdiviziunii administrativ-teritoriale **comunică actele administrative prevăzute la art. 196** alin. (1) … prefectului în cel mult **10 zile lucrătoare** de la data adoptării, respectiv emiterii." Cum art. 196 alin. (1) acoperă atât hotărârile, cât și **dispozițiile**, iar controlul de legalitate vizează toate actele locale, **se comunică prefectului ATÂT dispozițiile normative, CÂT și cele individuale**.

**Controlul de legalitate — pe toate.** *[TEXT DE LEGE]* Art. 200: „**Dispoziţiile primarului**, hotărârile consiliului local şi hotărârile consiliului judeţean sunt **supuse controlului de legalitate** exercitat de către prefect conform prevederilor art. 255." Art. 255 alin. (1)-(2): prefectul „**verifică legalitatea**" și „**poate ataca** actele … pe care le consideră ilegale, în faţa instanţei competente" — **nu** anulează (vezi pct. 6 și transfer din `research_hcl`).

**Implicație pentru backend:** garda de comunicare la prefect (paritate cu `ComunicareHclPrefectController`) se aplică **ambelor** tipuri de dispoziții, cu același termen de 10 zile lucrătoare, marcare manuală (dată + canal + nr. înregistrare la prefect), alertă T-3, audit. Nu se diferențiază pe tip la comunicare (spre deosebire de publicare, care e doar pentru normativ).

---

### 4. INTRAREA ÎN VIGOARE / EFECTE și momentul INTANGIBILITĂȚII

**Dispoziția normativă — efecte de la aducerea la cunoștință publică.** *[TEXT DE LEGE]* Art. 198 alin. (1): „Hotărârile şi **dispoziţiile cu caracter normativ devin obligatorii de la data aducerii lor la cunoştinţă publică**." Art. 198 alin. (2): „Aducerea la cunoştinţă publică a hotărârilor şi a dispoziţiilor cu caracter normativ se face în termen de **5 zile de la data comunicării oficiale către prefect**." (Paralel perfect cu HCL normativ.)

**Dispoziția individuală — efecte de la comunicarea către destinatar.** *[TEXT DE LEGE]* Art. 199 alin. (1): „Comunicarea hotărârilor şi **dispoziţiilor cu caracter individual către persoanele cărora li se adresează** se face în cel mult **5 zile de la data comunicării oficiale către prefect**." Principiul: actul individual produce efecte juridice **de la data comunicării către destinatar** (paralel cu art. 199 alin. (2) pentru HCL individual). *[TEXT DE LEGE + decizie de design]* **Excepție pentru actele de personal:** intrarea în vigoare a actelor privind nașterea/modificarea/suspendarea/sancționarea/încetarea raporturilor de serviciu ale funcționarilor publici e guvernată de **art. 528 Cod administrativ** (Partea a VI-a, Capitolul X), un regim separat și complex (art. 513-533), cu cale proprie de contestare (art. 527). **Cleriq NU codează acest regim** — tratează doar ciclul documentului (creat → semnat → comunicat → nepublicat); momentul substanțial de intrare în vigoare e determinarea secretarului/HR înscrisă pe act (principiul defer-to-secretar). Pentru design e suficient principiul: **efecte de la comunicarea către destinatar**, restul fiind în sarcina omului responsabil.

**Momentul INTANGIBILITĂȚII (înghețarea variantei semnate) — se transferă de la HCL.** *[INTERPRETARE PROPRIE, identică cu research_hcl]* Pragul tehnic de blocare = **cel mai timpuriu moment verificabil** la care actul „a ieșit" din sfera internă:
- **Normativ:** publicarea în MOL / aducerea la cunoștință publică (sau, mai devreme, comunicarea la prefect).
- **Individual:** comunicarea către destinatar (sau, mai devreme, comunicarea la prefect).

După acest prag: `AIntratInCircuit = true` → varianta semnată se **îngheață** (Înlocuiește/Șterge indisponibile), „Anulează MOL" **blocat**, orice corecție redirecționată către **Erată / act modificator** (Faza 7). Înainte de prag (semnat, necomunicat, nepublicat): corecții permise cu motiv obligatoriu în audit; „Anulează MOL" e fereastra legitimă (Admin), dar la dispoziție „Anulează MOL" are sens **doar pentru normativ** (individualul nici nu se publică).

---

### 5. NUMEROTARE: registru unic de dispoziții per UAT per an, separat de HCL

**Registru propriu, separat de HCL.** *[TEXT DE LEGE]* Anexa 1, art. 1 alin. (2) lit. d) instituie „**Registrul pentru evidenţa dispoziţiilor autorităţii executive**" (distinct de „Registrul pentru evidenţa hotărârilor autorităţii deliberative" de la lit. c). Art. 243 alin. (1) lit. d): secretarul „coordonează organizarea arhivei şi **evidenţa statistică a hotărârilor consiliului local şi a dispoziţiilor primarului**" — două evidențe distincte.

**Un singur registru pentru AMBELE tipuri de dispoziții.** *[INTERPRETARE PROPRIE + confirmare empirică]* Anexa 1 prevede **un singur** registru de dispoziții (nu unul pentru normative și altul pentru individuale). Confirmat de practică: în Monitorul Oficial al unei UAT, dispoziția de convocare (ex. **nr. 1586/13.12.2024**, PMB) și dispoziția de rectificare buget (ex. **nr. 361/13.07.2022**, com. Vulcana-Băi) apar în **aceeași serie numerică**, indiferent de obiect sau tip. Deci: **o singură secvență cronologică per instituție per an, ambele tipuri amestecate**.

**Cerințe legale de numerotare.** *[ZONĂ DE INCERTITUDINE — parțial interpretare/practică]* Codul nu impune textual „numerotare cronologică, fără lacune" printr-un articol dedicat; cerința derivă din (i) conceptul de **registru de evidență** (Anexa 1), (ii) normele de tehnică legislativă (Legea 24/2000), (iii) buna practică administrativă și exigențele controlului de legalitate. Detectarea de salturi (anti-gap) și anti-dublură = **feature de produs** (paritate cu HCL, Faza 5), util la controlul prefectului, **nu** mandat legal expres.

**Implicație pentru backend:** generalizarea `ServiciuNumerotare` peste `IActNumerotat` (decizia #2 din plan_faza4) este corectă; indexul unic filtrat `(InstitutieId, AnNumerotare, Numar)` se aplică pe `Dispozitii` ca **registru propriu**, cu aceeași logică anti-lacună + compare-and-swap ca la HCL. **Tipul (Normativ/Individual) NU segmentează secvența** — numerotarea e unică pe registrul de dispoziții.

---

### 6. INVALIDARE / ÎNCETAREA EFECTELOR: ce se aplică și unde diferă de HCL

**Modurile aplicabile** (transfer din `research_MotivInvalidare.md`, cu ajustări):

1. **Anulare de instanță** — *[TEXT DE LEGE]* Legea 554/2004 art. 1, 8, 18; Constituție art. 123 alin. (5). **Identic cu HCL: prefectul ATACĂ, instanța ANULEAZĂ.** Eticheta corectă: **„Anulat de instanța de contencios administrativ"**, NU „anulat de prefect".
2. **Revocare / retractare de către PRIMAR** — vezi mai jos (aici e singura divergență reală de framing).
3. **Abrogare** (doar normativ) — *[TEXT DE LEGE]* Legea 24/2000 art. 58, 64. Dispoziția normativă se scoate din vigoare printr-o dispoziție nouă de aceeași forță. Recomand **derivare automată** din relația de abrogare (ca la HCL), nu introducere manuală.
4. **Caducitate** — *[DOCTRINĂ — Podaru, 2019]* expirarea termenului / dispariția obiectului. Relevant practic la dispozițiile temporare (ex. delegări pe perioadă determinată, măsuri valabile un an fiscal).
5. **Inexistență** — *[TEXT DE LEGE + JURISPRUDENȚĂ]* Cod adm. art. 198; CCR Dec. 513/2019. Pentru dispoziția **normativă** neadusă la cunoștință publică. *[Atenție:]* la individuale, „nepublicarea" nu e aplicabilă (oricum nu se publică) — inexistența pentru individual ar viza lipsa unui element esențial / a comunicării.
6. **Altul** — câmp text liber obligatoriu (`MotivInvalidareAltulText`), pentru cazuri neprevăzute.

**Divergența reală — și o CORECTARE la plan_faza4 #5.** *[TEXT DE LEGE + INTERPRETARE]* Framing-ul din plan („primarul își poate revoca propria dispoziție, spre deosebire de consiliu la actele normative") este **imprecis**. Realitatea juridică:
- **Distincția care contează e normativ/individual, NU emitent unipersonal vs. colegial.**
- **Acte NORMATIVE** (și ale primarului, și ale consiliului): **oricând revocabile/abrogabile** prin act nou de aceeași forță. Primarul abrogă o dispoziție normativă printr-o dispoziție nouă, exact cum consiliul abrogă un HCL normativ. *Notă ÎCCJ 74/2023:* emitentul **nu poate cere instanței** anularea propriului act **normativ** (nici primarul, nici consiliul) — dar nici nu are nevoie, fiindcă îl poate abroga direct.
- **Acte INDIVIDUALE intrate în circuit și care au produs efecte**: **irevocabile pentru emitent** — și pentru primar, și pentru consiliu. *[TEXT DE LEGE]* Art. 1 alin. (6) Legea 554/2004: „Autoritatea publică emitentă a unui act administrativ unilateral nelegal **poate să solicite instanţei anularea** acestuia, în situaţia în care actul **nu mai poate fi revocat întrucât a intrat în circuitul civil şi a produs efecte juridice**. … Acţiunea poate fi introdusă în termen de un an de la data emiterii actului." Această ipoteză (autoanulare la cererea emitentului) **este disponibilă pentru dispoziția individuală** (act individual), spre deosebire de cea normativă.

**Singura schimbare obligatorie de model:** eticheta `Retractat` („de consiliul emitent") devine **„Revocat de primar (emitent)"** sau o formulare generică act-agnostică. Semantica: revocare proprie a primarului, **admisibilă înainte de intrarea în circuit** (individual) / **oricând** (normativ); după intrarea în circuit a unui individual → nu mai e revocare, ci anulare de instanță.

**Concluzie:** enum-ul `MotivInvalidare` se **reutilizează direct** (decizia #4 din plan_faza4 e corectă), cu (a) eticheta `Retractat` ajustată și (b) o **regulă de validare** care leagă admisibilitatea revocării proprii de `TipDispozitie` + `AIntratInCircuit`.

*[Confirmare empirică eMOL]* Regulamentele eMOL modelează revocarea ca eveniment distinct („retractarea actului valid prin care încetează efectele" — Scânteiești/Ferești art. 13) și prevăd, la atacarea în contencios, un **pas de reanaliză** (inițiatorul reanalizează în 5 zile: menține/modifică/revocă — Conop art. 20). Recomand să modelezi starea „atacat" cu un sub-flux de reanaliză → rezultat (menținut/modificat/revocat), nu doar un flag binar.

---

### 7. DISPOZIȚIA DE CONVOCARE a consiliului

**Temei — convocarea prin dispoziție a primarului.** *[TEXT DE LEGE]* Art. 133 alin. (1): „Consiliul local se întruneşte în şedinţe ordinare, cel puţin o dată pe lună, **la convocarea primarului**." Art. 133 alin. (2): ședințe extraordinare la convocarea: „a) primarului; b) a cel puţin unei treimi din numărul consilierilor locali în funcţie; c) primarului, ca urmare a solicitării prefectului". Art. 134 alin. (1): „Consiliul local se convoacă … a) **prin dispoziţie a primarului**, în cazurile prevăzute la art. 133 alin. (1), alin. (2) lit. a) şi c); b) prin convocare semnată de către consilierii locali care au această iniţiativă, în cazul prevăzut la art. 133 alin. (2) lit. b)."

**Concluzie pe tip de act.** *[INTERPRETARE PROPRIE + confirmare empirică]* Convocarea de către primar (ordinară + extraordinară lit. a) și c) **ESTE o dispoziție a primarului** — act administrativ **individual / de organizare** (vizează o ședință determinată, persoane determinate). În practică se emite cu temei expres art. 196 alin. (1) lit. b) și formula „DISPUNE / Se convoacă …" (confirmat: Dispoziția PMB nr. 1586/2024 „privind convocarea CGMB"; regulamentul eMOL Deta: primarul „va emite **dispoziția de convocare**" pornind de la proiectul ordinii de zi primit de la secretar). **Excepție:** convocarea inițiată de 1/3 din consilieri (art. 133 alin. (2) lit. b) **NU** e dispoziție — e „document de convocare semnat de consilieri" (art. 134 alin. (1) lit. b). Deci nu orice convocare = dispoziție.

**Ce trebuie să conțină.** *[TEXT DE LEGE]* Convocarea cuprinde data, ora, locul/modalitatea ședinței și are ca **anexă proiectul ordinii de zi** (art. 135 alin. (1) — proiectul ordinii de zi se redactează de secretar ca anexă la documentul de convocare). Termenele: art. 134 alin. (3) — data ședinței la **5 zile** de la comunicarea dispoziției (ordinară) / **3 zile** (extraordinară). Comunicarea către consilieri: art. 134 alin. (2) — în scris sau electronic, prin grija secretarului.

**Numerotare.** *[INTERPRETARE PROPRIE + confirmare empirică]* Dispoziția de convocare intră în **același registru general de dispoziții**, numerotată în secvența unică (confirmat: nr. 1586 în seria generală PMB). NU are registru separat.

**Publicare.** *[INTERPRETARE PROPRIE]* Fiind act **individual/de organizare**, **nu** intră sub obligația de publicare integrală în MOL (care vizează doar normativul, Anexa 1 lit. d). Unele UAT o publică totuși din transparență; legal, e suficientă înregistrarea în registru + comunicarea către consilieri. Pentru Cleriq: generarea automată a dispoziției de convocare la trimiterea convocării (bonus Faza 4 / Pas 4) o tratează ca dispoziție individuală numerotată, legată de ședință, **fără** publicare automată în MOL.

---

### 8. PROTECȚIA DATELOR (GDPR / Legea 190/2018) pentru dispozițiile individuale de personal

**Nepublicarea integrală = protecția structurală.** *[TEXT DE LEGE]* Așa cum s-a arătat la pct. 2, Anexa 1 art. 1 alin. (2) lit. d) **nu** prevede publicarea integrală a dispozițiilor individuale în MOL — exact mecanismul care ține actele de personal (cu CNP, adresă, date de sănătate la concedii medicale, sancțiuni etc.) **în afara expunerii publice**. Pentru ce totuși se publică (normativ), Anexa 1 art. 5 alin. (2) cere expres respectarea protecției datelor.

**Regimul CNP și al numerelor de identificare națională.** *[TEXT DE LEGE]* Legea 190/2018:
- Art. 2 lit. b) definește „numărul de identificare naţional" — CNP, seria/numărul actului de identitate, pașaport, permis de conducere, nr. asigurare socială de sănătate.
- Art. 4 alin. (1): prelucrarea unui număr de identificare naţional, „**inclusiv prin … dezvăluirea documentelor ce îl conţin**", se poate face în situaţiile de la art. 6 alin. (1) RGPD (temei legal necesar).
- Art. 4 alin. (2): când temeiul e **interesul legitim** (art. 6 alin. (1) lit. f RGPD), operatorul trebuie să instituie garanții: **măsuri tehnice şi organizatorice adecvate**, **desemnarea unui responsabil cu protecţia datelor (DPO)**, **termene de stocare specifice**, instruirea periodică a celor care prelucrează.
- Art. 7 (sarcină de interes public, art. 6 alin. (1) lit. e RGPD): garanții de **minimizare a datelor** (art. 5 RGPD), DPO dacă e cazul, termene de ștergere/revizuire.
- Art. 14 alin. (3)-(4): încălcarea art. 3-9 de către autorități/organisme publice = contravenție, amendă **10.000-100.000 lei** (regim sancționator distinct pentru sectorul public).

**Cadru RGPD + constituțional.** *[TEXT DE LEGE]* RGPD art. 6 alin. (1) lit. c) (obligație legală) și lit. e) (sarcină de interes public/exercitarea autorității publice) — temeiurile tipice pentru o primărie; art. 5 (legalitate, minimizare, integritate/confidențialitate). Constituția art. 26 (viața privată). *[JURISPRUDENȚĂ orientativă]* CJUE, cauza Lindqvist (C-101/01): publicarea pe internet a datelor de identificare = prelucrare; ANSPDCP a sancționat constant publicarea neautorizată a CNP/date de identificare.

**Implicații concrete pentru backend (must-have):**
- **Publicare individuale = nepublic implicit + override deliberat** (NU 409 dur): `TipDispozitie.Individual` nu se publică implicit; publicarea e un act deliberat al secretarului/Admin, cu avertisment + confirmare + motiv + audit (conform eMOL). Pentru actele cu date personale: anonimizare obligatorie înainte. Norma de bază (Anexa 1 lit. d — în MOL doar normativul) rămâne text de lege; default-ul nepublic + anonimizarea sunt protecția, nu imposibilitatea tehnică.
- **Conținutul integral al individualelor (PDF) NU pleacă pe rutele publice** (Faza 9). Registrul (Faza 5) expune doar metadate pentru individuale (nr., dată, obiect generic — fără nume/CNP în titlul public, dacă e expus public).
- **Control de acces intern** pe dispozițiile individuale (cine din aplicație vede PDF-ul de personal): aplicația e operator de date; minimizare + acces restricționat + termene de stocare.
- **Audit imutabil** (`IstoricActiuneAct`) pe accesul/operațiile cu acte de personal — util și pentru responsabilitate (accountability, RGPD art. 5 alin. (2) / art. 24).

---

## Recomandare de design software

### Matrice de stări × acțiuni, pe tip (Normativ / Individual)

**Dispoziție NORMATIVĂ** (regim aproape identic cu HCL):

| Stare | Editare conținut | Ștergere/înlocuire PDF semnat | „Anulează MOL" | Publicare MOL/portal |
|---|---|---|---|---|
| Draft / Numerotat | Permis | Permis (nu a circulat) | N/A | N/A |
| Semnat, NEpublicat, NEcomunicat | Permis cu motiv în audit | Permis cu motiv obligatoriu | **Fereastra legitimă** (Admin, motiv obligatoriu) | Permis (declanșează intrarea în circuit) |
| Publicat MOL / comunicat la prefect / în vigoare | **BLOCAT** | **BLOCAT TOTAL** | **BLOCAT** → redirecț. către Erată/act modificator | Deja publicat |

**Dispoziție INDIVIDUALĂ** (act de personal — **nepublic implicit**):

| Stare | Editare conținut | Ștergere/înlocuire PDF semnat | „Anulează MOL" | Publicare MOL/portal |
|---|---|---|---|---|
| Draft / Numerotat | Permis | Permis (nu a circulat) | N/A | Nepublic implicit; override deliberat (confirmare+motiv+audit) |
| Semnat, NEcomunicat | Permis cu motiv în audit | Permis cu motiv obligatoriu | „Anulează MOL" doar dacă a fost publicat prin override | Nepublic implicit; override deliberat |
| Comunicat destinatarului / în vigoare | **BLOCAT** | **BLOCAT TOTAL** | (doar dacă publicat anterior, înainte de efecte) | Nepublic implicit; override deliberat, cu anonimizare |

Reguli comune (transfer din HCL): orice corecție pe un act intrat în circuit → **redirecționare obligatorie** către Erată unificată (Faza 7) sau act modificator/abrogator; versionare append-only; hash per PDF; audit complet (cine, când, ce motiv, ce stare, ce hash).

### Reguli de business care intră în backend (sinteză acționabilă)

1. **Origine fără vot.** Entitate `Dispozitie` separată (nu moștenire din `Hcl`); fără `PunctOrdineZiId`, fără snapshot de vot, fără `TipMajoritate` (confirmă decizia #1 din plan_faza4).
2. **Semnatari = Emitent (primar) + SecretarContrasemnatura.** Gardă de completitudine la `Semneaza` (ambele roluri prezente). Modelează **explicit** și starea „contrasemnătură refuzată cu obiecție de legalitate" (motiv + autor + dată), cu expunere într-un registru al refuzurilor (Anexa 1 — „ALTE DOCUMENTE" lit. a). Derivarea emitentului/contrasemnatarului la data emiterii din funcții istorice: `CinEPrimarLa(data)` + „cine exercită atribuțiile de primar" (titular vs. înlocuitor de drept art. 163) + `CinESecretarulUatLa(data)`.
3. **`TipDispozitie { Normativ=1, Individual=2 }` ca discriminator de regim**, NU de numerotare. `EstePublicat`/`DataPublicareMol` au sens doar pentru `Normativ`. La `Individual`: **nepublic implicit**, publicarea = override deliberat (confirmare + motiv + audit + anonimizare), NU 409 dur — conform implementării eMOL.
4. **Comunicare la prefect pentru AMBELE tipuri**, 10 zile lucrătoare, alertă T-3, marcare manuală + audit (paralel `ComunicareHclPrefect`). Nu se diferențiază pe tip.
5. **Intrare în vigoare / `AIntratInCircuit`:** Normativ → la publicare/aducere la cunoștință publică; Individual → la comunicarea către destinatar. Pragul îngheață varianta semnată (latch identic HCL).
6. **Numerotare:** un singur registru de dispoziții per `InstitutieId` per `AnNumerotare`, index unic filtrat `(InstitutieId, AnNumerotare, Numar)`, ambele tipuri în aceeași secvență. Generalizare `IActNumerotat` (decizia #2).
7. **`MotivInvalidare` reutilizat direct**, cu: (a) eticheta `Retractat` → „Revocat de primar (emitent)"; (b) regulă de validare: revocarea proprie admisibilă **oricând** pentru `Normativ`, dar **doar înainte de `AIntratInCircuit`** pentru `Individual` (după → doar `AnulatDeInstanta`); (c) `AbrogatPrin...` derivat din relație; (d) `AnulatDeInstanta` cu sub-câmp opțional `reclamant` (Prefect / Terț vătămat / Autoritate emitentă), unde `Autoritate emitentă` e admis pentru `Individual` (art. 1 alin. (6)) dar **nu** pentru `Normativ` (ÎCCJ 74/2023).
8. **„Atacat de prefect + suspendat de drept"** = **stare/relație temporară**, NU `MotivInvalidare` (transfer identic din `research_MotivInvalidare`). Modelează un **sub-flux de reanaliză** (menținut/modificat/revocat), conform practicii eMOL (Conop art. 20), nu un flag binar.
9. **Dispoziția de convocare** (bonus): generată automat la trimiterea convocării, ca `Dispozitie` **individuală/de organizare**, numerotată în registrul general, legată de ședință/convocare, cu anexă „proiect ordine de zi", **fără** publicare automată în MOL. Doar pentru convocările făcute de primar (art. 133 alin. (1), (2) lit. a) și c) — NU pentru convocarea de 1/3 consilieri.
10. **Protecția datelor (must-have):** individualele de personal sunt nepublice implicit; orice publicare e override deliberat **cu anonimizare** a datelor cu caracter personal; minimizare în orice expunere (registru = doar metadate, fără nume/CNP în titlul public); control de acces intern; audit pe accesul la acte de personal; conformitate Legea 190/2018 art. 4/7 (DPO, termene de stocare, măsuri tehnice).

### Note de implementare (paritate / generalizări din plan_faza4 — validate juridic)

- `GeneratorDispozitie` **type-aware** (preambul cu competențele primarului + temei art. 155/196; „PRIMARUL … dispune:"; **fără** vot/cvorum). Pentru normativ: respectă normele de tehnică legislativă (Legea 24/2000). Decizia #6 (generator paralel, nu extras) e corectă.
- `GeneratorPdfAct` (bază comună PV+HCL+Dispoziție) — rule-of-three; `GeneratorPdfDispozitie` adaugă banner pe tip + secțiunea de semnături „Primar / Secretar general" (vs. „Președinte de ședință / Secretar").
- `IstoricActiuneAct` (cu `TipAct` + `ActId` referință slabă) — corect; auditul cross-act e necesar și pentru accountability RGPD pe acte de personal.
- **CRITIC — mentenanță:** adaugă `Dispozitii.CaleStocareSemnat` la scanul de orfani (`MentenantaController`), altfel cleanup-ul șterge fizic PDF-urile de personal semnate (impact și pe păstrarea probelor, și pe obligația de arhivare).
- **PRINCIPIU TRANSVERSAL — defer-to-secretar (reducerea riscului juridic prin design):** pentru deciziile juridic-incerte sau care țin de regimuri separate (intrarea în vigoare a actelor de personal — art. 528; publicarea unei individuale; clasificarea `Inexistent`; reanaliza după atacarea în contencios), aplicația NU hardcodează o regulă „perfectă juridic" — oferă **structura + opțiunile + auditul**, iar decizia o ia **secretarul general**, care e responsabilul legal (art. 240 alin. (2)-(3), art. 243). Așa procedează și eMOL (secretarul decide publicarea, reanaliza etc.). Efect: riscul juridic se mută corect către omul împuternicit de lege, iar Cleriq nu trebuie să fie autoritate juridică pe fiecare caz-limită. Regula de aur: **calculează ce e text de lege clar (numerotare, gărzi de circuit, semnatari); deferă către secretar ce e interpretabil sau ține de o lege separată.**

---

## Răspuns punctual la întrebările de domeniu din `plan_faza4_backend.md`

1. **Semnatari** → primar emitent (art. 240 alin. (1)) + secretar contrasemnătură pentru legalitate (art. 243 alin. (1) lit. a). Efect: aviz de legalitate ex-ante + răspundere (art. 240 alin. (2)-(3)). Primar absent: **înlocuitor de drept = viceprimar** doar în vacanță/suspendare/imposibilitate (art. 163) — **nu** prin delegare art. 157 pentru emiterea de dispoziții, **nu** pentru simplă absență fizică. `IServiciuFunctiiIstorice` are nevoie de `CinEPrimarLa(data)` + noțiunea de înlocuitor de drept.
2. **Publicare** → Normativ în MOL (Anexa 1 lit. d + art. 197 alin. (5)); **Individual = nepublic implicit** (text de lege — în MOL doar normativul). Publicarea unei individuale e posibilă doar ca **override deliberat** (confirmare + motiv + audit + anonimizare), pentru cazuri-limită legitime — nu un blocaj dur (vezi corecția din „Validare empirică"). Pe portal (Faza 9), implicit doar normativele.
3. **Comunicare la prefect** → **ambele tipuri**, 10 zile lucrătoare (art. 197 alin. (1)); controlul de legalitate pe toate (art. 200, 255).
4. **Numerotare** → **un singur registru** de dispoziții per UAT per an, ambele tipuri în aceeași secvență (Anexa 1; confirmat empiric). Separat de registrul HCL.
5. **Invalidare / revocare de primar** → enum reutilizat; eticheta `Retractat` → „Revocat de primar"; **corectare la framing**: distincția e normativ/individual, nu primar/consiliu. Individual intrat în circuit = irevocabil pentru primar, doar anulare de instanță (art. 1 alin. (6) Legea 554/2004).
6. **Temei legal generator** → primarul emite dispoziții: art. 155 (atribuții) + art. 196 alin. (1) lit. b); pentru normativ, redactare conform Legea 24/2000.
7. **Dispoziția de convocare** → dispoziție a primarului (art. 134 alin. (1) lit. a), act individual/de organizare, numerotată în registrul general, cu anexă „ordine de zi" (art. 135), comunicată consilierilor (art. 134 alin. (2)); fără publicare obligatorie în MOL. Declanșator: trimiterea convocării de către primar.

---

## Caveats — sursa fiecărei afirmații

**TEXT DE LEGE** (preluat din surse oficiale — legislatie.just.ro, Lege5, Codul administrativ publicat, Anexa 1 la Cod, Legea 190/2018, Constituție):
- Cod administrativ: art. 133, 134, 135 (convocare/ordine de zi); art. 152, 157, 163 (viceprimar/delegare/înlocuitor de drept); art. 155, 196 (atribuții primar / acte emise); art. 197 alin. (1), (3), (4), (5) (comunicare la prefect, obiecții, publicare); art. 198 alin. (1)-(2) și art. 199 alin. (1) (intrare în vigoare normativ/individual); art. 200 și art. 255 alin. (1)-(2) (control de legalitate prefect); art. 240 alin. (1)-(3) (semnare/răspundere); art. 243 alin. (1) lit. a), d), e) (atribuții secretar); art. 528 + Partea a VI-a, Capitolul X (intrarea în vigoare a actelor de personal pentru funcționari publici — regim separat).
- Anexa 1 la Cod: art. 1 alin. (2) lit. c), d) (ce se publică — HCL ambele, dispoziții doar normativ); art. 3 alin. (3) (identificare semnatari); art. 5 alin. (2) (format PDF + protecția datelor); secțiunea „ALTE DOCUMENTE" lit. a) (registrul refuzurilor de a semna/contrasemna).
- Legea 24/2000: art. 58, 64 (evenimente legislative/abrogare); art. 71 (rectificare/erată) — transfer din research_hcl.
- Legea 554/2004: art. 1 alin. (6) (autoanulare emitent pentru act intrat în circuit); art. 3 (prefect atacă, suspendare de drept).
- Legea 190/2018: art. 2 lit. b) (nr. identificare naţional); art. 4 alin. (1)-(2) (prelucrare CNP + garanții); art. 7 (interes public); art. 14 (contravenții autorități publice). RGPD art. 5, 6. Constituție art. 26, 123 alin. (5).

**JURISPRUDENȚĂ:** ÎCCJ (DCD) nr. 74/2023 (M. Of. nr. 12/2024) — emitentul nu poate cere anularea propriului act normativ (limitează `reclamant = Autoritate emitentă` la individuale); CCR Dec. 513/2019 (inexistență pentru nepublicare); CJUE C-101/01 Lindqvist (publicare pe internet = prelucrare). Cazul Castelu (Trib. Constanța 2025 / C. Ap. Constanța def. 2026) — transfer din research_hcl pentru limita strictă a eratei.

**INTERPRETARE PROPRIE** (concluzii de design, încadrarea cazurilor, marcate ca atare în text): criteriul practic normativ/individual; clasificarea convocării ca act individual/de organizare; matricea de stări × acțiuni; reluarea pragului de intangibilitate de la HCL; recomandările de model (entitate, enum, gărzi); concluzia că numerotarea unică pe registru nu se segmentează pe tip.

**VALIDARE EMPIRICĂ** (reference implementation): regulamentele și registrele publice eMOL (Deta, Conop, Scânteiești, Ferești, Sector 1) — sursa pentru maparea din secțiunea „Validare empirică". Confirmă cvasi-integral analiza juridică. **Recomandare corectată față de v1:** publicarea individualelor — de la „gardă dură 409" la „nepublic implicit + override deliberat (confirmare + motiv + audit + anonimizare)". Restul recomandărilor din v1 rămân valabile.

**ZONE DE INTERPRETARE — status și rezolvare** (marcate ca interpretare, nu text de lege; rezolvate prin surse primare + design, nu lăsate deschise):
- **Pragul exact de înghețare** (fereastra semnare → comunicare prefect → publicare/comunicare destinatar) — *rezolvat prin alegere conservatoare:* freeze la cel mai timpuriu moment verificabil (publicare pentru normativ / comunicare destinatar pentru individual). Confirmat indirect de practica eMOL: corecțiile se fac prin acte noi (modificare/abrogare), nu prin editarea originalului — deci originalul e de facto înghețat după ce circulă.
- **Regimul de comunicare/intrare în vigoare al actelor de personal** — *rezolvat prin pointer de lege + defer-to-secretar.* Regulamentele eMOL trimit la **art. 528 Cod administrativ**, care guvernează intrarea în vigoare a actelor privind nașterea/modificarea/suspendarea/sancționarea/încetarea raporturilor de serviciu ale funcționarilor publici (Cod adm., Capitolul X din Partea a VI-a; excepție de la regula generală art. 199). Regimul fin (art. 513-533, cu cale proprie de contestare — art. 527) e o **lege separată**, complexă. **Decizie de design: Cleriq NU codează art. 528 et al.** — aplicația tratează doar ciclul documentului (creat → semnat → comunicat → nepublicat); momentul substanțial de intrare în vigoare e determinarea secretarului/HR, înscrisă pe act. (Vezi principiul „defer-to-secretar" în Note de implementare.)
- **`reclamant` la `AnulatDeInstanta`** — *rezolvat prin jurisprudență.* Regulile sunt fixate de ÎCCJ 74/2023 (emitentul nu-și poate cere anularea propriului act normativ) + HP 2015 (primarul nu-și atacă propriile HCL); pentru dispoziție, prin analogie, `reclamant = Autoritate emitentă` e admis la individuale, nu la normative. E o constrângere de data-entry pe un sub-câmp, cu impact funcțional mic chiar dacă e ajustată ulterior.
- **`Inexistent` pentru o individuală** — *rezolvat prin defer-to-secretar:* clasificarea (inexistent vs. stare anterioară intrării în vigoare) o face secretarul la marcare; aplicația oferă valoarea + audit, nu impune o teorie juridică.
- **Caducitatea** — construcție doctrinară (Podaru), fără temei text-expres; implementabilă și utilă, de semnalat ca atare în documentația produsului.
- **Codul de procedură administrativă** (proiect în dezbatere la Senat, iunie 2026) — de urmărit: dacă va reglementa expres „rectificarea actelor administrative", reevaluați fluxul de erată după forma adoptată.

> **Status final:** concluziile ancorate în text de lege (semnatari, publicare doar normativ, comunicare ambele la prefect, registru unic, intrare în vigoare) fundamentează direct designul backend. Punctele de interpretare de mai sus sunt **rezolvate** prin surse primare + principiul defer-to-secretar (deciziile substanțiale incerte le ia omul responsabil legal — secretarul, art. 240/243 — cu aplicația oferind structură + audit). Echipa a planificat o verificare juridică finală la încheierea aplicației, ca pas de asigurare, nu ca precondiție de implementare.
