Rezumat sesiune S50 — Backend Faza 3 Modul A: controllere HCL

## Context
S49 a livrat infrastructura de date completă: 4 entități (Hcl, SemnatarHcl, ComunicareHclPrefect, RelatieHcl), 8 enum-uri + etichete, migrația AddModulHCL, 7 filtered unique, 3 check constraints noi, cascade soft-delete extins, 4 servicii (CalculatorZileLucratoare, ServiciuNumerotareHcl, ServiciuComunicareHclPrefect, GeneratorHcl) — toate în DI.

S50 = exclusiv controllere. **Schema DB NU s-a modificat** în această sesiune.

Stare teste: rămâne 160 verzi (din S48). Controllerele HCL din S50 NU sunt încă acoperite de teste — asta e S52. Până atunci, orice bug în S50 apare doar la smoke test.

## Revizuire plan — 3 fix-uri integrate
Înainte de implementare am revizuit plan_faza3_partea2_backend.md și am corectat 3 probleme:

**FIX #1 — timezone la numerotare.** AnNumerotare și lookup-ul Secretar UAT la generare folosesc data LOCALĂ a adoptării (prin `.LaFusOrar(institutie.FusOrar)` din ExtensiiData.cs), NU UTC. Altfel o ședință seara târziu (ex. 31.12 23:00 UTC) putea fi numerotată pe alt an. Aplicat în două locuri:
- HclController.Genereaza (lookup secretar la data locală)
- ServiciuNumerotareHcl.AtribuieAsync (derivarea anului din data locală — am editat serviciul deja livrat în S49, adăugând `using Cleriq.Helpers;` + încărcarea FusOrar)

**FIX #2 — matricea DELETE HCL = 4 gărzi ordonate, NU switch.** Cele „6 stări" din plan NU sunt mutual exclusive (DataInvalidare, EstePublicat, Comunicari active sunt ortogonale față de Status). Implementarea corectă e early-return în ordine:
1. Comunicari active → 409
2. DataInvalidare != null → OK (soft-delete; override — HCL anulat de instanță e legitim de șters)
3. Status == Semnat → 409
4. EstePublicat → 409
5. altfel (Draft / Numerotat fără comunicări) → OK

**FIX #3 — precondiție `>= Convocata` admitea Anulata.** StatusSedinta.Anulata (5) > Convocata (2), deci `>= Convocata` lăsa să treacă ședințe anulate. Corectat la `Status >= Convocata && Status != Anulata` (consistent cu PublicSedinteController).

## Ce s-a livrat (pas cu pas, toate compilează)

**Pas 1 — Extindere SedinteController + SedintaDto.**
- DTO nou `SetarePresedinteSedintaDto(int ConsilierId)`. SedintaDto extins la coadă cu `int? PresedinteSedintaConsilierId, string? PresedinteSedintaNumeComplet`.
- POST /api/Sedinte/{id}/PresedinteSedinta [Admin,Secretar] — validează consilier există + Activ în tenant, setează nav PresedinteSedinta.
- DELETE /api/Sedinte/{id}/PresedinteSedinta [Admin] — nullează FK.
- Lista/Detalii fac `.Include(s => s.PresedinteSedinta)` + mapează în memorie.
- Gardă DELETE Sedinta: 409 dacă există HCL cu Status >= Numerotat generat din punctele ședinței.

**Pas 2 — 4 fișiere DTO HCL** (fiecare cu `using Cleriq.Models;`):
- HclDto.cs: CreareHclDto, EditareContinutHclDto, AtribuireNumarHclDto, InvalidareHclDto, PublicareHclDto, PublicareMolDto, HclDto (listă), HclDetaliiDto (complet), DocumentHclDto (slim), MotivLipsaPresedinteDto.
- SemnatarHclDto.cs: AdaugareSemnatarDto, SemnatarHclDto.
- ComunicareHclPrefectDto.cs: CreareComunicareDto, ActualizareComunicareDto, ComunicareHclPrefectDto, RegistruComunicareDto.
- RelatieHclDto.cs: CreareRelatieDto, RelatieHclDto (poartă ambele capete sursă+țintă).
- HclUrgentDto deja există în IServiciuComunicareHclPrefect.cs — reutilizat, NU recreat.

