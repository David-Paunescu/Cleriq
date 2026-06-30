# Analiză juridică: Modificarea unui HCL publicat în MOL vs. erată — recomandare de design pentru Cleriq

## TL;DR
- **Un HCL semnat, publicat în MOL și intrat în vigoare NU poate fi modificat sau șters direct în aplicație.** Corecturile se fac exclusiv prin rectificarea erorilor materiale (Legea 24/2000, art. 71) sau printr-un act administrativ nou trasabil (modificare/abrogare/act distinct), iar funcția „Anulează MOL → modifică varianta semnată → republică" trebuie **eliminată** ca operațiune de modificare a fondului.
- **„Anulează publicarea MOL" rămâne legitim DOAR** ca instrument de corecție a unei erori de înregistrare/tehnice asupra unui act care **nu a produs încă efecte** (fișier greșit încărcat, dată de înregistrare greșită — înainte de comunicarea la prefect și aducerea la cunoștință publică). După acel moment intervine intangibilitatea.
- **Distincția cheie:** *revocarea/retractarea* (de către emitent) este interzisă după ce actul individual a intrat în circuitul civil și a produs efecte; *anularea* o dispune doar instanța de contencios; *abrogarea* scoate din vigoare actul normativ printr-un act nou; *erata* corectează numai erori pur materiale, nu fondul — exact ce a confirmat jurisprudența recentă în cazul Castelu (2025–2026).

## Key Findings

**1. Regimul publicării în MOL.** Conform Anexei nr. 1 la Codul administrativ (OUG 57/2019), art. 5 alin. (2), „publicarea oricărui act administrativ se face în format «pdf» editabil pentru a se păstra macheta și aspectul documentului intacte, astfel încât acesta să arate exact așa cum a fost realizat și să poată fi tipărit corect de oricine și de oriunde, fără să cuprindă semnăturile olografe ale persoanelor". MOL este publicația oficială proprie a fiecărei UAT, cuprinsă în structura paginii de internet a UAT (Anexa 1, art. 1). Întrucât Cleriq ESTE mediul de publicare, modificarea unui act după publicare = modificarea însăși a actului oficial din MOL.

**2. Intrarea în vigoare și momentul intangibilității.** Codul administrativ stabilește momente clare:
- **Art. 197 alin. (1):** secretarul general comunică actele prefectului în cel mult 10 zile lucrătoare de la adoptare/emitere.
- **Art. 198 alin. (1):** „Hotărârile și dispozițiile cu caracter normativ devin obligatorii de la data aducerii lor la cunoștință publică"; alin. (2): aducerea la cunoștință publică se face în termen de 5 zile de la comunicarea oficială către prefect.
- **Art. 199 alin. (2):** hotărârile cu caracter individual produc efecte juridice de la data comunicării către persoanele cărora li se adresează.

Din momentul comunicării către prefect și al aducerii la cunoștință publică, actul a „ieșit" din sfera internă a UAT și a început să producă efecte — deci nu mai poate fi atins direct.

**3. Principiul intangibilității și revocabilitatea cu excepțiile ei.** Doctrina și jurisprudența române sunt constante: actele administrative normative sunt oricând revocabile (se scot din vigoare prin abrogare), dar **actele administrative individuale care au intrat în circuitul civil și au produs efecte juridice NU mai pot fi revocate de emitent** — pot fi doar anulate de instanță. Temei: **art. 1 alin. (6) din Legea 554/2004**: „Autoritatea publică emitentă a unui act administrativ unilateral nelegal poate să solicite instanței anularea acestuia, în situația în care actul nu mai poate fi revocat întrucât a intrat în circuitul civil și a produs efecte juridice." ÎCCJ – Completul pentru dezlegarea unor chestiuni de drept, prin **Decizia nr. 74 din 20 noiembrie 2023** (Dosar nr. 4.899/118/2021, sesizare a Curții de Apel Constanța, publicată în M.Of. nr. 12 din 8 ianuarie 2024), a statuat că „autoritatea publică emitentă a unui act administrativ unilateral cu caracter normativ nu poate solicita instanței anularea acestuia" — adică acțiunea în anulare a propriului act prevăzută de art. 1 alin. (6) este admisă **exclusiv pentru actele individuale** intrate în circuitul civil.

