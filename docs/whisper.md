# Cleriq — Whisper (transcriere audio self-hosted)

Acest fișier acoperă: contractul HTTP cu wrapper-ul, pod-ul Docker custom, decizia model și lecțiile acustice. Integrarea .NET (entitate `Transcriere`, `IServiciuTranscriere`, `WorkerTranscrieri`, `IGeneratorPromptTranscriere`, retry backoff) e în `backend.md`.

## Decizia model (validată empiric)

**Configurația de calitate**: WhisperX 3.7.4 (sufficient — NU e nevoie de upgrade la 3.8.6) cu `large-v2` + prompt scurt doar-nume + diarizare (`pyannote/speaker-diarization-community-1`).

**FĂRĂ**: hotwords, `min/max_speakers`, preprocesare audio.

**Calitate validată pe RTX 4090**:
- 96% nume corecte (22/23)
- 0 halucinații
- ~27× realtime
- Cost: $0.20-0.30 / ședință de ~50 min

**Limită cunoscută**: aproximări fonetice fine (`"Arghirescu" → "Argirescu"`) — limită Whisper, nu regresie de configurație. Acoperite de secretar la validare.

## Lecții acustice (de respectat în decizii viitoare)

- **Calitatea audio = limitatorul DOMINANT**, NU alegerea modelului. Audio remot/hibrid (telefon, platforme video) = limită dură pentru nume — cade în orice model. Pârghie mai mare decât configurația = ghid setup înregistrare pentru primării.
- **`v3` face prompt-bleed peste vorbire reală**, `v2` mai sigur la halucinații. v3 câștigă pe audio curat, v2 pe audio greu — se anulează între cele două orașe testate. Siguranța v2 la halucinații se menține universal. (Halucinațiile sunt eroarea cea mai periculoasă pentru un act legal.)
- **Promptul scurt doar-nume = marele câștig** (0.81 → 0.98 acuratețe nume). Repară greșelile țintit + consecvența pe tot fișierul. **Ține pe ședințe lungi** (validat pe 50 min, fără erodare). În produs: TOȚI prezenții în prompt — numele omis rămâne nereparat.
- **Prompt lung scurge fraze de domeniu în goluri de audio prost** ("prompt-bleed"). Doar-nume nu are ce scurge — confirmat fără bleed pe audio degradat.
- **`min/max_speakers` = INERT**. Parametrul controlează NUMĂRUL de vorbitori; erorile noastre sunt atribuirea segmentelor scurte (Domnul X → prezentă). NU se repară din numărul de vorbitori.
- **Preprocesare audio = NEUTRĂ**. Bleed-ul e problemă de MODEL, nu de audio.
- **Diarizarea e nesigură pe răspunsuri scurte** (confuzii de gen). Soluția arhitecturală = mapare vorbitori determinist la nivel produs (parsarea apelului nominal) — Faza 3.

**Modelul de produs**: Cleriq = "draft de PV foarte avansat" pe care secretarul îl validează (~30 min), NU transcript 100% automat. Erorile rămase = exact ce corectează secretarul.

## Wrapper learnedmachine

Repo: `learnedmachine/whisperx-asr-service`. **NU ahmetoner** (bug critic pe `initial_prompt` la TypeError, issue upstream nereparat).

### Endpoint principal

`POST /asr`

### CRITIC: form-data vs query string

`task`, `language`, `initial_prompt` se trimit ca **multipart form-data**, NU query string. FastAPI le declară `Form(...)` și **ignoră silent** query-ul → Whisper auto-detectează limba greșit (galeză etc.). Bug-ul Form-vs-Query a costat o regresie majoră (s24/s25).

```
enable_diarization=true&output=json   ← query string (OK)
task, language, initial_prompt        ← multipart form (OBLIGATORIU)
```

### `hotwords` NU există

Câmpul NU e în signature-ul wrapper-ului pinned. **Regulă**: codul real al wrapper-ului e autoritativ, NU research-urile externe.

### Patch obligatoriu în wrapper

În `app_main.py` (linia ~214), `dataclasses.replace + reassign` pe `whisper_model.options.initial_prompt`. Kwarg-ul pasat direct la `.transcribe()` e silent-dropped de WhisperX.

### `GPU_CONCURRENCY=1` OBLIGATORIU

Semaforul async = thread-safety pe mutația partajată `options.initial_prompt`. Două cereri paralele cu prompturi diferite → corupție. Scalare multi-GPU = Ray Serve replicate, NU concurență mărită.

### Schema răspuns

```json
{ "text": [ /* segmente */ ] }
```

**Diferit de CLI WhisperX** (`{"segments": [...]}`). `ExtrageDurataDinJson` în .NET are **lookup dublu defensiv** (`text` SAU `segments`) pentru robustețe.

**Cleriq NU parsează semantic `ContinutBrut`** — parser dedicat în Angular pentru viewer transcript (implementat în frontend).

## Pod Docker custom

**Repo**: `github.com/David-Paunescu/cleriq-whisper-pod`

