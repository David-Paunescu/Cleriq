namespace Cleriq.Helpers;

public static class ValidareAudio
{
    public const long MarimeMaxima = 500L * 1024 * 1024; // 500 MB

    public static readonly IReadOnlyDictionary<string, string> ExtensiiPermise =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".m4a"] = "audio/mp4",
            [".ogg"] = "audio/ogg",
            [".flac"] = "audio/flac",
            [".aac"] = "audio/aac"
        };

    public static string? Valideaza(string numeFisier, long marime)
    {
        if (marime <= 0)
            return "Fișierul audio este gol.";
        if (marime > MarimeMaxima)
            return $"Fișierul audio depășește limita de {MarimeMaxima / (1024 * 1024)} MB.";

        var extensie = Path.GetExtension(numeFisier);
        if (string.IsNullOrEmpty(extensie) || !ExtensiiPermise.ContainsKey(extensie))
            return $"Extensie audio nepermisă. Permise: {string.Join(", ", ExtensiiPermise.Keys)}.";

        return null;
    }

    public static string TipMimePentru(string numeFisier)
        => ExtensiiPermise.TryGetValue(Path.GetExtension(numeFisier), out var mime)
            ? mime
            : "application/octet-stream";
}