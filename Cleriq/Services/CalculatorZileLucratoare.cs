using System.Collections.Concurrent;

namespace Cleriq.Services;

public class CalculatorZileLucratoare : ICalculatorZileLucratoare
{
    // Sărbători legale fixe — Codul Muncii art. 139
    private static readonly (int Luna, int Zi)[] SarbatoriFixe =
    {
        (1, 1),    // Anul Nou
        (1, 2),
        (1, 24),   // Ziua Unirii Principatelor
        (5, 1),    // Ziua Muncii
        (6, 1),    // Ziua Copilului
        (8, 15),   // Adormirea Maicii Domnului
        (11, 30),  // Sf. Andrei
        (12, 1),   // Ziua Națională
        (12, 25),  // Crăciun
        (12, 26),
    };

    // Cache per an pentru sărbătorile derivate din Paște (calcul scump)
    private static readonly ConcurrentDictionary<int, HashSet<DateOnly>> CacheSarbatori = new();

    public bool EsteZiLucratoare(DateOnly data)
    {
        var zi = data.DayOfWeek;
        if (zi == DayOfWeek.Saturday || zi == DayOfWeek.Sunday)
            return false;

        return !ObtineSarbatoriPentru(data.Year).Contains(data);
    }

    public DateOnly AdaugaZileLucratoare(DateOnly start, int zile)
    {
        if (zile < 0)
            throw new ArgumentOutOfRangeException(nameof(zile),
                "Numărul de zile trebuie să fie pozitiv.");

        if (zile == 0) return start;

        var rezultat = start;
        var adaugate = 0;
        while (adaugate < zile)
        {
            rezultat = rezultat.AddDays(1);
            if (EsteZiLucratoare(rezultat))
                adaugate++;
        }
        return rezultat;
    }

    public int CalculeazaZileLucratoarePanaLa(DateOnly start, DateOnly final)
    {
        if (final < start)
            throw new ArgumentException("final trebuie să fie >= start.", nameof(final));

        if (final == start) return 0;

        var count = 0;
        var cursor = start.AddDays(1);
        while (cursor <= final)
        {
            if (EsteZiLucratoare(cursor))
                count++;
            cursor = cursor.AddDays(1);
        }
        return count;
    }

    private HashSet<DateOnly> ObtineSarbatoriPentru(int an)
    {
        return CacheSarbatori.GetOrAdd(an, anNou =>
        {
            var sarbatori = new HashSet<DateOnly>();

            foreach (var (luna, zi) in SarbatoriFixe)
                sarbatori.Add(new DateOnly(anNou, luna, zi));

            var paste = CalculeazaPasteOrtodox(anNou);
            sarbatori.Add(paste.AddDays(-2));   // Vinerea Mare
            sarbatori.Add(paste);                // Paște
            sarbatori.Add(paste.AddDays(1));    // A doua zi de Paște
            sarbatori.Add(paste.AddDays(49));   // Rusalii
            sarbatori.Add(paste.AddDays(50));   // A doua zi de Rusalii

            return sarbatori;
        });
    }

    // Algoritm Meeus/Jones/Butcher pentru Paște ortodox.
    // Returnează data în calendar Julian, +13 zile = Gregorian (valid sec. 21-22).
    private static DateOnly CalculeazaPasteOrtodox(int an)
    {
        var a = an % 4;
        var b = an % 7;
        var c = an % 19;
        var d = (19 * c + 15) % 30;
        var e = (2 * a + 4 * b - d + 34) % 7;
        var suma = d + e + 114;
        var luna = suma / 31;
        var zi = (suma % 31) + 1;

        var dataJulian = new DateOnly(an, luna, zi);
        return dataJulian.AddDays(13);
    }
}