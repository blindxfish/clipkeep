namespace ClipForge.Core.Settings;

/// <summary>Maps a <see cref="RetentionPeriod"/> to a maximum age, if any.</summary>
public static class RetentionPolicy
{
    /// <summary>Null means "keep forever"; otherwise the max age of a non-favorite entry.</summary>
    public static TimeSpan? MaxAge(RetentionPeriod period) => period switch
    {
        RetentionPeriod.Days30 => TimeSpan.FromDays(30),
        RetentionPeriod.Days90 => TimeSpan.FromDays(90),
        RetentionPeriod.Year1 => TimeSpan.FromDays(365),
        _ => null
    };
}
