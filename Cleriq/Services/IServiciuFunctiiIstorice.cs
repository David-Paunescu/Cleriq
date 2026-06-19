using Cleriq.Models;

namespace Cleriq.Services;

/// <summary>
/// Query istoric al funcțiilor oficiale: cine deținea o funcție/membrie la o dată dată.
/// Consumat de Modulele A (HCL), B (Proiecte HCL) și C (Dispoziții) pentru semnături derivate
/// pe acte administrative.
/// </summary>
/// <remarks>
/// Parametrul <c>data</c> reprezintă întotdeauna <b>data evenimentului juridic</b> — data ședinței
/// în care s-a votat HCL-ul, data emiterii dispoziției, data avizului de legalitate. Niciodată
/// data apelului sau data generării PDF-ului. La regenerare de PDF sau republicare PV semnat,
/// lookup-ul rămâne pe data evenimentului original.
/// </remarks>
public interface IServiciuFunctiiIstorice
{
    Task<Persoana?> CinEPrimarulLa(DateOnly data, CancellationToken ct = default);

    Task<Persoana?> CinESecretarulUatLa(DateOnly data, CancellationToken ct = default);

    /// <summary>
    /// Pattern „viceprimar fantomă": viceprimari al căror mandat de consilier era valid la data X.
    /// Cei fără mandat de consilier valid (demisie, deces, expirare neînchisă explicit) sunt
    /// excluși din rezultat — operatorul îi corectează manual prin badge dedicat în UI.
    /// </summary>
    Task<List<(Consilier Consilier, MandatFunctie Mandat)>> CineEViceprimariiLa(
        DateOnly data, CancellationToken ct = default);

    Task<List<(Consilier Consilier, RolComisie Rol)>> CineEMembriiComisieiLa(
        int comisieId, DateOnly data, CancellationToken ct = default);

    Task<Consilier?> CinePresedinteleComisieiLa(
        int comisieId, DateOnly data, CancellationToken ct = default);

    Task<List<Consilier>> CineEConsilieriiLa(DateOnly data, CancellationToken ct = default);
}