**Imagine Docker Hub**: `davidp90/cleriq-whisper-pod`

**CI/CD**: GitHub Actions pe push `main` → tags `latest` + `sha-<short>`. Secret `DOCKERHUB_TOKEN` în repo settings.

### Dockerfile multi-stage

**Stage 1**: `caddy:2.8.4-builder` + `xcaddy` cu plugin `github.com/mholt/caddy-ratelimit`.

**Stage 2**: `FROM learnedmachine/whisperx-asr-service` **cu digest pinned `@sha256:...`** + binary Caddy + Caddyfile + entrypoint.

Build CI ~10-15 min.

### Pinning versiuni

**Template RunPod pinned pe tag SHA** (`sha-<short>`), NICIODATĂ `:latest`. Tag-ul SHA garantează imagine identică la pornire — `:latest` se poate schimba sub picioare.

## Caddy = punct unic de securitate

### Ordine Caddyfile (contează pentru info hiding)

```
/caddy-health    → public (înainte de orice altă regulă)
IP filter        → 403 ÎNAINTEA oricărui indiciu de auth (nu scurgem că auth-ul e Bearer)
route {
  rate_limit     → 10 req/min per cheie Authorization
  Bearer auth    → dual-key (WHISPER_API_KEY + _BACKUP)
  fallback       → reverse_proxy :9000 cu timeout-uri 6h
}
```

### IP allowlist

Env var `CLERIQ_ALLOWED_IPS`. Lipsă → **default allow-all în entrypoint** (dev convenient, producție explicit). Producție obligatoriu setat la IP-ul backend-ului Cleriq.

### Bearer dual-key (rotație zero-downtime)

Două chei active simultan: `WHISPER_API_KEY` (primary) + `WHISPER_API_KEY_BACKUP`. Rotație:
1. Setezi key nou ca BACKUP
2. Mut backend Cleriq pe key nou
3. Promovezi BACKUP la PRIMARY (devine WHISPER_API_KEY)
4. Eliberezi BACKUP

**Matcher `@empty_bearer`** OBLIGATORIU pentru orice matcher Caddy pe env var potențial gol. Interceptează `"Bearer "` (cu spațiu, fără cheie) ca să nu treacă ca match valid când env var-ul e gol.

### Rate limit

- Cheie = header `Authorization` (per cheie API)
- 10 req/min
- Bucket separat per cheie

### Token redaction în log

`format filter` cu `Authorization delete` în log directive — bearer-ul NU apare în log-uri.

### Lecție Caddy

Pluginurile cu `order X before Y` global pot fi scoase **SILENT** din lanț (`caddy validate` trece!). Folosește **`route` block cu ordine explicită** ca să garantezi că plugin-ul rămâne activ.

## Entrypoint

**Fail-fast** pe `WHISPER_API_KEY` lipsă (refuz pornire).

**`caddy validate`** înainte de `caddy run` (oprire la config invalid).

**Pornire**: Caddy în background + `uvicorn app.main:app --host 0.0.0.0 --port 9000`.

## RunPod — operare

### Template setup

**Imagine**: `davidp90/cleriq-whisper-pod:sha-<short>` (pinned).

**Env vars complete**:
```
HF_TOKEN=hf_...                                # token HuggingFace tip Read
DEVICE=cuda
COMPUTE_TYPE=float16
BATCH_SIZE=16
PRELOAD_MODEL=large-v2
GPU_CONCURRENCY=1                              # OBLIGATORIU (vezi mai sus)
TORCH_FORCE_NO_WEIGHTS_ONLY_LOAD=true          # fix PyTorch 2.6 WeightsUnpickler
WHISPER_API_KEY=<crypto-safe>
WHISPER_API_KEY_BACKUP=                        # opțional pentru rotație
CLERIQ_ALLOWED_IPS=<IP backend>                # producție: explicit; dev: omis = allow-all
```

**Hardware**: RTX 4090 Secure EU-RO-1 pentru calitate/producție (~$0.70/oră). Orice GPU EU pentru teste de infra/auth.

**Storage**:
- Container disk: 30+ GB
- Volume: 20 GB pe `/.cache` (modele HuggingFace)
- HTTP port: 8080

**Drift `ModelFolosit`**: backend-ul Cleriq trimite `Whisper:ModelFolosit` (config), pod-ul are `PRELOAD_MODEL`. **Risc de drift** — mitigare: pin în template + consecvență manuală la schimbare.

### HuggingFace setup (la cont nou)

Acceptare termeni pe **3 modele pyannote**:
- `pyannote/speaker-diarization-community-1` (NU 3.1 — altfel prima rulare crapă)
- `pyannote/segmentation-3.0`
- `pyannote/speaker-diarization-3.1`

Token tip **Read** din `huggingface.co/settings/tokens`.

### Disciplină operațională

**Terminate** (NU Stop) după fiecare sesiune.

Motiv: Stop păstrează slot-ul GPU → "not enough free GPUs" la următoarea pornire. Cost GPU se plătește cât pod-ul e pornit, NU cât se folosește.