**4. Erata / rectificarea — limită strictă.** **Legea 24/2000, art. 71**: „(1) În cazul în care după publicarea actului normativ se descoperă erori materiale în cuprinsul său, se procedează la publicarea unei note cuprinzând rectificările necesare. (2) Se interzice modificarea prevederilor unor acte normative prin recurgerea la operațiunea de rectificare, care trebuie limitată numai la erorile materiale. (3) Rectificarea se face la cererea organului emitent, cu avizul Consiliului Legislativ." Modificarea de fond se face exclusiv prin **evenimente legislative** (art. 56/58: modificare, completare, abrogare, suspendare, republicare), adoptate printr-un act de aceeași forță juridică sau superioară.

**5. Jurisprudență direct relevantă (cazul Castelu).** În acțiunea de tutelă administrativă introdusă de Prefectul Județului Constanța la 16.07.2025 (Dosar nr. 3504/118/2025), Tribunalul Constanța a admis cererea și a anulat HCL Castelu nr. 2/10.01.2025, prin care Consiliul Local încercase „îndreptarea erorii materiale" din HCL nr. 90/10.10.2024 modificând numărul de voturi de la 7 la 8 (pentru a acoperi retroactiv lipsa cvorumului). Curtea de Apel Constanța – Secția de contencios administrativ și fiscal a respins recursul Consiliului Local ca nefondat în termenul din 20 aprilie 2026, soluția fiind definitivă. Considerentele instanței de fond:
> „Corectarea erorilor materiale are așadar o accepțiune limitată în dreptul administrativ, vizând inadvertențe de transcriere sau de redactare, care nu modifică conținutul ori efectele juridice ale actului, fiind admisibilă doar în măsura în care prin corecție nu se schimbă soluția juridică adoptată."

Acolo unde corecția alterează rezultatul votului „nu poate fi vorba despre o eroare materială, ci despre o modificare pe fond care pentru a fi remediată presupune reluarea procedurii și un nou vot în condițiile legii, iar nu un act de «îndreptare»". Procesul-verbal al ședinței (care arăta clar 7 voturi „pentru") este „un înscris autentic care face deplină dovadă față de orice persoană, până la declararea sa ca fals", iar o declarație ulterioară a unui consilier nu poate schimba rezultatul votului. Instanța a mai reținut că, deși art. 71 din Legea 24/2000 nu se aplică actelor individuale, „Codul administrativ nu consacră un mecanism de «validare» retroactivă a unei hotărâri lipsite, la data adoptării, de numărul de voturi cerut de lege".

## Details

### (a) Este legal să modifici/ștergi documentul semnat al unui HCL publicat și în vigoare?
**Nu.** [*Text de lege citat:*] Art. 198–199 Cod administrativ stabilesc că actul produce efecte de la aducerea la cunoștință publică (normativ), respectiv de la comunicare (individual); art. 71 alin. (2) din Legea 24/2000 interzice modificarea prevederilor prin rectificare. [*Interpretare proprie:*] Întrucât MOL în Cleriq este chiar publicația oficială, suprascrierea/ștergerea PDF-ului semnat = alterarea actului oficial publicat. Corecturile se fac EXCLUSIV prin:
- **îndreptarea erorii materiale / rectificare** (notă de rectificare) — doar pentru greșeli pur formale (typo, greșeli de transcriere, trimiteri greșit inserate) care nu ating fondul;
- **act administrativ nou** (HCL de modificare/completare/abrogare, sau erată ca act distinct) — pentru orice schimbare de fond, adoptat cu același cvorum și aceeași procedură ca actul inițial.

Regimul intangibilității: actul normativ se schimbă doar prin eveniment legislativ printr-un act de forță cel puțin egală; actul individual intrat în circuitul civil este irevocabil pentru emitent și poate fi doar anulat de instanță [art. 1 alin. (6) Legea 554/2004; Decizia ÎCCJ 74/2023].

