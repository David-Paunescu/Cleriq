Plan S51 — Faza 3 Modul A: PDF generat + PDF semnat + Portal public HCL
Context de pornire (verificat)
S49 a livrat stratul de date (4 entități, 8 enum-uri, migrația AddModulHCL, filtered unique + check constraints, cascade, 4 servicii bază). S50 a livrat controllerele HCL (HclController cu 14 endpoint-uri + SemnatariHcl + Comunicari + Registru + Relatii + Dashboard + extinderi Sedinte/Puncte/Documente), cu 3 fix-uri față de planul original: timezone la numerotare, matricea DELETE ca gărzi ordonate cu early-return, precondiția >= Convocata && != Anulata.
Confirmat prin SQL pe baza de dev: cele 5 câmpuri signed (CaleStocareSemnat, NumeFisierSemnat, MarimeSemnat, HashSha256Semnat, DataIncarcareSemnat) există deja în tabela Hcluri — au fost adăugate în S49, migrația aplicată. Nu e nevoie de migrație nouă în S51. S51 e pur cod de aplicație; schema DB rămâne neatinsă (ca S50).
Stare teste: 160 verzi (din S48). Controllerele HCL (S50) și ce livrăm în S51 nu sunt încă acoperite — asta e S52. Până atunci, bug-urile apar doar la smoke test.
Ordinea sesiunii
Pas 1 (generator PDF) → Pas 2 (PDF semnat) → Pas 3 (portal public) → Pas 4 (smoke test, la final).
Dependență reală: Pas 1 înaintea Pas 3 (endpoint-ul public PDF folosește IGeneratorPdfHcl ca fallback). Smoke test-ul la final exercitează tot modulul într-o singură secvență rulabilă (inclusiv download PDF, upload semnat, re-download, acces public). Piesele 1–3 nu depind de corectitudinea lanțului S50, deci un bug acolo ar fi prins oricum înainte de S52. Opțional, dacă vrei liniște: o rulare manuală rapidă a lanțului core (până la Semneaza) înainte de Pas 1 e ieftină.

Pas 1 — Generator PDF HCL
Creez Cleriq/Services/IGeneratorPdfHcl.cs + GeneratorPdfHcl.cs, adaptare ~1:1 din GeneratorPdfProcesVerbal (pe care îl am). Aceasta e duplicarea de ~150 linii acceptată conștient (rule-of-three: refactor la o bază comună abia la al 3-lea generator — dispozițiile din Faza 4).
Interfața:
```csharp
public interface IGeneratorPdfHcl
{
    byte[] Genereaza(Hcl hcl, Institutie institutie);
}
```
Diferențe față de PV:

Sursa de conținut: randează hcl.Continut (Markdown deja generat de GeneratorHcl), exact ca PV randează pv.Continut. NU regenerează conținutul.
Watermark cu 3 stări: INVALIDAT dacă hcl.DataInvalidare != null → altfel DRAFT dacă hcl.Status != Semnat → altfel fără watermark. (PV are doar DRAFT/fără.)
Header instituție identic cu PV.
Generatorul ia DOAR (Hcl, Institutie) — randează un string, nu are nevoie de navigări populate (spre deosebire de GeneratorHcl.GenereazaContinut). Încărcare simplă, fără Include.

DI în Program.cs: `builder.Services.AddScoped<IGeneratorPdfHcl, GeneratorPdfHcl>();` (licența QuestPDF Community e deja setată).
Endpoint în HclController (injectez IGeneratorPdfHcl în constructor):
```csharp
// GET /api/Hcl/{id}/Pdf  [orice rol auth]
var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
if (hcl is null) return NotFound();
var institutie = await _context.Institutii.FirstAsync();  // filtrul global → tenant curent
var pdf = _generatorPdf.Genereaza(hcl, institutie);
var nume = hcl.Numar != null ? $"hcl-{hcl.Numar}-{hcl.AnNumerotare}.pdf" : $"hcl-draft-{hcl.Id}.pdf";
return File(pdf, "application/pdf", nume);
```
Important: endpoint-ul intern /Pdf e mereu generat on-the-fly, fără prioritate-semnat — paritar ProcesVerbalController.ObtinePdf (care generează din Markdown; varianta semnată are propriul GET /Semnat). Prioritatea-semnat trăiește DOAR pe endpoint-ul public (Pas 3).
Fișier necesar: HclController.cs (îl am din discuția de planificare).

