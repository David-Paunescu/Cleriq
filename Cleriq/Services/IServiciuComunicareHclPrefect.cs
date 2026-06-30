using Cleriq.Models;

namespace Cleriq.Services;

public record HclUrgentDto(
    int HclId,
    int Numar,
    int AnNumerotare,
    string Titlu,
    DateOnly DataAdoptare,
    DateOnly DataLimitaComunicare,
    int ZileRamase,
    StatusActRedactional Status);

public interface IServiciuComunicareHclPrefect
{
    Task<int> SugereazaNumarOrdineRegistruAsync(
        int institutieId, int anRegistru, CancellationToken ct = default);

    DateOnly CalculeazaDataLimitaComunicare(DateOnly dataAdoptare);

    Task<List<HclUrgentDto>> ObtineHclUrgentDeComunicatAsync(
        int institutieId, int pragZileRamase, CancellationToken ct = default);
}