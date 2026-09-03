namespace TimeReady.Tests.Unit;

/// <summary>
/// Freezes "now" so the date-dependent rules can be tested deterministically.
/// </summary>
internal sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
