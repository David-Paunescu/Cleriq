Plan Faza 4 (backend) — Modul C: Dispoziții primar

## Context de pornire (ce reutilizăm din Modul A / HCL)
Dispoziția primarului e un act administrativ cu același ciclu de viață ca HCL
(Draft → Numerotat → Semnat), dar cu origine și reguli diferite. Diferențe esențiale:
- **Origine:** NU dintr-un punct de ordine de zi / vot de consiliu — primarul o **emite**
  unilateral. Deci fără `PunctOrdineZiId`, fără snapshot de vot, fără `TipMajoritate`.
- **Semnatari:** **primar (emitent) + contrasemnătură secretar** — nu președinte+secretar,
  fără cazul art. 140.
- **Două tipuri** (`TipDispozitie`): **Normativ** (se publică în MOL) vs. **Individual**
  (acte de personal — NU se publică; date cu caracter personal).
- **Numerotare** într-un **registru propriu** (separat de HCL), per instituție per an.

Piese HCL de citit ca paritate (le adaptăm, nu le reinventăm): `ServiciuNumerotareHcl`,
`GeneratorHcl`, `GeneratorPdfHcl` (+ `GeneratorPdfProcesVerbal`), `HclController`
(signed variant + PublicareMol/AnuleazaPublicareMol + Invalidare), `IstoricActiuneHcl` +
`AIntratInCircuit`, `ComunicareHclPrefectController`, `IServiciuFunctiiIstorice`, `AppDbContext`
(tenant + soft-delete cascade), `MentenantaController` (scan orfani).

**Rule-of-three declanșat la Faza 4** (notat în `plan_faza3_partea3_backend.md`):
- **Generator PDF** = al 3-lea (PV + HCL + Dispoziție) → **extragem bază comună** `GeneratorPdfAct`.
- **Variantă semnată (Nivel 1)** = al 3-lea (PV + HCL + Dispoziție) → **extragem helper comun**
  de stocare/încărcare/freeze.

## Decizii de arhitectură (reuse) — recomandate, cu motiv scurt
1. **Entitate separată `Dispozitie`** (NU moștenire EF din `Hcl`). *Motiv:* origine, semnatari și
   regula Normativ/Individual diferă suficient; o tabelă partajată ar umple `Hcl` de coloane
   nullable + discriminator. Reutilizăm prin **servicii/abstracții comune**, nu prin tabelă comună.
2. **Generalizăm serviciul de numerotare** la o abstracție de registru (`IActNumerotat` implementat
   de `Hcl` și `Dispozitie`; `TipRezultatAtribuire` redenumit act-agnostic). *Motiv:* logica
   anti-lacună + compare-and-swap pe indexul unic e identică și **sensibilă la concurență** —
   duplicarea ar fi un risc de bug. Excepție conștientă de la rule-of-three (generalizăm la a 2-a
   folosire fiindcă costul unui bug în cod duplicat e mare). Plasă: `TesteNumerotareHcl` rămâne verde.
3. **Lifecycle enum partajat:** redenumesc `StatusHclRedactional` → **`StatusActRedactional`**
   (Draft/Numerotat/Semnat). *Motiv:* ciclu identic, o singură sursă de adevăr, evită drift între
   2 enum-uri. E un rename mecanic, acoperit de teste. (Alternativă low-churn: enum paralel — o resping.)
4. **`MotivInvalidare` reutilizat direct** (numele e deja act-agnostic). Singura atenție: eticheta
   `Retractat` zice „de consiliul emitent" — la dispoziție e **revocare de către primar**; ajustăm
   eticheta sau o facem generică. Vezi întrebările de domeniu.
5. **Jurnal de audit generalizat:** redenumesc `IstoricActiuneHcl` → **`IstoricActiuneAct`** cu
   `TipAct` (Hcl/Dispozitie) + `ActId` (referință slabă, fără FK). *Motiv:* auditul e prin natură
   cross-act; o referință slabă evită o coloană-per-tip și e mai corectă pentru un log imuabil
   (supraviețuiește chiar dacă actul e șters; renunțăm la soft-cascade-ul din S57). Tabel mic →
   refactor ieftin cât e proaspăt. (Alternativă: tabel paralel `IstoricActiuneDispozitie` — mai
   multă duplicare; o resping.)
6. **Generator de conținut paralel** (`GeneratorDispozitie`), NU extras. *Motiv:* doar a 2-a folosire
   a pattern-ului „generator de act din date structurate"; template-ul diferă (preambul cu
   competențele primarului, fără vot). Refolosim structura (header, baner invalidare, secțiuni),
   nu codul.

## Întrebări de domeniu / legale de confirmat (eventual mini-research, ca la HCL)
1. **Semnatari:** primar emitent + contrasemnătură secretar (art. 196/240 Cod adm.?). Cazul primar
   absent → viceprimar/împuternicit. Cum derivăm? (`IServiciuFunctiiIstorice` are nevoie de
   `CinEPrimarLa(data)` pe lângă `CinESecretarulUatLa`.)
2. **Publicare:** Normativ → MOL (Legea 52/2003 + Cod adm.); Individual → NU se publică. De confirmat
   că individualele nu ajung nici pe portal.
3. **Comunicare la prefect:** art. 197 — secretarul comunică prefectului **dispozițiile primarului**.
   Ambele tipuri? (Controlul de legalitate se aplică tuturor.)
4. **Numerotare:** un **singur registru** de dispoziții per instituție per an (ambele tipuri în
   aceeași secvență)? Probabil da — de confirmat.
