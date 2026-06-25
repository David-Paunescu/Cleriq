# docs/

Documentația de referință Cleriq. Fișierele de aici se citesc LA NEVOIE —
NU se încarcă automat (spre deosebire de fișierele `CLAUDE.md`).

- `context-aplicatie.md` — produs, piață, competiție, cadru legal, business.
  (din `prompt_context_aplicatie.md`, fără artefactele de conversație)
- `setup.md` — mediu local: SQL Server, Redis/Docker, user-secrets, seed dev,
  env vars de producție, capcane.
- `roadmap.md` — planul pe 12 faze (de la s44 până la pilot).
- `teste.md` — referința completă de testare (rezumatul e în
  `Cleriq.Tests/CLAUDE.md`).
- `frontend.md` — biblioteca completă de pattern-uri Angular (rezumatul e în
  `cleriq-web/CLAUDE.md`).
- `planuri/` — planul detaliat al fiecărei sesiuni (ex. `plan_faza3_*.md`).

## Cum se împart informațiile
- Fișierele `CLAUDE.md` (root, `Cleriq.Tests/`, `cleriq-web/`) conțin esența
  mereu-necesară și se încarcă automat (selectiv, pe folder).
- `docs/` conține detaliul complet, citit doar când lucrăm la acel subiect.
