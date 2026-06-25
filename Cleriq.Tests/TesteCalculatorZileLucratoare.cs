using Cleriq.Services;

namespace Cleriq.Tests;

// Test pur-unit: CalculatorZileLucratoare nu atinge DB/fixture (paritar TesteContractWhisperWrapper).
// Datele de referință sunt verificate manual: 01.01.2024 = luni; Paște ortodox 2024 = 05.05.2024.
public class TesteCalculatorZileLucratoare
{
    private readonly CalculatorZileLucratoare _calc = new();

    [Theory]
    [InlineData(2024, 1, 4, true)]    // joi normal
    [InlineData(2024, 1, 6, false)]   // sâmbătă
    [InlineData(2024, 1, 7, false)]   // duminică
    [InlineData(2025, 5, 1, false)]   // joi, dar Ziua Muncii (sărbătoare fixă)
    [InlineData(2025, 1, 24, false)]  // Ziua Unirii Principatelor
    [InlineData(2024, 12, 25, false)] // Crăciun
    public void EsteZiLucratoare_WeekendSiSarbatoriFixe(int an, int luna, int zi, bool asteptat)
        => Assert.Equal(asteptat, _calc.EsteZiLucratoare(new DateOnly(an, luna, zi)));

    [Fact]
    public void EsteZiLucratoare_PasteOrtodoxSiDerivate_NuSuntLucratoare()
    {
        // Paște ortodox 2024 = 5 mai (Meeus + 13 zile)
        Assert.False(_calc.EsteZiLucratoare(new DateOnly(2024, 5, 3)));   // Vinerea Mare
        Assert.False(_calc.EsteZiLucratoare(new DateOnly(2024, 5, 6)));   // a doua zi de Paște (luni)
        Assert.False(_calc.EsteZiLucratoare(new DateOnly(2024, 6, 24)));  // a doua zi de Rusalii (Paște+50, luni)
    }

    [Fact]
    public void AdaugaZileLucratoare_SareWeekendul()
        // vineri 05.01.2024 + 1 zi lucrătoare = luni 08.01.2024
        => Assert.Equal(new DateOnly(2024, 1, 8),
            _calc.AdaugaZileLucratoare(new DateOnly(2024, 1, 5), 1));

    [Fact]
    public void AdaugaZileLucratoare_SareSarbatoareLaMijloc()
        // miercuri 30.04.2025 + 1 zi = vineri 02.05.2025 (sare joi 1 mai, Ziua Muncii)
        => Assert.Equal(new DateOnly(2025, 5, 2),
            _calc.AdaugaZileLucratoare(new DateOnly(2025, 4, 30), 1));

    [Fact]
    public void AdaugaZileLucratoare_Zero_IntoarceData()
        => Assert.Equal(new DateOnly(2024, 1, 5),
            _calc.AdaugaZileLucratoare(new DateOnly(2024, 1, 5), 0));

    [Fact]
    public void AdaugaZileLucratoare_Negativ_Arunca()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => _calc.AdaugaZileLucratoare(new DateOnly(2024, 1, 5), -1));

    [Fact]
    public void CalculeazaZileLucratoarePanaLa_NumaraDoarLucratoarele()
        // luni 08 → luni 15.01.2024: marți-vineri (4) + luni 15 (1), weekendul exclus = 5
        => Assert.Equal(5, _calc.CalculeazaZileLucratoarePanaLa(
            new DateOnly(2024, 1, 8), new DateOnly(2024, 1, 15)));

    [Fact]
    public void CalculeazaZileLucratoarePanaLa_AceeasiZi_Zero()
        => Assert.Equal(0, _calc.CalculeazaZileLucratoarePanaLa(
            new DateOnly(2024, 1, 8), new DateOnly(2024, 1, 8)));

    [Fact]
    public void CalculeazaZileLucratoarePanaLa_FinalInainteDeStart_Arunca()
        => Assert.Throws<ArgumentException>(() => _calc.CalculeazaZileLucratoarePanaLa(
            new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 8)));
}