Pas 2 — Variantă PDF semnat (Nivel 1, paritar PV)
Pas B — Endpoint-uri în HclController (injectez IStocareDocumente în constructor)
Extras din ProcesVerbalController Nivel 1 (pe care îl am). Stocare în IStocareDocumente reutilizat (NU a 3-a stocare). Replace lasă fișierul vechi orfan post-commit (măturat de mentenanță).
Decizia de gardă (varianta B, rafinată): garda DataPublicareMol == null se aplică pe replace și DELETE, dar prima atașare e permisă mereu cât Status == Semnat, chiar și post-MOL.
Raționament: semnătura legală e deja atestată la Semneaza (acolo se consfințesc semnatarii); PDF-ul semnat e dovada ei, nu actul. Prima atașare post-MOL e upgrade benign (cetățeanul vede generat → semnat, conținut identic, ambele fără watermark la Semnat). Replace-ul/DELETE-ul dovezii deja publicate e tamperingul real. Divergența față de PV (HCL permite prima atașare post-sigiliu, PV nu) e intenționată — ancorele diferă (PV DataAprobare; HCL DataPublicareMol).
POST /api/Hcl/{id}/Semnat  [Admin,Secretar]  [RequestSizeLimit(MarimeMaxima)]:
  - hcl există                                          else 404
  - Status == Semnat                                    else 409
  - dacă CaleStocareSemnat != null (replace):
        DataPublicareMol == null                        else 409
  - fișier prezent + .pdf (whitelist, NU header client) else 400
  - salvează în IStocareDocumente, setează cele 5 câmpuri
  - post-commit: la replace, șterge fișierul vechi (try/catch, orfan acceptabil)

GET /api/Hcl/{id}/Semnat:
  - hcl + CaleStocareSemnat != null                     else 404
  - download cu Content-Disposition (NumeFisierSemnat)

DELETE /api/Hcl/{id}/Semnat  [Admin]:
  - hcl + CaleStocareSemnat != null                     else 404
  - DataPublicareMol == null                            else 409
  - nullează cele 5 câmpuri
Gap rezidual acceptat pentru pilot: fereastra EstePublicat = true dar DataPublicareMol = null (HCL live pe portal, PDF încă înlocuibil). Tolerabil — operatorul publică pe portal și înregistrează MOL în același pas de workflow; audit-ul prinde orice swap. Se strânge on-demand la EstePublicat || DataPublicareMol != null.
Nota de DTO: HclDto (slim) nu poartă câmpurile signed. Pentru a reflecta rezultatul upload-ului, POST /Semnat întoarce HclDetaliiDto (re-query cu Include-uri, paritar cum PV întoarce DTO-ul complet cu signed) sau un DTO focusat — se decide la implementare, nu schimbă planul.
Pas C (CRITIC) — Mentenanță
Adaug Hcluri.CaleStocareSemnat în dicționarul de scan din MentenantaController.CalculeazaOrfaniDocumenteAsync, alături de Documente.CaleStocare și ProceseVerbale.CaleStocareSemnat (aceeași stocare fizică partajată). Fără asta, cleanup-ul șterge fizic PDF-urile HCL semnate ca orfane (le-ar clasifica FaraRandInDb). Pattern critic din backend.md.
Fișiere necesare: HclDto.cs (pentru mapper-ul de răspuns). HclController.cs + MentenantaController.cs le am.

