using System.Security.Cryptography;

namespace Cleriq.Services;

public class StocareDocumenteDisk : IStocareDocumente
{
    private readonly string _caleRoot;
    private readonly ILogger<StocareDocumenteDisk> _logger;

    public StocareDocumenteDisk(IConfiguration config, ILogger<StocareDocumenteDisk> logger)
    {
        _logger = logger;

        var configurat = config["DirectorDocumente:CaleRoot"];
        if (string.IsNullOrWhiteSpace(configurat))
            throw new InvalidOperationException(
                "Configurare lipsă: DirectorDocumente:CaleRoot în appsettings.");

        // Permite cale relativă (rezolvată în ContentRootPath) sau absolută.
        _caleRoot = Path.IsPathRooted(configurat)
            ? configurat
            : Path.Combine(AppContext.BaseDirectory, configurat);

        Directory.CreateDirectory(_caleRoot);
    }

    public async Task<FisierStocat> SalveazaAsync(
        int institutieId, string numeFisierOriginal, Stream continut, CancellationToken ct = default)
    {
        var acum = DateTime.UtcNow;
        var subdir = Path.Combine(
            _caleRoot,
            institutieId.ToString(),
            acum.Year.ToString(),
            acum.Month.ToString("D2"));
        Directory.CreateDirectory(subdir);

        var extensie = Path.GetExtension(numeFisierOriginal);
        if (extensie.Length > 20) extensie = "";

        var numeUnic = $"{Guid.NewGuid():N}{extensie}";
        var caleAbsoluta = Path.Combine(subdir, numeUnic);

        using var sha = SHA256.Create();
        long total = 0;

        await using (var fileStream = new FileStream(
            caleAbsoluta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true))
        await using (var cryptoStream = new CryptoStream(
            fileStream, sha, CryptoStreamMode.Write, leaveOpen: true))
        {
            var buffer = new byte[81920];
            int citit;
            while ((citit = await continut.ReadAsync(buffer, ct)) > 0)
            {
                await cryptoStream.WriteAsync(buffer.AsMemory(0, citit), ct);
                total += citit;
            }
        }

        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();

        var cheieRelativa = Path.GetRelativePath(_caleRoot, caleAbsoluta)
                                .Replace('\\', '/');   // normalizare cross-platform

        _logger.LogInformation(
            "Document stocat: tenant={Tenant}, cheie={Cheie}, marime={Marime}B",
            institutieId, cheieRelativa, total);

        return new FisierStocat(cheieRelativa, total, hash);
    }

    public Task<Stream> DeschideAsync(string cheie, CancellationToken ct = default)
    {
        var caleAbsoluta = RezolvaCaleSigura(cheie);
        if (!File.Exists(caleAbsoluta))
            throw new FileNotFoundException("Fișierul nu există pe disk.", cheie);

        Stream stream = new FileStream(
            caleAbsoluta, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task StergeAsync(string cheie, CancellationToken ct = default)
    {
        var caleAbsoluta = RezolvaCaleSigura(cheie);
        if (File.Exists(caleAbsoluta))
            File.Delete(caleAbsoluta);
        return Task.CompletedTask;
    }

    private string RezolvaCaleSigura(string cheie)
    {
        var combinata = Path.GetFullPath(Path.Combine(_caleRoot, cheie));
        var rootNormalizat = Path.GetFullPath(_caleRoot);

        if (!combinata.StartsWith(rootNormalizat, StringComparison.Ordinal))
            throw new UnauthorizedAccessException(
                $"Cheie invalidă (path traversal detectat): {cheie}");

        return combinata;
    }
}