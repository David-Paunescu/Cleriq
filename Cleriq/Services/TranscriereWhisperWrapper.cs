using System.Net.Http.Headers;
using System.Text.Json;

namespace Cleriq.Services;

public class TranscriereWhisperWrapper : IServiciuTranscriere
{
    private readonly HttpClient _http;
    private readonly ILogger<TranscriereWhisperWrapper> _logger;

    public TranscriereWhisperWrapper(
        HttpClient http,
        ILogger<TranscriereWhisperWrapper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<RezultatTranscriere> TrimiteAsync(
        Stream audio, string numeFisier, string prompt, CancellationToken ct = default)
    {
        var url = "/asr?task=transcribe"
            + "&language=ro"
            + "&diarize=true"
            + "&word_timestamps=true"
            + "&output=json"
            + $"&initial_prompt={Uri.EscapeDataString(prompt)}";

        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(audio, bufferSize: 81920);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "audio_file", numeFisier);

            _logger.LogInformation(
                "Trimit audio la wrapper Whisper: nume={Nume}, lungime_prompt={Lungime}",
                numeFisier, prompt.Length);

            using var response = await _http.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var corpRaspuns = await response.Content.ReadAsStringAsync(ct);
                var corpTruncat = corpRaspuns.Length > 500
                    ? corpRaspuns[..500] + "..."
                    : corpRaspuns;
                var statusCode = (int)response.StatusCode;
                var retriable = statusCode >= 500;

                _logger.LogWarning(
                    "Eșec wrapper Whisper: status={Status}, retriable={Retriable}",
                    statusCode, retriable);

                return new RezultatTranscriere(
                    Succes: false,
                    EsteRetriable: retriable,
                    ContinutJson: null,
                    DurataAudioSecunde: null,
                    Detalii: $"HTTP {statusCode}: {corpTruncat}");
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var durata = ExtrageDurataDinJson(json);

            _logger.LogInformation(
                "Transcriere primită: caractere={Caractere}, durata={Durata}s",
                json.Length, durata);

            return new RezultatTranscriere(
                Succes: true,
                EsteRetriable: false,
                ContinutJson: json,
                DurataAudioSecunde: durata,
                Detalii: $"OK, {json.Length} caractere");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancellation explicit, propagăm
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout la apel wrapper Whisper.");
            return new RezultatTranscriere(
                false, true, null, null, $"Timeout: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Eroare HTTP la apel wrapper Whisper.");
            return new RezultatTranscriere(
                false, true, null, null, $"Network: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eroare neașteptată la apel wrapper Whisper.");
            return new RezultatTranscriere(
                false, false, null, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static int? ExtrageDurataDinJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("segments", out var segments))
                return null;
            if (segments.ValueKind != JsonValueKind.Array)
                return null;

            double max = 0;
            foreach (var seg in segments.EnumerateArray())
            {
                if (seg.TryGetProperty("end", out var end) && end.TryGetDouble(out var val))
                {
                    if (val > max) max = val;
                }
            }
            return max > 0 ? (int)Math.Ceiling(max) : null;
        }
        catch
        {
            return null;
        }
    }
}