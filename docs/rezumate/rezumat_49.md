Rezumat sesiune S49 — Backend Faza 3 Modul A: modele + migrație + servicii bază
Ce s-a livrat
Planificare + 7 rafinări pe plan_faza3_backend.md integrate ca subdecizii blocate:

HclSursaId NOT NULL în RelatieHcl + check constraint XOR pe țintă (HclTinta vs ReferintaActExternText)
3 precondiții explicite la POST /Genereaza (PresedinteSedinta setat, SecretarUat valid la DataAdoptare, Status ședință ≥ Convocata)
TipHcl parametru obligatoriu în DTO la generare, fără default backend (sugestia keyword-matching = UX la frontend)
Status minim Numerotat pentru POST /Comunicari (art. 197 alin. 1 — termenul curge de la adoptare, NU semnare)
Matricea DELETE HCL cu 6 stări + subdecizia critică "numere arse" (paritar slug-uri instituții)
Gardă DELETE pe PunctOrdineZi și Sedinta când există HCL Numerotat+ (extensie pattern D7)
Regulă coexistență TipDocument vs TipDocumentHcl: forțare TipDocument = Altele când HclId setat, TipDocumentHcl devine sursa de adevăr

Implementare:

8 enum-uri noi + etichete în ExtensiiEnumuri.cs
4 entități noi (Hcl, SemnatarHcl, ComunicareHclPrefect, RelatieHcl) + extinderi Document și Sedinta
AppDbContext config: FK Restrict, 7 filtered unique, 3 check constraints noi (CK_Document_AnexaMetadata, CK_RelatieHcl_ExactUnaTinta, CK_SemnatarHcl_ExactUnSubject + CK_SemnatarHcl_FkCorectaPerRol), rescriere CK_Document_ExactUnContext cu 3 contexte, cascade soft-delete extins (Hcl → copii, Persoana/Consilier → SemnatarHcl)
Migrație AddModulHCL aplicată curat
4 servicii: CalculatorZileLucratoare (singleton, Meeus + sărbători art. 139 Codul Muncii), IServiciuNumerotareHcl (3 gărzi + numere arse), IServiciuComunicareHclPrefect (T-10 + dashboard urgent), IGeneratorHcl (Markdown sync in-memory)
DI registrations în Program.cs

Decizii arhitecturale luate în sesiune
Numere arse la nivel serviciu, NU DB constraint. Filtered unique pe (InstitutieId, AnNumerotare, Numar) păstrează [EsteSters] = 0. Verificare explicită AnyAsync(EsteSters = true) în AtribuieAsync blochează refolosirea. Tradeoff: race condition reziduală extrem de rară (două cereri simultane pe același număr ars). Acceptat pentru MVP — dacă apare presiune, migrație ulterioară scoate [EsteSters] = 0 din filter și constraint-ul devine hard.
CalculeazaDataLimitaComunicare sync, NU Task<DateOnly>. Planul cerea async pentru consistență, dar metoda e wrapper pur peste calculator in-memory. Task.FromResult ar fi overhead inutil. Sync e onest.
IGeneratorHcl sync, NU folosește IServiciuFunctiiIstorice. Generator primește Hcl cu navigări populate, lucrează in-memory. Semnatarii sunt entități SemnatarHcl pre-create de controller (care folosește IServiciuFunctiiIstorice pentru SecretarUat la DataAdoptare). Separare clară: controller orchestrează + populează, generator scrie. Mai testabil.
Pattern extension method dual pentru enum-uri snapshot. TipMajoritate e nullable pe PunctOrdineZi (poate să nu existe) dar non-nullable pe Hcl (snapshot imutabil). Adăugat Eticheta(this TipMajoritate t) ca suprasarcină care deleagă la versiunea nullable. Pattern reutilizabil pentru orice viitor enum care apare ca snapshot pe entități post-eveniment.
Bug descoperit și rezolvat
GeneratorHcl apela hcl.TipMajoritate.Eticheta() pe enum non-nullable, iar extension-ul existent era doar pentru TipMajoritate?. Compilatorul propunea fallback la Eticheta(TipSedinta) (cel mai apropiat semantic) — eroare clară de tip. Fix prin suprasarcină nouă care face cast la nullable și deleagă (DRY — etichete într-un singur loc).
Probleme deschise noi (de tratat în S50+)

