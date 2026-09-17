namespace Microplex.Web.Utilities;

public static class SriLankaTime
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "Sri Lanka Standard Time", TimeSpan.FromHours(5.5), "Sri Lanka Standard Time (+05:30)", "Sri Lanka Standard Time");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    public static DateTime FromUtc(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
            : utcDateTime.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    public static DateTime ToUtc(DateTime sriLankaLocalDateTime)
    {
        var local = DateTime.SpecifyKind(sriLankaLocalDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, TimeZone);
    }
}
