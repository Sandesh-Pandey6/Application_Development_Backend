namespace Autopartspro.Infrastructure;


// Helpers for PostgreSQL timestamptz — comparisons must use DateTimeKind.Utc.

internal static class UtcDate
{
    public static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    public static DateTime StartOfMonth(DateTime utcReference)
    {
        var utc = EnsureUtc(utcReference);
        return new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime StartOfYear(DateTime utcReference)
    {
        var utc = EnsureUtc(utcReference);
        return new DateTime(utc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime StartOfDay(DateTime utcReference)
    {
        var utc = EnsureUtc(utcReference);
        return new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
