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
}