# Frontend Cleriq (cleriq-web)

Angular 22 **zoneless** (fără zone.js), Angular Material (M3, Azure/Blue),
SCSS, standalone components + signals, Reactive Forms, TypeScript strict,
`LOCALE_ID='ro'`. Fără SSR (decizie la sesiunea portalului public).
Biblioteca completă de pattern-uri: `docs/frontend.md`.

## Comenzi
`npm start` (ng serve :4200) · `npm run build` · `npm test` (Vitest) ·
`npm run lint`. Backend pe https://localhost:7006 + container redis pornit.

## Reguli must-know
- Domeniu în română, termeni Angular în engleză. Mesaje de eroare user: română.
- Nu faci rezumatul sesiunii, pana nu iti descriu cum trebuie facut
- Componente fără sufix de fișier (`login.ts`); infra `core/` cu sufixe
  clasice (`.service.ts`, `.guard.ts`, `.interceptor.ts`).
- Orice pagină nouă = lazy `loadComponent`. În routing, segmentele fixe
  ÎNAINTEA celor cu `:param`.
- Reactivitate template pe form values:
  `toSignal(form.controls.X.valueChanges, { initialValue: form.controls.X.value })`
  — NU `.value` direct în `@if` (zoneless nu prinde schimbarea).
- `async/await` cu `firstValueFrom`, NU `subscribe`. Serviciile per feature
  returnează `Promise<T>`.
- Permisiuni client în `features/X/X.permisiuni.ts` = oglindă strictă a
  matricei backend (drift backend↔frontend = bug).
- Download autenticat = prin blob (`window.open` ocolește `authInterceptor`
  → 401). Cleanup obligatoriu `URL.revokeObjectURL`.
- Date/timp: helperele din `shared/data.ts` cu `timeZone` explicit
  (`Europe/Bucharest`), NICIODATĂ `new Date()` local.

Pattern-uri detaliate (auto-save, publish flow, indicator salvare, tab-uri
hub ședință, Material gotchas, upload cu progres): `docs/frontend.md`.