5. **Invalidare:** relevanța „revocării de către primar" (primarul își poate revoca propria dispoziție,
   spre deosebire de consiliu la actele normative). Eticheta/valoarea `Retractat`.
6. **Temei legal** pentru generator (primarul emite dispoziții — art. 155 / 196 Cod adm.).
7. **Dispoziția de convocare** (bonus): ce tip e + ce date poartă + declanșatorul (la trimiterea
   convocării).

## Construcția pe pași (backend), teste-verzi-la-fiecare-pas
Lucrăm ca în S57: fiecare pas = cod + teste verzi înainte de a trece mai departe (NU amânăm testele).

**Pas 1 — Strat de date + numerotare**
- Entitate `Dispozitie` (paralel `Hcl`, fără cele HCL-specifice): `Numar?`, `AnNumerotare?`,
  `TipDispozitie`, `Titlu`, `Continut?`, `DataEmitere`, `DataIntrareInVigoare?`,
  `Status (StatusActRedactional)`, cele 5 câmpuri signed, `EstePublicat`, `DataPublicareMol (DateOnly)`,
  `PublicataDe?`, `AIntratInCircuit`, câmpurile de invalidare (+`MotivInvalidareAltulText`),
  `InstitutieId`. + `SemnatarDispozitie` (rol Emitent / SecretarContrasemnatura).
- Enum-uri: `TipDispozitie { Normativ=1, Individual=2 }`; rename `StatusHclRedactional` →
  `StatusActRedactional`; rename `IstoricActiuneHcl` → `IstoricActiuneAct` (+ `TipAct`, `ActId`).
- `AppDbContext`: DbSet-uri, index unic filtrat `(InstitutieId, AnNumerotare, Numar)` pe `Dispozitii`
  (registru propriu), FK Restrict + soft-cascade (`case Dispozitie`), constraint semnatari.
- Generalizare `ServiciuNumerotare` peste `IActNumerotat`.
- **Migrație** (tabele noi + renames). Aplic pe DB dev (`dotnet ef database update` — manual în dev).
- Teste: numerotare pe registrul de dispoziții (lacune, nr. luat, concurență paritar `TesteNumerotareHcl`).

**Pas 2 — Controller lifecycle + conținut + semnare + variantă semnată + invalidare**
- `DispozitiiController`: `Creeaza` (tip+titlu+dataEmitere → scaffold conținut + derivă emitent/secretar
  din funcții istorice), `EditeazaContinut`/`Regenereaza`, `AtribuieNumar`, `Semneaza` (gardă
  completitudine semnatari), variantă semnată (POST/GET/DELETE `Semnat`, freeze pe `AIntratInCircuit`
  via **helper comun extras** — rule-of-three), `Invalidare`/`AnuleazaInvalidare`, `Delete` (matricea de
  gărzi ca HCL). `GeneratorDispozitie` (type-aware).
- **CRITIC — Mentenanță:** adaug `Dispozitii.CaleStocareSemnat` la scanul de orfani
  (`MentenantaController`), lângă Documente/PV/HCL. Fără asta, cleanup-ul șterge fizic PDF-urile semnate.
- Teste: ciclul Creeaza→…→Semnat, gărzi, invalidare cu Altul, delete.

**Pas 3 — Publicare (Normativ) + MOL + latch + audit + comunicare prefect + PDF**
- Publicare portal + MOL **doar pentru Normativ** (Individual → 409/400 la încercare de publicare).
  Reutilizez integral mecanica din S57: `AIntratInCircuit` la publicare MOL, „Anulează MOL" =
  metadată + motiv + audit (`IstoricActiuneAct`), gardă comunicare la prefect.
- **Comunicare la prefect pentru dispoziții:** generalizez/paralelez `ComunicareHclPrefect`
  (decizie la pas, după întrebarea de domeniu #3).
- **PDF: extrag `GeneratorPdfAct` (bază comună)** din `GeneratorPdfProcesVerbal` + `GeneratorPdfHcl`,
  apoi `GeneratorPdfDispozitie` o folosește (rule-of-three).
- Teste: publicare blocată pe Individual, fluxul MOL + latch + anulare (paritar `TesteAnulareMolHcl`).

**Pas 4 — Bonus: dispoziția de convocare (opțional, posibil sesiune separată)**
- La trimiterea convocării (`ConvocareController`), auto-generez o `Dispozitie` (tip de confirmat),
  numerotată, legată de ședință/convocare. Integrare — o ținem la final, izolată.

## Ce NU intră în Faza 4 (limite de scop)
- **Portalul public** pentru dispoziții normative = **Faza 9** (rute publice noi + decizia SSR).
  Faza 4 face doar starea internă de publicare (EstePublicat + MOL), nu endpoint-urile publice.
- **Relații între dispoziții** (modifică/abrogă) — necerute explicit în roadmap pentru Faza 4; le
  amânăm (sau le tratăm odată cu erata, Faza 7).
- **Erata pe dispoziție** = Faza 7 (pattern unificat PV/HCL/Dispoziție).

## Filozofie de implementare (din planul Fazei 3, încă validă)
Planul e ghid, nu script. La fiecare piesă: citesc controllerul/serviciul de paritate HCL,
identific pattern-ul, adaptez la Dispoziție. Verific compilarea + testele după fiecare piesă, nu doar
la final. Generalizările (numerotare, audit, PDF) le fac cu testele HCL existente ca plasă de siguranță.
