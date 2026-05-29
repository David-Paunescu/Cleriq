namespace Cleriq.Helpers;

public static class ValidareDocument
{
    public const long MarimeMaxima = 25 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> ExtensiiPermise =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    public static string? Valideaza(string numeFisier, long marime)
    {
        if (marime <= 0)
            return "Fișierul este gol.";
        if (marime > MarimeMaxima)
            return $"Fișierul depășește limita de {MarimeMaxima / (1024 * 1024)} MB.";

        var extensie = Path.GetExtension(numeFisier);
        if (string.IsNullOrEmpty(extensie) || !ExtensiiPermise.ContainsKey(extensie))
            return $"Extensie nepermisă. Permise: {string.Join(", ", ExtensiiPermise.Keys)}.";

        return null;
    }

    public static string TipMimePentru(string numeFisier)
        => ExtensiiPermise.TryGetValue(Path.GetExtension(numeFisier), out var mime)
            ? mime
            : "application/octet-stream";
}