Pas 3 — Portal public HCL
PublicHclController, rute /public/{slug}/hcl..., fără [Authorize] (SlugTenantMiddleware setează tenant-ul). Paritar PublicSedinteController + PublicProcesVerbalController + PublicDocumenteController (toate le am).
Vizibilitate: EstePublicat == true AND Status >= Numerotat. HCL invalidat rămâne vizibil cu badge (EstePublicat păstrează valoarea — vizibilitatea persistă, decizie juridică).
Endpoint-uri:
GET /public/{slug}/hcl — listă paginată (page/size), ordine DataAdoptare desc, doar metadate de bază + flag invalidat, fără Continut.
GET /public/{slug}/hcl/{id} — detalii.

Expune: numar/an/titlu/DataAdoptare/TipHcl, Continut, vot snapshot (Pentru/Împotrivă/Abținere + TipMajoritate), semnatari (nume + rol — funcționari publici pe act oficial), MotivLipsaSemnaturaPresedinte, DataIntrareInVigoare, anexe publice (Documente cu EstePublic=true, etichetă din TipDocumentHcl), relații (RelatieHcl), status + detalii invalidare (motiv + RefInvalidare + dată), DataPublicareMol.
NU expune: comunicări prefect + registru, observații interne de pe comunicări, ID-uri audit (InvalidatDe, PublicataDe), internele de stocare (CaleStocareSemnat/HashSha256Semnat/NumeFisierSemnat).

