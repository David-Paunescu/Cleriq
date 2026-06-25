# Context proiect: Aplicație management ședințe consiliu local

## Cine sunt eu
Sunt programator incepator, cu experiență în **.NET, Angular și SQL Server**. Vreau să construiesc un SaaS pentru instituții publice din România, pe care să îl vând prin achiziție directă pe SEAP (sub pragul de 270.120 lei, fără licitație).
---

## Ce construim

O aplicație SaaS de **management al ședințelor consiliului local** pentru primăriile din România, cu **transcriere automată audio prin Whisper self-hosted** ca diferențiator principal față de competiție.

### Problema rezolvată
Secretarii primăriilor:
- Transcriu manual procesele verbale ascultând înregistrările audio (durează ore întregi)
- Convoacă consilierii manual prin email/WhatsApp
- Gestionează prezența și votul pe hârtie sau în Excel
- Publică manual documentele în Monitorul Oficial Local

### Piața țintă
**Orașe și orașe municipii cu 20.000–100.000 locuitori** (ex: Focșani, Piatra Neamț, Baia Mare, Buzău, Slobozia, Deva). Municipiile mari au deja soluții scumpe (Bosch Dicentis, TIDEN). Comunele mici au volum prea mic. Segmentul de mijloc este neacoperit.

Potențial: 300–500 clienți × 60 €/lună = 18.000–30.000 €/lună la penetrare completă.

---

## Funcționalități stabilite (ce construim)

### Faza 1 — Fundație și MVP
- Autentificare, roluri, securitate
- Gestionare consilieri (membri, comisii, mandat)
- Creare ședință cu agendă și documente atașate
- Convocare digitală consilieri (email + SMS)
- Vot electronic (Da/Nu/Abținere, nominal sau secret)
- Înregistrare prezență + calcul cvorum automat
- Schelet automat proces verbal din date structurate
- Portal public transparență (ședințe, voturi, documente)
- Publicare automată în Monitorul Oficial Local (conform Legea 52/2003)

### Faza 2 — Whisper și lansare
- Upload înregistrare audio post-ședință
- Transcriere automată prin **Whisper self-hosted** (audio nu părăsește serverul)
- Editor draft proces verbal (secretarul corectează transcrierea și validează)
- Export PDF final semnat
- Primii clienți pilot (Slobozia și Deva au arătat interes explicit)

### Faza 3 — Creștere (Luna 7–12)
- Lansare comercială pe SEAP (achiziție directă)
- Vânzare activă către primarii
- Adaptare pentru alte institutii, pe langa primarii

### Ce NU construim (intenționat)
- Flux documente pre-ședință (proiecte HCL, avize comisii, dispoziții primar) — eMOL face asta bine, nu are sens să duplicăm
- Integrare cu sisteme naționale centrale (X-Road, ROeID, ANAF)
- Modul GIS sau urbanism
- Streaming video live (opțional, versiune ulterioară)

---

## Stack tehnologic

