namespace Queue.Backend.Common;

public static class DateTimeProvider
{
    /// <summary>
    /// Gets the current time in Thai Standard Time (UTC+7).
    /// </summary>
    public static DateTime Now()
    {
        return DateTime.UtcNow.AddHours(7);
    }
}
