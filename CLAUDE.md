# Cleriq

SaaS de management al ședințelor de consiliu local pentru primăriile din
România. Diferențiator: transcriere audio automată cu Whisper self-hosted.
Multi-tenant — fiecare UAT (primărie) e un tenant izolat, identificat prin
slug. Vânzare prin achiziție directă pe SEAP.
Context de produs/piață/legal: `docs/context-aplicatie.md`.

## Structura soluției
- `Cleriq/` — backend ASP.NET Core Web API (.NET 10)
  - `Controllers/` `Services/` `Data/` `Models/` `DTOs/` `Middleware/`
    `Helpers/` `Migrations/`
- `Cleriq.Tests/` — teste de integrare xUnit (vezi `Cleriq.Tests/CLAUDE.md`)
- `cleriq-web/` — frontend Angular 22 (vezi `cleriq-web/CLAUDE.md`)
- `docs/` — context, setup, roadmap, planuri de sesiune
- `Cleriq.slnx` — soluția .NET

## Stack
.NET 10, EF Core 10 + SQL Server, ASP.NET Identity + JWT, Redis (cache +
Data Protection), QuestPDF (PDF), Markdig (Markdown), MailKit (email),
Twilio (SMS). Frontend: Angular 22 zoneless, Angular Material (M3), SCSS.

## Comenzi
- Build:    `dotnet build Cleriq.slnx`
- Teste:    `dotnet test`   (cere SQL Server local + Redis — vezi `docs/setup.md`)
- API:      `dotnet run --project Cleriq`   (https://localhost:7006)
- Frontend: `cd cleriq-web && npm install && npm start`   (:4200)

## Convenții generale
- Cod și domeniu în limba română; termeni de framework în engleză.
  Mesajele către utilizator: română.
- Multi-tenant cu filtre globale; rutele publice `/public/{slug}/...` prin
  `SlugTenantMiddleware`.
- Soft-delete (`StersLa`) + audit (cine/când). Orice stocare nouă de fișiere
  se adaugă la scanul de orfani din Mentenanță.
- Comentarii minime în cod (vezi „Cum lucrăm").

## Cum lucrăm (preferințe)
- Gândește în adâncime. Fără scurtături în detrimentul calității — preferăm
  să facem corect de la început decât să refacem mai târziu.
- Sunt programator începător; la fiecare decizie de arhitectură recomandă-mi
  varianta optimă, cu un motiv scurt.
- Explică pe scurt CE ai implementat, cu focus pe cod, nu discuții lungi.
- Pas cu pas: fiecare temă separat; secțiunile mari în sesiuni separate
  (context mic). Dacă vezi o problemă, o rezolvăm acum, nu o amânăm — decât
  dacă chiar e necesar.
- Comentarii minime în cod; explicațiile rămân în chat, nu în fișiere.
- TU (Claude) rulezi testele și verifici rezultatul — nu-mi da mie comenzi
  sau query-uri de copiat. Arată-mi ce a ieșit.
- Nu faci rezumatul sesiunii, pana nu iti descriu cum trebuie facut

## Planuri de sesiune
Planurile detaliate sunt în `docs/planuri/`. La începutul unei sesiuni îți
spun faza; citește planul ei înainte de a scrie cod.
