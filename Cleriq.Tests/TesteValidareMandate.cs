using System.Net;
using System.Net.Http.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteValidareMandate
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteValidareMandate(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    private static Task<HttpResponseMessage> PostMandatFunctie(
        HttpClient admin, TipFunctie tip, int? persoanaId, int? consilierId,
        DateOnly dataInceput, DateOnly? dataSfarsit = null)
        => admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = tip,
            persoanaId,
            consilierId,
            dataInceput,
            dataSfarsit,
            nrActNumire = (string?)null
        });

    // === VerificaOverlap — Primar / SecretarUat: exclusivitate per tenant ===

    // Existing [..2024-12-31], new [2024-12-31..] → boundary same-day = overlap.
    [Fact]
    public async Task Overlap_PrimarAdiacentSameDay_BoundaryInclusiv_409()
    {
        using var admin = await AdminAsync();
        var p1 = await admin.CreeazaPersoanaAsync("P1");
        var p2 = await admin.CreeazaPersoanaAsync("P2");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, p1, null,
            new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Primar, p2, null, new DateOnly(2024, 12, 31));
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Overlap_PrimarStrictUlterior_FaraOverlap_200()
    {
        using var admin = await AdminAsync();
        var p1 = await admin.CreeazaPersoanaAsync("P1");
        var p2 = await admin.CreeazaPersoanaAsync("P2");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, p1, null,
            new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Primar, p2, null, new DateOnly(2025, 1, 1));
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
    }

    [Fact]
    public async Task Overlap_PrimarOpenSiAlDoileaOpen_409()
    {
        using var admin = await AdminAsync();
        var p1 = await admin.CreeazaPersoanaAsync("P1");
        var p2 = await admin.CreeazaPersoanaAsync("P2");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, p1, null, new DateOnly(2020, 1, 1));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Primar, p2, null, new DateOnly(2025, 1, 1));
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Overlap_SecretarUat_ExclusivitatePerTenant_409()
    {
        using var admin = await AdminAsync();
        var p1 = await admin.CreeazaPersoanaAsync("S1");
        var p2 = await admin.CreeazaPersoanaAsync("S2");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.SecretarUat, p1, null, new DateOnly(2020, 1, 1));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.SecretarUat, p2, null, new DateOnly(2025, 1, 1));
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    // === VerificaOverlap — Viceprimar: filtrat pe ConsilierId ===

    [Fact]
    public async Task Overlap_DoiViceprimariConsilieriDiferiti_AmbeleReusesc_200()
    {
        using var admin = await AdminAsync();
        var c1 = await admin.CreeazaConsilierAsync("C1");
        var c2 = await admin.CreeazaConsilierAsync("C2");
        await admin.CreeazaMandatConsilierAsync(c1, new DateOnly(2020, 1, 1));
        await admin.CreeazaMandatConsilierAsync(c2, new DateOnly(2020, 1, 1));

        var r1 = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c1, new DateOnly(2020, 1, 1));
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var r2 = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c2, new DateOnly(2020, 1, 1));
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    [Fact]
    public async Task Overlap_AcelasiConsilierDouaViceprimarii_409()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.CreeazaMandatConsilierAsync(c, new DateOnly(2020, 1, 1));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, c, new DateOnly(2020, 1, 1));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c, new DateOnly(2024, 1, 1));
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    // === PUT cu mandatExistentId: exclude self ===

    [Fact]
    public async Task Put_EditareCuDateleNoiNeoverlap_200_ChiarCuSibling()
    {
        using var admin = await AdminAsync();
        var pA = await admin.CreeazaPersoanaAsync("PA");
        var pB = await admin.CreeazaPersoanaAsync("PB");

        var mA = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pA, null,
            new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pB, null, new DateOnly(2026, 1, 1));

        var raspuns = await admin.PutAsJsonAsync($"/api/MandateFunctie/{mA}", new
        {
            dataInceput = new DateOnly(2021, 1, 1),
            dataSfarsit = new DateOnly(2024, 12, 31),
            nrActNumire = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
    }

    [Fact]
    public async Task Put_EditareCareCreazaOverlapCuSibling_409()
    {
        using var admin = await AdminAsync();
        var pA = await admin.CreeazaPersoanaAsync("PA");
        var pB = await admin.CreeazaPersoanaAsync("PB");

        var mA = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pA, null,
            new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pB, null, new DateOnly(2025, 1, 1));

        var raspuns = await admin.PutAsJsonAsync($"/api/MandateFunctie/{mA}", new
        {
            dataInceput = new DateOnly(2020, 1, 1),
            dataSfarsit = new DateOnly(2025, 6, 30),
            nrActNumire = (string?)null
        });
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    // === PoateFiViceprimar (400 dacă fail) ===

    [Fact]
    public async Task Viceprimar_ConsilierFaraMandat_400()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C fără mandat");

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c, new DateOnly(2024, 1, 1));
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Viceprimar_MandatConsilierAcoperaPerioada_200()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.CreeazaMandatConsilierAsync(
            c, new DateOnly(2020, 1, 1), new DateOnly(2028, 12, 31));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c,
            new DateOnly(2024, 1, 1), new DateOnly(2027, 12, 31));
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
    }

    [Fact]
    public async Task Viceprimar_MandatConsilierNuAcoperaEnd_400()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.CreeazaMandatConsilierAsync(
            c, new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c,
            new DateOnly(2022, 1, 1), new DateOnly(2025, 12, 31));
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Viceprimar_MandatConsilierBoundedDarVicepOpen_400()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.CreeazaMandatConsilierAsync(
            c, new DateOnly(2020, 1, 1), new DateOnly(2028, 12, 31));

        var raspuns = await PostMandatFunctie(
            admin, TipFunctie.Viceprimar, null, c, new DateOnly(2024, 1, 1));
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    // === Inchide bypassuje PoateFiViceprimar ===

    // Scenariu fantom: Mandatul de consilier expiră, Mandatul de Viceprimar rămâne deschis.
    // Inchide trebuie să permită închiderea cu DataSfarsit chiar și după expirarea mandatului
    // de consilier — e instrumentul pentru rezolvarea fantomei.
    [Fact]
    public async Task Inchide_PeViceprimarFantoma_PermiteFaraReValidare_200()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C fantomă");
        var mandatConsilierId = await admin.CreeazaMandatConsilierAsync(
            c, new DateOnly(2020, 1, 1));
        var vicepId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, c, new DateOnly(2020, 1, 1));

        await admin.ActualizeazaMandatConsilierAsync(
            mandatConsilierId, new DateOnly(2020, 1, 1), new DateOnly(2022, 12, 31));

        var raspuns = await admin.PostAsJsonAsync(
            $"/api/MandateFunctie/{vicepId}/Inchide",
            new { dataSfarsit = new DateOnly(2024, 6, 30) });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
    }
}