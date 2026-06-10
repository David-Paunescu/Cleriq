namespace Cleriq.Services;

// Lacăt distribuit (Redis) pentru excludere mutuală între instanțele aplicației.
// Returnează un handle care eliberează lacătul la DisposeAsync (pattern „await using",
// ca IConexiuneEmail). null = lacătul e deținut de altă instanță sau Redis e indisponibil.
public interface ILacatDistribuit
{
    Task<IAsyncDisposable?> IncearcaBlocareAsync(string cheie, TimeSpan durata);
}