Refactor IGeneratorPdfProcesVerbal pentru HCL. Generatorul PDF curent e specific PV. Pentru HCL, decidem în S50: (a) suprasarcină generică Markdown + metadate, (b) IGeneratorPdfHcl nou care reutilizează parserul Markdig + QuestPDF intern.
Decizia regenerare conținut HCL la AtribuieNumar. Header Markdown conține _[urmează să fie atribuit]_ cât HCL e Draft. La tranziția Draft → Numerotat, conținutul se regenerează automat (pierdem modificările secretarului) sau secretar ajustează manual? Decidem la implementarea HclController.
Algoritm Meeus expiră 2099. Conversia Julian → Gregorian trece de la +13 la +14 zile după anul 2100. Marcat pentru audit temporal (împreună cu R1 din planul Faza 3 — schimbări legislative pe sărbători).
Performance dashboard HCL urgent. Filtrare T-N în memorie (calculator e in-memory, nu poate rula pe SQL Server). OK pentru 50 HCL/an pe instituție. Pentru scale 500+ HCL/an, ar trebui materializare ZileLucratoare ca tabel DB. Risc post-pilot.
Schemă index-uri IX_SemnatarHcl_PresedinteSedintaActiv / IX_SemnatarHcl_SecretarUatActiv. Acoperă cazul normal (max 1 per rol). SemnatarAlternativArt140 NU are filtered unique — pot fi 2+. Cardinalitatea minimă (≥2 alternativi) e validată în controller, nu DB (greu de exprimat ca check constraint).

Schimbări față de backend.md
De adăugat la următoarea actualizare:

Pattern "filtered unique cu vs fără [EsteSters] = 0": distincție semantică între "soft-deleted poate fi refolosit" (slot-uri) vs "soft-deleted e ars permanent" (numere/identificatori). Cu HCL, semantica e ars; momentan impusă la nivel serviciu, nu DB.
Pattern extension method dual pentru enum-uri snapshot (TipMajoritate non-nullable pe HCL).
Pattern "generator sync in-memory" cu navigări populate de controller (paritar IGeneratorHcl).
Entități noi tenant: Hcl, SemnatarHcl, ComunicareHclPrefect, RelatieHcl — la lista filtered unique + cascade.
Cascade noi în AplicaCascadaSoftDelete: Hcl → SemnatariHcl + ComunicariHclPrefect + Documente, extindere Persoana și Consilier la SemnatariHcl.
MotivLipsaSemnaturaPresedinte pe Hcl (nu pe SemnatarHcl) — un motiv unic per HCL.
Migrații cumulative: adaugă AddModulHCL.

Pentru S50 (controllere HCL)
Conform plan: 7 controllere + extinderi + smoke test end-to-end. Volumul e mare → strategia paritar S49: un controller per mesaj/sub-pas, fără rescrierea fișierelor existente decât unde absolut necesar.
Ordine sugerată:

HclController (cel mai gras — 11 endpoint-uri)
SemnatariHclController + extindere SedinteController cu POST /PresedinteSedinta
ComunicariHclPrefectController + RegistruComunicariPrefectController
RelatiiHclController
HclDashboardController (T-3 widget)
Extindere DocumenteController pentru anexe HCL (gardă TipDocument = Altele forțat când HclId setat)
Smoke test complet în Cleriq.http: provisioning → ședință → punct ProiectHCL adoptat → setare președinte → generare HCL → editare conținut → atribuire număr → semnare → comunicare prefect → publicare.

De ținut minte la fiecare endpoint:

Cele 3 precondiții POST /Genereaza
Matricea DELETE HCL cu 6 stări + numere arse
Gărzile DELETE Sedinta și DELETE PunctOrdineZi când există HCL Numerotat+
Forțare TipDocument = Altele la upload Document cu HclId
Decizia regenerare conținut la AtribuieNumar (vezi problemă deschisă #2)

Reluare gândit complet înainte de cod. Planul e ghid, nu script — fiecare controller merită gândit pentru integrarea cu ce există (paritar PV, ConvocareController) înainte de implementare.
Servicii bază sunt complete și acoperă logica complexă. S50 va fi în mare parte plumbing controller + validare DTO + integrare cu serviciile.