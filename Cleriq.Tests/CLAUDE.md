# Teste Cleriq

Teste de integrare xUnit prin `WebApplicationFactory`, pe SQL Server real
(`CleriqTest` — ștearsă și recreată la fiecare rulare) + Redis index 15.
NU InMemory/SQLite. Referință completă + capcane: `docs/teste.md`.

## Reguli de aur
1. Orice clasă: `[Collection("Cleriq")]` + primește `CleriqFixture` în
   constructor. NICIODATĂ factory propriu.
2. Verificările se fac DOAR prin API. `DbTest` e exclusiv pentru a aranja
   stări inaccesibile prin API (ex. `StersLa` în trecut, status la mijloc de
   flux) — niciodată pentru aserțiuni.
3. Izolare prin tenant nou cu slug `Guid` (`ProvisioneazaInstitutieAsync`),
   nu prin curățare DB.
4. Config de test prin env vars în `CleriqWebApplicationFactory`. Orice cheie
   nouă citită inline în `Program.cs` se adaugă în `SeteazaVariabileDeMediu`.
5. Asertă fiecare pas de setup, cu mesaj util
   (`await raspuns.Content.ReadAsStringAsync()`).
6. Aranjarea în `DbTest` oglindește semantica reală a serviciilor
   (ex. `Trimisa` + `TrimisLa` împreună; hash-ul refresh token).
7. Rate limiting: asertă COMPORTAMENT (apare 429 + `Retry-After` + fereastra
   nouă permite din nou), nu numărătoare exactă.
8. Un singur test per tip de stocare la testele de mentenanță.

## Stack și paritate
- xUnit 2.9 (NU v3). Fără FluentAssertions — assert-uri xUnit native.
- Scenariile se construiesc cu extensiile din `Infrastructura/ExtensiiTeste.cs`.
- Modele de paritate la teste noi: `TesteProcesVerbal.cs` (ciclul de viață al
  unui act), `TestePortalPublic.cs` (portalul public).
- Rulare secvențială (`parallelizeTestCollections: false`).