GET /public/{slug}/hcl/{id}/pdf — varianta semnată prioritară (nume canonic hcl-{numar}-{an}-semnat.pdf, NU numele încărcat) + fallback la PDF generat (IGeneratorPdfHcl) când nu există semnat. Lipsă pe disk → warning în log + degradare grațioasă (cetățeanul primește mereu un PDF).
Cache Redis PDF — rafinare importantă (corectează nota lui S50):
Spre deosebire de PV (care publică doar Finalizat = imutabil), HCL poate fi public la Numerotat, stare în care Continut e încă editabil. Dacă HCL a fost publicat la Numerotat și i s-a cerut PDF-ul, sub cheia {hclId} stă cache-uit un PDF cu watermark DRAFT — iar la Semnat aceeași cheie ar servi „DRAFT" pe un act final publicat în MOL. Pe portal de transparență, asta induce în eroare.
Fix (fără invalidare explicită de chei):
```
if Status == Semnat:
    cheie = DataInvalidare.HasValue ? "hcl:pdf:{id}:inv" : "hcl:pdf:{id}"
    citește din cache; pe miss generează + scrie  (TTL 1h)
else:  // Numerotat — singura altă stare public-vizibilă
    generează proaspăt, FĂRĂ cache
```
La Semnat conținutul e înghețat → cache mereu valid (port fidel al PV care cache-uiește doar imutabilul, NU „cache always"). La Numerotat: stare rară, conținut mutabil, rate limiter-ul pe /public/* plafonează CPU-ul. Cheia :inv acoperă HCL invalidat (watermark INVALIDAT). Verificările de vizibilitate rămân per-request (HCL depublicat/șters nu mai servește PDF imediat). Try/catch defensiv (Redis jos → generare directă, NU 500).
Anexe download — branch nou în PublicDocumenteController.Descarca:
Anexele HCL au SedintaId și PunctId ambele null → codul actual ar da NPE pe doc.PunctId!.Value. Deci nu e doar eleganță, e bug-fix. Adaug branch:
doc.HclId != null  → vizibilitate HCL (EstePublicat && Status >= Numerotat)
altfel             → vizibilitatea pe sedinta/punct (logica existentă)
Reutilizează URL-ul /public/{slug}/documente/{id} pe care frontend-ul îl știe deja; ține logica de download într-un singur loc.
DTOs noi: PublicHclDto + PublicHclDetaliiDto (slim, fără internals — paritar PublicDocumentDto).
Fișiere necesare: HclDto.cs, SemnatarHclDto.cs, RelatieHclDto.cs (pentru a modela versiunile publice slim). PublicDocumenteController.cs îl am.

Pas 4 — Smoke test complet
Secțiune FAZA 3 în Cleriq/Cleriq.http. Lanț end-to-end:
login admin.slobozia → POST /Sedinte → POST /Sedinte/{id}/PresedinteSedinta {consilierId:1} → POST /Sedinte/{id}/Puncte (tip ProiectHCL=1, tipMajoritate, tipVot, necesitaVot:true) → convocare + Incepe (status InDesfasurare) → POST prezență (consilieri Prezent) → POST vot → POST {punctId}/Inchide (→ Rezultat Adoptat) → POST /Hcl/Genereaza → AtribuieNumar → Semneaza → Publicare → PublicareMol → Comunicare prefect → GET /Hcl/{id}/Pdf (download) → POST /Hcl/{id}/Semnat (upload) → GET /Hcl/{id}/Semnat (re-download, confirmă prioritatea) → acces public (listă + detalii + pdf).
CRITIC: punctul trebuie Adoptat înainte de Genereaza (lanțul de vot complet).
Teste gărzi de inclus:

generare HCL fără PresedinteSedinta setat → 400
AtribuieNumar nr. 5 când sugestia e 1 → 409 cu lacune [1,2,3,4]
re-AtribuieNumar confirmaCuLacune=true → 200
DELETE HCL Semnat → 409
DELETE Sedinta cu HCL Numerotat+ → 409
NOU (garda Pas 2): DELETE semnat după PublicareMol → 409; opțional al doilea upload (replace) după PublicareMol → 409. (Cu varianta B, prima încărcare post-MOL din lanțul principal trece — ordinea rămâne validă fără reordonare.)

Seed Slobozia (de verificat în SeedDevelopment.cs înainte de a scrie testul): admin.slobozia@cleriq.ro / AdminSlobozia1!; consilieri Ion=1 (Activ, bun ca președinte), Vasile=2, TestFiltru=3; Primar persoana Andrei Mihalache; Secretar UAT persoana Maria Ionescu (mandat activ din 2024-10-27 → CinESecretarulUatLa pe o dată din 2026 întoarce non-null).
Fișiere necesare: Cleriq.http, SeedDevelopment.cs (le am).

După S51
S52 = teste xUnit (~54 noi: TesteHcl, TesteNumerotareHcl, TesteSemnatariHcl, TesteComunicareHclPrefect, TesteCalculatorZileLucratoare, TesteAlerteT3, TesteRelatiiHcl, TesteInvalidareHcl, TesteAnexeHcl, TestePublicHcl). Suprafața API e stabilă după S51 — orice modificare ulterioară va fi prinsă de teste.
Notă de igienă (nu în S51): backend.md rămâne de actualizat la o trecere de consolidare — controllere noi din S50, garda DataPublicareMol pe semnat, generator PDF HCL, branch HclId în PublicDocumente, listă migrații. E snapshot consolidat; rezumatele S46–S51 sunt delta-urile.

Rezumat fișiere necesare, pe pași

Pas 1: HclController.cs ✓ (îl am)
Pas 2: HclDto.cs, + HclController.cs ✓, MentenantaController.cs ✓
Pas 3: HclDto.cs, SemnatarHclDto.cs, RelatieHclDto.cs, PublicDocumenteController.cs ✓
Pas 4: Cleriq.http ✓, SeedDevelopment.cs ✓

Decizii de settled rămase, ambele rezolvate: gardă PDF semnat = varianta B; DataPublicareMol public = da. Nimic deschis.
Filozofie de implementare (din planul original, încă validă): planul e ghid, nu script. La fiecare piesă, citesc controllerul/serviciul de paritate (PV pentru PDF + semnat, PublicSedinte/PublicProcesVerbal pentru portal), identific pattern-urile, apoi adaptez la HCL. Verific compilarea după fiecare piesă, nu doar la final.