**Pas 3 — HclController** (api/Hcl, [Authorize]). 14 endpoint-uri:
GET / (filtre an/status/tipHcl, paginat skip/take default 50, ordine DataAdoptare desc); GET /{id} (HclDetaliiDto cu Include pe Semnatari+Persoana/Consilier, Documente, RelatiiSursa.HclTinta, RelatiiTinta.HclSursa, Comunicari); POST /Genereaza (5 precondiții → snapshot vot din punct.Voturi → creare Hcl + 2 Semnatari → SaveChanges → reload cu Include → GenereazaContinut → SaveChanges); PUT /{id}/Continut (gardă !=Semnat); POST /{id}/RegenereazaContinut (gardă !=Semnat); POST /{id}/AtribuieNumar (switch pe TipRezultatAtribuire: Succes→Ok, HclInexistent→404, NumarInvalid→400, StareInvalidaHcl→409, LacuneNeconfirmate→409 {mesaj,lacune}, NumarLuat→409 {mesaj,sugestieAlternativa}); POST /{id}/Semneaza (gardă Numerotat + validare semnatari completi: exact 1 SecretarUat ȘI (1 Presedinte SAU ≥2 Art140 + motiv setat)); POST /{id}/Invalidare; DELETE /{id}/Invalidare [Admin]; PUT /{id}/Publicare; PUT /{id}/PublicareMol; DELETE /{id}/PublicareMol [Admin]; DELETE /{id} [Admin] (matricea 4 gărzi). Mapper-e private. Controllerul + serviciul împart același DbContext scoped, deci după AtribuieAsync entitatea tracked reflectă deja schimbarea.

**Pas 4 — SemnatariHclController + endpoint MotivLipsaPresedinte.**
- **Gap de plan descoperit:** planul nu avea endpoint pentru setarea Hcl.MotivLipsaSemnaturaPresedinte, dar fluxul SemnatarAlternativArt140 îl cere (POST alternativ → 400 dacă motivul e null). Am adăugat PUT /api/Hcl/{id}/MotivLipsaPresedinte [Admin,Secretar] (gardă !=Semnat, obligatoriu, max 500) în HclController + MotivLipsaPresedinteDto.
- SemnatariHclController (api/Hcl/{hclId}/Semnatari): GET / (ordonat OrdineAfisare); POST / (XOR Persoana/Consilier, validare rol↔FK, pentru Art140: motiv setat + consilier prezent la Hcl.PunctOrdineZi.Sedinta (Prezent/OnlinePrezent), filtered unique Presedinte/Secretar, conflict cross-rol pe același consilier); DELETE /{semnatarId} (gardă !=Semnat, auto-clear motiv dacă scoți ultimul Art140, în aceeași tranzacție).

**Pas 5 — ComunicariHclPrefectController + RegistruComunicariPrefectController.**
- Comunicari (api/Hcl/{hclId}/Comunicari): GET / (ordine NumarOrdineInRegistru desc); POST / (gardă Status>=Numerotat, **retry loop max 3 pe violare unique 2601/2627** pentru race-ul pe NumarOrdineInRegistru — detach + retry, 409 la epuizare); PUT /{comunicareId} (imutabile post-creare: HclId/NumarOrdine/AnRegistru/DataTrimiteri/CanalTransmitere); DELETE /{comunicareId} [Admin].
- RegistruComunicariPrefect (api/RegistruComunicariPrefect) [Admin,Secretar]: GET / (an default anul curent, page/size, Include Hcl, ordine NumarOrdineInRegistru asc).

**Pas 6 — RelatiiHclController + HclDashboardController.**
- Relatii (api/Hcl/{hclId}/Relatii): GET / (o singură interogare `HclSursaId==hclId || HclTintaId==hclId`, partiționare sursă/țintă în memorie); POST / (XOR țintă internă/text extern, text max 300, auto-referință → 400, duplicat (sursa,tinta,tip) upfront + catch unique); DELETE /{relatieId} (**doar din capătul sursă**, `r.HclSursaId == hclId`).
- Dashboard (api/Hcl/UrgentDeComunicat) [Admin,Secretar]: GET /UrgentDeComunicat?prag=3 → ObtineHclUrgentDeComunicatAsync(InstitutieIdCurenta, prag). Rută literală bate {id} din HclController (fără eroare de ambiguitate la pornire).