### (b) Cum se raportează „Anulează MOL + modifică" la erată? Trebuie eliminată?
[*Interpretare proprie, fundamentată pe texte:*] Lanțul tehnic „anulează publicarea MOL → înlocuiește/șterge PDF-ul semnat → republică" ocolește exact intangibilitatea pe care legea o impune. El permite o modificare de fond fără act nou, fără vot, fără trasabilitate — adică tocmai conduita pe care instanța a sancționat-o în cazul Castelu. **Funcția trebuie eliminată ca mecanism de modificare a conținutului unui act care a circulat.**

Ce rămâne legitim pentru „Anulează MOL": **doar corecția unei erori de ÎNREGISTRARE/tehnice** — fișier greșit urcat, dată de înregistrare greșită, document duplicat — și **DOAR înainte ca actul să producă efecte** (înainte de comunicarea la prefect și de aducerea la cunoștință publică). Orice corecție de fond se redirecționează obligatoriu către fluxul de erată/act nou.

### (c) În ce condiții este admisibilă „anularea" unei publicări în MOL?
Trebuie distinse net două situații:
- **(i) Eroare de înregistrare în sistem, act care nu a produs încă efecte:** admisibilă tehnic, ca o corecție de „back-office" (ex.: s-a urcat PDF-ul altui HCL, s-a greșit data calendaristică de înregistrare). Aici nu vorbim de o operațiune juridică asupra actului, ci de remedierea unei erori de operare, înainte ca actul să fi intrat în circuit.
- **(ii) Retragerea/revocarea unui act deja publicat și în vigoare:** inadmisibilă pentru emitent. Diferențele juridice:
  - **Revocare** = operațiunea prin care însuși organul emitent (sau cel ierarhic superior) desființează actul; interzisă pentru actele individuale după intrarea în circuitul civil și producerea de efecte.
  - **Retractare** = revocarea dispusă chiar de organul emitent (caz particular al revocării).
  - **Anulare** = desființarea pentru nelegalitate, dispusă de **instanța de contencios administrativ**. Consiliul local NU își poate anula singur actul intrat în circuit.
  - **Abrogare** = scoaterea din vigoare a unui act normativ printr-un act nou de aceeași forță (eveniment legislativ, Legea 24/2000); operează pentru viitor, nu retroactiv.
  - **Rolul prefectului (tutela administrativă):** **art. 200 Cod administrativ** — „Dispozițiile primarului, hotărârile consiliului local și hotărârile consiliului județean sunt supuse controlului de legalitate exercitat de către prefect conform prevederilor art. 255." **Art. 255 alin. (2):** „Prefectul poate ataca actele autorităților prevăzute la alin. (1) pe care le consideră ilegale, în fața instanței competente, în condițiile legii contenciosului administrativ." Prefectul NU anulează actul — îl atacă; instanța anulează (art. 123 alin. (5) din Constituție: „Prefectul poate ataca, în fața instanței de contencios administrativ, un act al consiliului județean, al celui local sau al primarului… Actul atacat este suspendat de drept").

[*Zonă de incertitudine — necesită confirmarea unui avocat:*] Granița exactă a momentului „a produs efecte" pentru un HCL individual vs. normativ și gestionarea ferestrei dintre semnare și comunicarea la prefect. Recomand ca pragul tehnic de blocare să fie cel mai timpuriu moment verificabil (publicarea/comunicarea), pentru siguranță.

### (d) Recomandare concretă de design software

**Reguli de business pe stări (matrice acțiuni):**
| Stare HCL | Editare conținut | Ștergere/înlocuire PDF semnat | „Anulează MOL" |
|---|---|---|---|
| Draft / Numerotat | Permis | Permis (nu a circulat) | N/A |
| Semnat, NEpublicat și NEcomunicat | Permis cu motiv în audit | Permis cu motiv obligatoriu în audit | **Fereastra legitimă** (Admin, motiv obligatoriu) |
| Publicat în MOL / comunicat la prefect / în vigoare | **BLOCAT** | **BLOCAT TOTAL** | **BLOCAT** → redirecționare către Erată |

Orice acțiune de corecție pe un act publicat declanșează **redirecționarea obligatorie** către fluxul de **Erată unificată** (act nou, numerotare proprie, legat de original) sau către flux de act modificator/abrogator.

**Garanții obligatorii:**
- eliminarea lanțului „Anulează MOL → modifică PDF → republică" pentru orice act care a circulat;
- păstrarea „Anulează MOL" doar pentru rol **Admin** și doar pentru acte care nu au produs efecte, cu **motiv obligatoriu, timestamp, user, IP** salvate imutabil în audit log;
- **versionare append-only:** nicio versiune publicată nu se suprascrie; orice corecție = obiect nou legat de original;
- **hash criptografic** pentru fiecare PDF publicat, astfel încât intangibilitatea să fie demonstrabilă tehnic;
- erata trebuie să fie un act distinct, cu propriul flux Draft → Numerotat → Semnat → Publicat, afișat lângă originalul intact;
- log de audit complet: cine, când, ce motiv, ce stare, ce document, ce hash.

## Recommendations
1. **Imediat:** dezactivați lanțul „Anulează MOL → modifică/șterge varianta semnată → republică" pentru orice act cu status „publicat / comunicat / în vigoare". Înlocuiți butonul cu redirecționare către „Creează erată" sau „Creează act modificator". *Benchmark de schimbare:* dacă un act este încă în stare „semnat, necomunicat", acțiunea de corecție rămâne permisă.
2. **Restrângeți „Anulează publicarea MOL"** la rol Admin + condiția ca actul să nu fi fost comunicat la prefect / adus la cunoștință publică, cu motiv obligatoriu salvat în audit.
3. **Implementați versionare append-only și hash** pentru fiecare PDF publicat, ca dovadă tehnică a intangibilității.
4. **Praguri care schimbă recomandarea:** dacă viitorul **Cod de procedură administrativă** — proiect adoptat de Camera Deputaților și aflat în dezbatere la Senat (text publicat pe senat.ro, doc. 26L311FC), al cărui art. 1 prevede reglementarea „cadrului juridic unitar, clar și previzibil aplicabil procedurilor administrative generale și speciale, condițiilor de emitere, executare și control al actelor administrative" — va reglementa expres „rectificarea actelor administrative" (prevăzută ca articol distinct în proiecte), reevaluați fluxul de erată pentru a-l alinia textului adoptat.
5. **Validare juridică finală** cu un avocat de drept administrativ pentru pragul exact de blocare și pentru formularea notelor de rectificare vs. acte modificatoare.

## Caveats
- **Legea 24/2000 vizează expres actele NORMATIVE.** Aplicarea „eratei"/rectificării la HCL individuale este o extindere prin analogie — în cazul Castelu instanța a reținut chiar că art. 71 nu se aplică actelor individuale, dar a analizat oricum legalitatea pe principiul general al legalității. Necesită confirmarea unui avocat.
- **Distincția normativ/individual determină regimul** (normativ = revocabil/abrogabil oricând prin act nou; individual = irevocabil după intrarea în circuit, doar anulabil de instanță). Aplicația trebuie să permită clasificarea corectă a fiecărui HCL, întrucât de ea depinde ce flux de corecție este legal.
- **Codul de procedură administrativă era în stadiu de proiect/dezbatere** la data analizei (iunie 2026); verificați forma finală adoptată înainte de a fixa definitiv fluxul de erată.
- Unde am citat texte de lege am indicat numărul exact al articolului (art. 197–200, 255 Cod administrativ; art. 71, 56/58 Legea 24/2000; art. 1 alin. (6), art. 3, art. 14–15 Legea 554/2004; art. 123 alin. (5) Constituție); unde am formulat concluzii de design sau am interpretat efectele juridice am marcat explicit „interpretare proprie" și „zonă de incertitudine — confirmare avocat".