- **Frontend:** Angular
- **Backend:** .NET (C#)
- **Bază de date:** SQL Server
- **Transcriere audio:** Whisper self-hosted (Whisper.net — librărie .NET nativă, zero Python necesar)
- **GPU pentru Whisper:** on-demand (RunPod, Vast.ai — ~0.50–1.00 $/oră, doar când procesezi)
- **Hosting:** Hetzner sau similar (~30–40 €/lună server web + BD)

### De ce Whisper self-hosted (nu OpenAI API)
Ședințele de consiliu local conțin deliberări sensibile. Trimiterea audio către servere externe (OpenAI, Azure) ridică probleme de GDPR și suveranitate a datelor. Cu Whisper self-hosted, audio nu părăsește infrastructura. Costul total de hosting pentru 100 clienți: ~60–75 €/lună.

---

## Competiția identificată

| Competitor | Ce face | Slăbiciune față de noi |
|---|---|---|
| **eMOL** (Big Media/Faxmedia) | Flux complet documente HCL, MOL, vot electronic. 554+ primării. | **Nu are transcriere audio.** Deva are contract separat de transcriere tocmai din această cauză. |
| **Bosch Dicentis** | Hardware fizic de vot în sală. Zeci de mii de euro. | Segment complet diferit (municipii mari cu bugete mari). Nu suntem în competiție directă. |
| **ePRIM** (SOBIS Solutions) | Videoconferință + modul ședințe online. | Vechi, fără transcriere, PV tot manual. |
| **TIDEN** | Folosit de Oradea, produs necunoscut public. | Neidentificat — probabil soluție custom locală. |
| **Galați** | Software intern custom, nedivulgat. | Nu e disponibil altor primării. |

**Diferențiatorul nostru principal:** Suntem singurul produs care combină managementul ședinței cu transcriere audio automată self-hosted. Niciun competitor nu face asta.

---

## Validare de piață (răspunsuri Legea 544/2001)

Am trimis cereri de informații publice la 20 de primării. Rezultate relevante:

- **Slobozia:** Fără software dedicat. PV manual. ~80 ore pregătire/ședință. **Interes explicit: DA.**
- **Deva:** Are e-mol + contract extern de transcriere. A cerut prezentarea soluției noastre.
- **Pitești:** Fără software dedicat. PV manual. Interesat, dar nu acum.
- **Sector 6:** Fără software dedicat. Parțial automatizat.
- **Arad:** Folosesc Zoom. PV manual. Nu sunt interesați momentan.
- **Craiova:** ePRIM. PV manual (ascultă înregistrarea).
- **Brașov:** Bosch Dicentis. Parțial automatizat. Nu sunt interesați de schimbare.
- **Oradea:** TIDEN. PV parțial automatizat. Nu sunt interesați.
- **Galați:** Software intern custom. PV automat. Mulțumiți cu ce au.
- **Iași:** Transcriere automată proprie. Nu sunt clienți țintă.
- **Suceava:** Webex/Discentis pentru vot. PV manual. Evaluează soluții.
- **Târgu Mureș:** Parțial digitalizat. Posibil transcriere automată.

**Concluzie validare:** Unele din primăriile mari (municipii) sunt fie deja echipate, fie refractare. Segmentul preferat sunt orașele medii necontactate încă.

---

## Cadrul legal relevant

- **OUG 57/2019 (Codul Administrativ)** — art. 133–140: convocare, cvorum, vot electronic permis, proces verbal cu vot nominal, ședințe hibride permise
- **Legea 52/2003** — transparență decizională: publicare proiecte HCL cu 30 zile înainte, publicare procese verbale cu vot nominal, raport anual
- **Legea 544/2001** — liberul acces la informații publice (folosită pentru cercetare de piață)
- **Legea 98/2016** — achiziții publice: sub 270.120 lei (~54.000 €) = achiziție directă fără licitație

---

## Model de business

- **Tip:** SaaS cu abonament lunar
- **Preț țintă:** 40–80 €/lună per primărie (sub pragul de achiziție directă)
- **Canal de vânzare:** SEAP (catalog electronic, achiziție directă), contact direct cu secretari/primari
- **Riscuri principale:**
  - Plăți întârziate (media ~65 zile la stat) → SaaS lunar în avans, întrerupere automată la neplată
  - Ciclu lung de vânzare → contact uman direct, word-of-mouth între primari
  - eMOL poate adăuga transcriere → fereastra de oportunitate este acum

---

## Inspirație: sistemul VOLIS din Estonia

VOLIS (Kohaliku omavalitsuse VOLIkogu InfoSüsteem) este echivalentul estonian, folosit de ~50% din cele 79 de UAT-uri estoniene. Open-source (GPL), operat de Novian Eesti OÜ. Funcționalități: agendă, vot electronic, proces verbal generat din date structurate, portal public, streaming live, autentificare prin eID. **Nu folosește transcriere audio** — procesul verbal se generează din date structurate introduse în timp real. Noi mergem mai departe cu Whisper.
