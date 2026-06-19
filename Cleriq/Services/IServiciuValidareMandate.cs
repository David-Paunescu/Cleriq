using Cleriq.Models;

namespace Cleriq.Services;

public record RezultatValidare(bool Succes, string? MotivEsec);

public interface IServiciuValidareMandate
{
    /// <summary>
    /// Verifică dacă un consilier are mandat de consilier care acoperă integral perioada
    /// propusă pentru funcția de viceprimar. Dacă DataSfarsit propusă este null (perioadă
    /// deschisă), mandatul de consilier trebuie să fie și el deschis.
    /// </summary>
    Task<RezultatValidare> PoateFiViceprimar(
        int consilierId,
        DateOnly dataInceput,
        DateOnly? dataSfarsit,
        int? mandatExistentId,
        CancellationToken ct = default);

    /// <summary>
    /// Verifică suprapunerea perioadei propuse cu mandate existente de același tip.
    /// Pentru Primar/SecretarUat: exclusivitate per tenant. Pentru Viceprimar: per ConsilierId.
    /// <paramref name="mandatExistentId"/> permite excluderea propriei intrări la UPDATE.
    /// </summary>
    Task<RezultatValidare> VerificaOverlap(
        TipFunctie tip,
        DateOnly dataInceput,
        DateOnly? dataSfarsit,
        int? persoanaId,
        int? consilierId,
        int? mandatExistentId,
        CancellationToken ct = default);
}