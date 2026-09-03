namespace TimeReady.Api.Models.Auditing;

/// <summary>
/// Columns shared by live and archived audit rows, so search filters can be
/// written once without tying the archive table to the live entity hierarchy.
/// </summary>
public interface IAuditSearchRow
{
    long Id { get; }

    string EntityName { get; }

    string EntityId { get; }

    AuditAction Action { get; }

    string? UserId { get; }

    string UserName { get; }

    DateTimeOffset TimestampUtc { get; }
}