**Pas 7 — Extindere DocumenteController + gardă PuncteOrdineZiController.**
- DocumentDto extins la coadă (HclId, TipDocumentHcl, NumarOrdinAnexa); ActualizareDocumentDto extins (TipDocumentHcl, NumarOrdinAnexa).
- Lista acceptă hclId (verificare ternară exact-unul-din-3). Incarca acceptă hclId/tipDocumentHcl/numarOrdinAnexa → forțează TipDocument=Altele când HclId setat, anexă cere NumarOrdinAnexa, duplicat anexă upfront (înainte de salvarea fișierului) + catch unique. Actualizeaza: pentru documente HCL gardă NumarOrdinAnexa imutabil când Status=Semnat + revalidare anexă, forțează TipDocument=Altele; documente non-HCL păstrează comportamentul clasic.
- PuncteOrdineZiController.Sterge: gardă 409 dacă există HCL Status>=Numerotat din acest punct.

## Decizii noi luate în sesiune
- **DELETE relație doar din sursă.** Relațiile se creează pe HCL-ul sursă (POST /api/Hcl/{sursaId}/Relatii), deci se administrează de acolo. Din capătul țintă vezi relația dar nu o poți șterge. Frontend-ul afișează butonul de ștergere doar pe relatiiSursa.
- **Retry loop pe registru comunicări.** SugereazaNumarOrdineRegistruAsync + insert poate avea race (două comunicări simultane pe același număr de ordine). Catch pe 2601/2627 + detach + retry (max 3) + 409 la epuizare. Numerele de registru sunt „arse" (counting prin IgnoreQueryFilters — corect pentru audit cronologic).
- **Duplicat anexă: upfront + catch.** Verificare AnyAsync înainte de salvarea fizică a fișierului (ca să nu lași orfani la cazul comun secvențial) + catch pe filtered unique pentru race-ul concurent.

## Probleme deschise (de tratat în S51)
1. **PDF generator HCL — NEIMPLEMENTAT.** Step 8 din S50 (IGeneratorPdfHcl + GeneratorPdfHcl + DI + GET /{id}/Pdf) a rămas nefăcut din lipsă de context. Prioritate 1 în S51.
2. **Smoke test — NEIMPLEMENTAT.** Step 9 din S50 (flux complet în Cleriq.http). Prioritate 2 în S51.
3. **Câmpurile signed-PDF NU sunt pe Hcl.** S49 nu a adăugat CaleStocareSemnat/NumeFisierSemnat/etc. pe Hcl. Feature-ul signed PDF (S51) cere migrație nouă AddHclSemnat înainte de endpoints.
4. **Duplicare generator PDF acceptată.** GeneratorPdfHcl va duplica ~150 linii de randare Markdown→PDF din GeneratorPdfProcesVerbal. Regula rule-of-three: refactor la o bază comună abia la a 3-a stocare/generator (dispoziții, Faza 4).

## Schimbări față de backend.md (de adăugat la următoarea actualizare)
- 6 controllere noi: HclController (14 endpoint-uri), SemnatariHclController, ComunicariHclPrefectController, RegistruComunicariPrefectController, RelatiiHclController, HclDashboardController.
- SedinteController extins: PresedinteSedinta (POST/DELETE) + gardă DELETE Sedinta (HCL Numerotat+) + DTO cu PresedinteSedinta.
- PuncteOrdineZiController extins: gardă DELETE (HCL Numerotat+).
- DocumenteController extins: anexe HCL (HclId/TipDocumentHcl/NumarOrdinAnexa), forțare TipDocument=Altele când HclId setat.
- Pattern nou: **matricea DELETE multi-stare = gărzi ordonate cu early-return, NU switch** (stările pot coexista). Aplicat la DELETE HCL; reutilizabil la dispoziții/erată.
- Fix timezone: AnNumerotare derivat din data LOCALĂ a adoptării (`.LaFusOrar`).
- Convenție rute: segment literal bate parametru `{id}` (HclDashboardController coexistă cu HclController pe api/Hcl).

---

# Pentru S51 — plan detaliat

