using System.Text;
using System.Text.RegularExpressions;

namespace Cleriq.Helpers;

public static class Slugify
{
    private static readonly Regex PatternValid = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    private static readonly Regex NonAlfanumeric = new(
        "[^a-z0-9]+",
        RegexOptions.Compiled);

    // Defense in depth: cuvinte care nu pot fi slug-uri pentru a evita coliziuni de routing
    private static readonly HashSet<string> CuvinteRezervate = new(StringComparer.Ordinal)
    {
        // Generale / infrastructure
        "admin", "api", "public", "static", "www", "app", "auth", "login",
        "assets", "health", "status", "robots", "favicon",
        // Domain-specific pentru Cleriq (segmente de URL viitoare)
        "mol", "monitor", "transparenta", "documente", "cautare",
        "sedinte", "voturi", "consilieri", "convocari", "procesverbal", "puncte", "documente"
    };

    public const int LungimeMinima = 3;
    public const int LungimeMaxima = 100;

    /// <summary>
    /// Generează un slug canonic din text liber.
    /// Ex: „Primăria Slobozia" → „primaria-slobozia".
    /// Returnează string.Empty dacă rezultatul nu trece validarea (prea scurt, rezervat etc.).
    /// </summary>
    public static string Genereaza(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Transliterare RO → ASCII (explicit, predictibil; nu ne bazăm pe normalizare Unicode)
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case 'ă': case 'â': case 'Ă': case 'Â': sb.Append('a'); break;
                case 'î': case 'Î': sb.Append('i'); break;
                case 'ș': case 'ş': case 'Ș': case 'Ş': sb.Append('s'); break;
                case 'ț': case 'ţ': case 'Ț': case 'Ţ': sb.Append('t'); break;
                default: sb.Append(c); break;
            }
        }

        // 2. Lowercase
        var lowered = sb.ToString().ToLowerInvariant();

        // 3. Orice run de non-alfanumeric → un singur hyphen (colapsează spații, punctuație, hyphens duplicate)
        var hyphenated = NonAlfanumeric.Replace(lowered, "-");

        // 4. Trim hyphens de la capete
        var trimmed = hyphenated.Trim('-');

        // 5. Aplică lungimea maximă, cu re-trim defensiv dacă tăierea a lăsat hyphen la coadă
        if (trimmed.Length > LungimeMaxima)
            trimmed = trimmed.Substring(0, LungimeMaxima).TrimEnd('-');

        // 6. Validare finală
        if (trimmed.Length < LungimeMinima) return string.Empty;
        if (!PatternValid.IsMatch(trimmed)) return string.Empty;
        if (CuvinteRezervate.Contains(trimmed)) return string.Empty;

        return trimmed;
    }

    /// <summary>
    /// Validează un slug furnizat explicit de SuperAdmin în DTO.
    /// Returnează un mesaj de eroare descriptiv sau null dacă slug-ul e valid.
    /// </summary>
    public static string? Valideaza(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Slug-ul este obligatoriu.";
        if (slug.Length < LungimeMinima)
            return $"Slug-ul trebuie să aibă minim {LungimeMinima} caractere.";
        if (slug.Length > LungimeMaxima)
            return $"Slug-ul nu poate depăși {LungimeMaxima} caractere.";
        if (!PatternValid.IsMatch(slug))
            return "Slug-ul poate conține doar litere mici a-z, cifre 0-9 și hyphens între segmente (fără spații, diacritice sau alte caractere).";
        if (CuvinteRezervate.Contains(slug))
            return "Slug-ul este rezervat și nu poate fi folosit.";
        return null;
    }

    /// <summary>
    /// Variante numerice pentru sugestii la conflict.
    /// Ex: pentru „primaria-slobozia" produce „primaria-slobozia-2", „-3", „-4".
    /// </summary>
    public static IEnumerable<string> GenereazaSugestii(string baseSlug, int cate = 3)
    {
        for (int i = 2; i < 2 + cate; i++)
            yield return $"{baseSlug}-{i}";
    }

    public static bool EsteRezervat(string slug) => CuvinteRezervate.Contains(slug);
}