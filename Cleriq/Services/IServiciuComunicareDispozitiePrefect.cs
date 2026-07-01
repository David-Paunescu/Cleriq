using Cleriq.Models;

namespace Cleriq.Services;

public record DispozitieUrgentDto(
    int DispozitieId,
    int Numar,
    int AnNumerotare,
    string Titlu,
    DateOnly DataEmitere,
    DateOnly DataLimitaComunicare,
    int ZileRamase,
    StatusActRedactional Status,
    TipDispozitie TipDispozitie);

public interface IServiciuComunicareDispozitiePrefect
{
    Task<int> SugereazaNumarOrdineRegistruAsync(
        int institutieId, int anRegistru, CancellationToken ct = default);

    DateOnly CalculeazaDataLimitaComunicare(DateOnly dataEmitere);

    Task<List<DispozitieUrgentDto>> ObtineDispozitiiUrgentDeComunicatAsync(
        int institutieId, int pragZileRamase, CancellationToken ct = default);
}
