namespace TimeReady.Api.Services;

/// <summary>
/// Stable rule identifiers. The frontend uses them for icons and filtering,
/// which keeps it independent of the wording of a message.
/// </summary>
public static class ReadinessCodes
{
    public const string NegativeTimeBalance = "negative-time-balance";
    public const string VacationStartsSoon = "vacation-starts-soon";
    public const string LowVacationDays = "low-vacation-days";
    public const string ManagerNotInformed = "manager-not-informed";
    public const string HandoverIncomplete = "handover-incomplete";
    public const string NoVacationPlanned = "no-vacation-planned";
}
