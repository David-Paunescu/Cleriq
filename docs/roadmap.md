Plan complet — de la finalul s43 până la pilot.
12 faze, în ordinea optimă pe baza dependențelor funcționale și a alternanței mărimii (deschidem cu lucruri mici, ducem modulul mare la mijloc, închidem cu deployment).

Faza 1 — Etapa 1 PV semnat: gardă aprobare oficială (1-2 sesiuni)
Marcare formală a momentului în care PV-ul a fost aprobat în ședința următoare (conform OUG 57/2019 art. 138). După această marcare, acțiunile distructive pe varianta semnată (Înlocuiește/Șterge) devin indisponibile. Frontend afișează indicator clar pe portalul public. Închide întrebarea deschisă din s43.
Începem cu o piesă mică, terminată — intrăm în ritm.
**Faza 1 — ÎNCHISĂ în s45.** 102 teste verzi (84 + 18). Smoke test curat. Lecție D7: gărzile de ștergere pentru entități cu referențiere bidirecțională trebuie să verifice AMBELE direcții. Prinsă la smoke test, nu în plan inițial.

Faza 2 — Trasabilitate istorică a funcțiilor (Lacuna 2) (1-2 sesiuni)
Capacitatea de a răspunde la întrebări de tipul "cine era secretar UAT / primar / președinte al unei anumite comisii / membru al unei anumite comisii / funcționar cu atribuții de avizare la o dată anume?". Critic juridic — la contestarea unui HCL după 2 ani, această informație trebuie să fie disponibilă.
A doua, nu mai târziu: toate modulele care urmează (A, B, C) consumă această informație. Construită acum, fără retrofit în trei locuri ulterior.
**Faza 2 — ÎNCHISĂ

Faza 3 — Modul A: HCL adoptat generat din date structurate + Comunicare Prefect (3-5 sesiuni)
Sub-componenta principală (Tier 1): generarea automată a HCL-urilor adoptate pornind de la datele existente (punct ordine de zi, vot, prezență). Conține: numerotare asistată anti-gap și anti-dublură per instituție per an; preambul cu temei juridic și dispozitiv conform OUG 57/2019; semnături derivate din trasabilitatea funcțiilor; anexe atașate (regulamente, organigrame, planuri); relații cu HCL-uri anterioare (modifică/abrogă/suspendă/pune în aplicare); status pe ciclu de viață de la adoptare la publicare. Cel mai mare time-saver pentru secretar.
Sub-componenta integrată — Comunicare Prefect: termenul legal de 10 zile de la adoptare pentru transmiterea HCL la Instituția Prefectului, cu alertă vizibilă la T-3 zile, marcare manuală a transmiterii (data + canal + număr înregistrare la prefect), audit complet. Funcționalitate paritară cu eMOL.
Începem cu valoarea cea mai mare, independent de Modul B.

Faza 4 — Modul C: Dispoziții primar (2-3 sesiuni)
Generarea și gestiunea dispozițiilor primarului. Două tipuri: normative (se publică) și individuale (acte de personal — nu se publică). Numerotare asistată separată de HCL. Bonus: dispoziția de convocare a ședinței consiliului devine act administrativ explicit, generat automat când Cleriq trimite convocarea — acum trimitem doar documentul tehnic, dar legal vorbind există o dispoziție de convocare separată.
Pattern similar cu Modul A, reutilizează experiența din faza precedentă; mai mică, intrăm relaxat în Modul B.

Faza 5 — Registre electronice cu detectare gap (1 sesiune)
Registrul cronologic al HCL-urilor și registrul cronologic al dispozițiilor, cu vizualizare per an, căutare, filtrare. Detectare automată a salturilor de numerotare — alertă vizibilă pentru secretar, indiferent dacă gap-urile provin din date anterioare Cleriq sau din intervenții directe în baza de date. Funcționalitate care previne erori la controlul de legalitate.
Piesă mică, depinde doar de A + C, ne mută în Modul B cu inerție.

Faza 6 — Modul B: Proiecte HCL + Flux avizare + Ședințe comisii (Lacuna 1) + Multi-inițiator (Lacuna 3) (6-8 sesiuni)
Cel mai mare modul. Trei sub-componente integrate:
Proiecte HCL pre-ședință: gestiunea proiectului de hotărâre înainte să devină HCL adoptat. Inițiator(i) multipli — Lacuna 3 — un proiect poate fi inițiat de un singur consilier, un grup de consilieri, primar, o comisie întreagă, sau un cetățean. Atașamente: expunere de motive, raport de specialitate. Status pe ciclu de viață (depus → în avizare → avizat → pe ordinea de zi → adoptat / respins / retras). Aviz de legalitate al secretarului (favorabil/nefavorabil cu motivare). Când proiectul ajunge "pe ordinea de zi", devine punct pe o ședință. Când e adoptat, generează automat HCL-ul (Modul A).
Ședințe comisii de specialitate (Lacuna 1): comisiile devin entități deliberative cu drepturi depline — propriile ședințe, prezență, vot, proces verbal. Funcționalitate paritară cu eMOL Expert. Necesar pentru orașele care respectă strict procedura legală (comisia se întrunește formal înainte de plen, dezbate proiectele, votează avizul).
Avize comisii: legătură explicită între punctul votat în ședința comisiei și avizul atașat la proiectul HCL. Tip aviz (favorabil/nefavorabil/cu condiții) + conținut + autor + dată. Avizele construite în ședințele comisiilor alimentează direct fluxul proiectelor HCL.
Depinde de Funcție, Modul A, Modul C — toate complete înainte. Cea mai grea piesă, o ducem când avem deja inerție.