S51 are 3 părți. Ordinea: finalizează întâi ce a rămas din S50 (PDF generat + smoke test), apoi varianta PDF semnată, apoi portalul public.

## Partea 1 — Finalizare S50

### Step 8 — Generator PDF HCL
Creează IGeneratorPdfHcl.cs + GeneratorPdfHcl.cs, paritar cu PV (Markdig + QuestPDF). Interfața:
```csharp
public interface IGeneratorPdfHcl
{
    byte[] Genereaza(Hcl hcl, Institutie institutie);
}
```
Diferențe față de PV:
- Randează `hcl.Continut` (Markdown deja generat de GeneratorHcl), exact ca PV randează pv.Continut. NU regenerează conținutul.
- Watermark: `INVALIDAT` dacă `hcl.DataInvalidare != null`; altfel `DRAFT` dacă `hcl.Status != Semnat`; altfel fără watermark.
- Header instituție identic cu PV.
- IMPORTANT: generatorul randează un STRING (hcl.Continut), deci NU are nevoie de navigări populate (spre deosebire de GeneratorHcl.GenereazaContinut). Doar `hcl` (cu Continut, Status, DataInvalidare, Numar, AnNumerotare) + institutie. Încărcare simplă, fără Include.

DI în Program.cs: `builder.Services.AddScoped<IGeneratorPdfHcl, GeneratorPdfHcl>();` (QuestPDF Community License e deja setat în Program.cs).

Endpoint în HclController (injectează IGeneratorPdfHcl în constructor):
```csharp
// GET /api/Hcl/{id}/Pdf  [orice rol auth]
var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
if (hcl is null) return NotFound();
var institutie = await _context.Institutii.FirstAsync();   // filtrul global o limitează la tenant curent
var pdf = _generatorPdf.Genereaza(hcl, institutie);
var nume = hcl.Numar != null ? $"hcl-{hcl.Numar}-{hcl.AnNumerotare}.pdf" : $"hcl-draft-{hcl.Id}.pdf";
return File(pdf, "application/pdf", nume);
```

### Step 9 — Smoke test
Adaugă în Cleriq/Cleriq.http secțiunea FAZA 3 (skeletul există deja în plan_faza3_partea2_backend.md, dar îi lipsește lanțul de vot). CRITIC: punctul trebuie să fie Adoptat înainte de Genereaza. Lanțul complet:
login admin.slobozia → POST /Sedinte → POST /Sedinte/{id}/PresedinteSedinta {consilierId:1} → POST /Sedinte/{id}/Puncte (tip ProiectHCL=1, tipMajoritate, tipVot, necesitaVot:true) → convocare + start ședință (status InDesfasurare) → POST prezenta (consilieri Prezent) → POST vot → POST InchideVot (→ Rezultat Adoptat) → POST /Hcl/Genereaza → AtribuieNumar → Semneaza → Publicare → Comunicare prefect → teste gărzi.

Teste gărzi de inclus: generare fără PresedinteSedinta → 400; AtribuieNumar nr.5 când sugestia e 1 → 409 cu lacune [1,2,3,4]; re-AtribuieNumar confirmaCuLacune=true → 200; DELETE HCL Semnat → 409; DELETE Sedinta cu HCL Numerotat+ → 409.

Seed Slobozia: admin.slobozia@cleriq.ro / AdminSlobozia1!; consilieri Ion=1 (Activ, bun ca președinte), Vasile=2, TestFiltru=3; Primar persoana Andrei Mihalache; Secretar UAT persoana Maria Ionescu (mandat activ din 2024-10-27 → CinESecretarulUatLa pe o dată din 2026 întoarce non-null). Verifică în SeedDevelopment.cs id-urile exacte înainte de a scrie testul.

## Partea 2 — Variantă PDF semnat (Nivel 1, paritar PV)

### Pas A — Câmpuri pe Hcl + migrație
Verifică Hcl.cs. Dacă NU are câmpurile signed (probabil nu — S49 nu le-a adăugat), adaugă:
```csharp
public string? CaleStocareSemnat { get; set; }
public string? NumeFisierSemnat { get; set; }
public long? MarimeSemnat { get; set; }
public string? HashSha256Semnat { get; set; }
public DateTime? DataIncarcareSemnat { get; set; }
```
Migrație: `Add-Migration AddHclSemnat` / `Update-Database`. Adaugă AddHclSemnat la lista migrații cumulative din backend.md. (Reamintește utilizatorului: discutăm impactul pe teste înainte — dar aici sunt câmpuri nullable noi, fără spargere de teste existente.)

