namespace Cleriq.Helpers;

public static class Telefon
{
    public static string? Normalizeaza(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var cifre = new string(input.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(cifre))
            return null;

        string codTaraSiNumar;
        if (cifre.StartsWith("00"))
            codTaraSiNumar = cifre.Substring(2);
        else if (cifre.StartsWith("0"))
            codTaraSiNumar = "40" + cifre.Substring(1);
        else
            codTaraSiNumar = cifre;

        if (codTaraSiNumar.Length < 8 || codTaraSiNumar.Length > 15)
            return null;

        return "+" + codTaraSiNumar;
    }
}