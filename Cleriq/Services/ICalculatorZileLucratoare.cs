namespace Cleriq.Services;

public interface ICalculatorZileLucratoare
{
    bool EsteZiLucratoare(DateOnly data);
    DateOnly AdaugaZileLucratoare(DateOnly start, int zile);
    int CalculeazaZileLucratoarePanaLa(DateOnly start, DateOnly final);
}