### Pas B — Endpoint-uri în HclController (extras din ProcesVerbalController Nivel 1)
- POST /api/Hcl/{id}/Semnat [Admin,Secretar] — upload PDF semnat extern. Gardă: Status = Semnat (echivalentul „Finalizat" la PV — actul redacțional e finalizat). Stocare în IStocareDocumente REFOLOSIT (NU a 3-a stocare). Validare PDF ([RequestSizeLimit] + whitelist .pdf). Replace = upload din nou; fișierul vechi devine orfan acceptabil (măturat de mentenanță).
- GET /api/Hcl/{id}/Semnat — download cu Content-Disposition.
- DELETE /api/Hcl/{id}/Semnat [Admin] — nullează cele 5 câmpuri (mai destructivă decât replace → Admin-only), portalul retrogradează la PDF generat.

Decizie de luat în S51: PV are garda D8 (signed intangibil după DataAprobare). HCL NU are axă „aprobare" separată — semnatul ESTE actul final. Recomandare: NU adăuga o gardă echivalentă; păstrează simplu (doar Status=Semnat ca precondiție upload). Dacă piloții cer „blocare după publicare MOL", se adaugă ulterior.

### Pas C — Mentenanță (CRITIC)
Pattern obligatoriu din backend.md: adaugă `Hcluri.CaleStocareSemnat` la dicționarele de scan din MentenantaController. Altfel cleanup-ul șterge fizic PDF-urile HCL semnate ca orfane (aceeași stocare partajată ca Documente + ProceseVerbale). Fără asta = pierdere de acte semnate.

## Partea 3 — Portal public HCL (PublicHclController)

Paritar PublicSedinteController + PublicProcesVerbalController. Rute /public/{slug}/hcl... (fără [Authorize]; SlugTenantMiddleware setează tenant-ul).
- **Vizibilitate:** `EstePublicat == true AND Status >= Numerotat`. HCL invalidat rămâne vizibil cu badge (EstePublicat păstrează valoarea — vizibilitatea PERSISTĂ, ca decizie juridică).
- GET /public/{slug}/hcl — listă (paginat, fără Continut, doar metadate de bază + flag invalidat).
- GET /public/{slug}/hcl/{id} — detalii + Continut + anexe publice + relații (decide ce e public). NU expune comunicările prefect (audit intern). Semnatarii: probabil OK de expus (transparență), decide în sesiune.
- GET /public/{slug}/hcl/{id}/pdf — varianta semnată are PRIORITATE (nume canonic `hcl-{numar}-{an}-semnat.pdf`, NU numele fișierului încărcat), fallback la PDF generat (IGeneratorPdfHcl) când nu există semnat. Lipsă pe disk → warning în log + degradare grațioasă (cetățeanul primește mereu un PDF).
- **Cache Redis PDF:** cheie `cleriq:hcl:pdf:{hclId}` TTL 1h (paritar `cleriq:pv:pdf:{sedintaId}`). Cache-uiește DOAR generarea; verificările de vizibilitate rămân per-request (HCL depublicat/șters nu mai servește PDF imediat). Try/catch defensiv (Redis jos → generare directă, NU 500 spre cetățean).
- Anexe publice: documentele HCL cu EstePublic=true (toggle prin PUT /Documente/{id}/Vizibilitate existent). Viewer afișează eticheta din TipDocumentHcl când HclId IS NOT NULL.

## După S51 → S52 = teste xUnit
~54 teste noi (listă detaliată în plan_faza3_partea2_backend.md secțiunea „Pentru sesiunile S51-S52"): TesteHcl, TesteNumerotareHcl, TesteSemnatariHcl, TesteComunicareHclPrefect, TesteCalculatorZileLucratoare, TesteAlerteT3, TesteRelatiiHcl, TesteInvalidareHcl, TesteAnexeHcl, TestePublicHcl. Suprafața API e stabilă după S51 — orice modificare ulterioară va fi prinsă de teste.