Faza 7 — Modul H: Erată unificată (1-2 sesiuni)
Pattern unic pentru corectarea oficială a actelor administrative finalizate. Acoperă: erata pe PV finalizat (rezolvă întrebarea deschisă din s42 — typo material), erata pe HCL adoptat, erata pe dispoziție normativă. Actul de erată = al doilea act, legat de primul, cu propria numerotare și flux conform Legea 24/2000 — nu modifică actul original, îl completează.
După Modul B avem nevoie de decompresare cu o piesă mică — Erată oferă fix asta.

Faza 8 — Modul G: Raport anual Legea 52/2003 art. 7 (1-2 sesiuni)
Generarea automată a raportului anual de transparență decizională. Conține: proiecte HCL publicate cu termenul legal de 30 zile, dezbateri publice organizate, propuneri primite de la cetățeni, HCL-uri adoptate per categorie, participare cetățeni la ședințele publice. Format conform anexelor legii. Export PDF descărcabil de secretar pentru publicare pe site-ul primăriei.
Depinde de toate datele agregate (A + B + C complete) — așezat după modulele care alimentează raportul.

Faza 9 — Portal public extins + decizia SSR/CSR/SSG (2-3 sesiuni)
Două lucruri integrate într-o singură fază pentru că decizia SSR se aplică o singură dată tuturor rutelor publice:
Decizia tehnică SSR: Server-Side Rendering (Angular Universal), Static Site Generation (pre-render), sau Client-Side Rendering pur cu meta tags pentru indexare. Implicații pentru SEO (Legea 52/2003 cere acces ușor pentru cetățeni — căutarea Google e canalul real), pentru deployment, și pentru cost de complexitate. Decizia se ia în sesiune după evaluare concretă.
Extinderea portalului public existent: rute publice noi pentru HCL adoptate, dispoziții normative, proiecte HCL în consultare publică (cu termenul de 30 zile), registre, raport anual. Toate construite peste infrastructura portalului public existent din s7. Cetățeanul primește acces la întreg fluxul deliberativ al primăriei pe domeniul Cleriq, fără ca secretarul să facă upload manual.
Penultimul pas funcțional — totul e generat din date structurate construite în fazele precedente.

Faza 10 — Interfață SuperAdmin + configurare SMTP (1-2 sesiuni)
Interfață Angular pentru SuperAdmin: provisionare instituții noi (acum se face prin endpoint direct cu instrumente de test), configurare SMTP per instituție (acum se editează direct în baza de date), vizualizare globală a instituțiilor active, mentenanță orfani. Primul consumator real al gărzii pe rol existente.
Independentă, mică — închide partea de administrare globală.

Faza 11 — Management conturi (1 sesiune)
Mesajele politicii de parolă vin acum în engleză — traducere completă în română. La dezactivarea unui cont de utilizator, revocarea tuturor sesiunilor active (gap moștenit din s32, când am implementat refresh tokens). Interfață Angular pentru activare/dezactivare cont consilier de către Admin.
Independentă, mică — închide ultimele gap-uri funcționale.

Faza 12 — Deployment + precondiții (2-3 sesiuni)
Configurarea producției finale. Conține: rularea automată a migrațiilor la pornire pe mediul Production (notat din s30, neexecutat empiric); gardă pe migrație la multi-instance (precondiție notată din s27); rate limiting per-IP în reverse proxy ca strat de securitate suplimentar; configurare Hetzner cu server web + DB + Redis; setare DNS, certificate TLS automate; backup automat DB + Redis; validare end-to-end pe mediul Production.
Ultima fază. La acest punct, aplicația e funcțional completă — doar o ducem pe internet.

Post Pilot - Webhook Twilio + Test integration Whisper
Webhook Twilio delivery confirmation (~0.5-1 sesiune, post-pilot)
Endpoint care primește callback-uri asincrone de la Twilio cu status real delivered. Acum capturăm doar status sync (queued/sent) — confirmă acceptarea de către Twilio, NU livrarea efectivă la telefon. Pentru audit legal al comunicării convocării, delivered real e dovada mai puternică. Tehnic: endpoint public dedicat + update pe IncercariTrimitere + posibil câmp DataLivrare separat de TrimisLa. Precondiție: URL public real (motiv pentru care se face post-deployment). Lăsat post-pilot pentru că status sync e acceptat ca dovadă suficientă în prima fază; upgrade-ul vine după feedback piloți despre cât de strict cer auditul.

Test integration pentru wrapper Whisper (~1 sesiune, post-pilot)
Suită de teste integration cu HandlerHttpFals care simulează răspunsurile wrapper-ului learnedmachine/whisperx-asr-service, fără pod real. Bug-ul Form-vs-Query din s24-s25 (task/language/initial_prompt trimise greșit ca query string în loc de multipart form-data) ar fi fost prins instant — singura integrare externă majoră fără teste de contract. Acoperă: forma exactă a cererii HTTP, schema răspuns {"text": [...]}, status codes 200/4xx/5xx/timeout, semantica retry-ului. Lăsat post-pilot pentru că configurația actuală e validată empiric și pinned pe SHA (Docker tag); riscul real apare la următorul upgrade Whisper sau wrapper — testele devin gardă pre-upgrade.