**Confirmă descărcarea fișierelor** înainte de Terminate.

## Endpoint diagnostic în Cleriq

`GET /api/Transcriere/HealthCheck` — `[Authorize(Roles = "Admin,SuperAdmin")]`.

Diagnostic rapid pe tot lanțul: DNS → TLS → Caddy → Bearer → IP → wrapper FastAPI → GPU.

Răspuns: `{Succes, LatentaMs, Status, Device, ComputeType, Detalii}`.

**Pattern pentru orice integrare black-box viitoare** (LLM corector etc.).

## Smoke test rapid

```powershell
# 1. HealthCheck Cleriq backend
curl.exe -H "Authorization: Bearer $tokenAdmin" `
  https://localhost:7006/api/Transcriere/HealthCheck

# 2. Upload audio real
curl.exe -X POST `
  -H "Authorization: Bearer $tokenAdmin" `
  -F "fisier=@audio.mp3" `
  https://localhost:7006/api/Sedinte/1/Transcriere

# 3. Monitorizare progress
curl.exe -H "Authorization: Bearer $tokenAdmin" `
  https://localhost:7006/api/Sedinte/1/Transcriere
```

**Accelerare retry** pe transcriere `Esuata`: UPDATE manual în `Transcrieri` cu **reset `Status = InAsteptare`** (worker-ul filtrează pe Status; altfel no-op silent).

## Capcane (specifice fluxului Whisper)

- **`-SkipCertificateCheck` doar PS7+**. PS 5.1: `[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}`.
- **U+202A** (LRE) caracter invizibil la copy-paste de căi din Properties → `curl: error 26`. Fix: `cd` în folder + nume relativ.
- **GitHub web UI** = un commit per fișier salvat → build-uri CI multiple. SHA-ul relevant = cel mai recent. Preferă git local cu un singur commit.
- **Comment la coloana 1** în patch-ul `app_main.py` (linia ~214) — cosmetic, la următoarea atingere a fișierului.

## Decizii respinse explicit (NU se redeschid)

### Modele alternative

- **`large-v3`** — face prompt-bleed peste vorbire reală. v2 sigur la halucinații pe toate datele testate.
- **`wav2vec2 română`** (TOP-1 benchmark RO) — producea `"bunozia"` în loc de `"Bună ziua"`. **Lecția**: "TOP-1 la benchmark" ≠ "bun pentru produs". Benchmark-urile măsoară WER pe text normalizat (fără punctuație/diacritice); Cleriq are nevoie de document formatat.

### Wrapper alternative

- **`ahmetoner/whisper-asr-webservice`** — bug critic pe `initial_prompt` (TypeError), issue upstream nereparat.

### Provideri cloud

- **OpenAI Whisper API, Google USM/Chirp, Azure** — suveranitate. Ședințele conțin date sensibile (nume, voturi, cazuri sociale). GDPR nu dispare prin publicare pe YouTube. Moat real = integrarea (DB + workflow + SEAP), NU motorul ASR.

### Parametri de configurare

- **`hotwords`** — nu există în signature-ul wrapper-ului pinned. Cod orphaned în Cleriq (cleanup minor la următoarea atingere).
- **`min/max_speakers`** — inert empiric. Parametrul controlează NUMĂRUL de vorbitori; erorile sunt atribuirea segmentelor scurte.
- **Preprocesare audio** (ffmpeg `highpass=f=80,loudnorm`) — neutră. Bleed-ul e problemă de model, NU de audio.

### Abordări de calitate

- **Vocabular pre-mută (find-replace orb pe nume)** — periculos. Numele consilierilor seamănă între ele → vot atribuit greșit. Vocabularul INFORMEAZĂ prin `initial_prompt`, NU pre-mută.
- **Fork de cod Python al wrapper-ului** — cost de mentenanță complet diferit față de Dockerfile custom peste upstream pinned. Patch-ul în `app_main.py` rămâne minim, aplicat la build.

## Probleme deschise / Faza 3

- **Idempotency wrapper Whisper** — cheie pe `TranscriereId`. Acum: succes "pierdut" la SaveChanges eșuat post-răspuns OK → reprocesare, cost GPU dublu. Acceptat pentru pilot.
- **LLM corector self-hosted** (Llama/Mistral/Qwen mediu) cu prompt STRICT anti-falsificare. Rezolvă reziduul fonetic/gramatical și prinde halucinațiile.
- **Mapare vorbitori determinist** — parsarea apelului nominal. Consilii diferite înregistrează prezența diferit (apel verbal vs aplicație de vot) — maparea trebuie să trateze AMBELE cazuri.
- **Ghid setup înregistrare pentru primării** — cea mai mare pârghie de calitate, mai ales pentru ședințe hibride.
- **Test integration pentru wrapper Whisper** cu `HandlerHttpFals` — bug-ul Form-vs-Query ar fi fost prins instant. Vezi `teste.md`.
- **Stocare perechi brut + corectat** ca date de antrenament pentru fine-tune viitor.
