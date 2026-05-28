namespace Cleriq.Helpers;

public static class ExtensiiData
{
    public static DateTime LaFusOrar(this DateTime utc, string fusOrar)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(fusOrar);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback defensiv: dacă DB are un nume invalid, returnăm UTC
            return utc;
        }
    }

    public static DateTime? LaFusOrar(this DateTime? utc, string fusOrar)
        => utc.HasValue ? utc.Value.LaFusOrar(fusOrar) : null;


    public static string IndicatorFusOrar(this DateTime utc, string fusOrar)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(fusOrar);
            var offset = tz.GetUtcOffset(utc);
            var semn = offset < TimeSpan.Zero ? "-" : "+";
            var ore = Math.Abs(offset.Hours);
            var minute = Math.Abs(offset.Minutes);
            return minute == 0 ? $"UTC{semn}{ore}" : $"UTC{semn}{ore}:{minute:D2}";
        }
        catch (TimeZoneNotFoundException)
        {
            return string.Empty;   // fallback: fără indicator (la fel ca LaFusOrar care cade pe UTC)
        }
    }
}