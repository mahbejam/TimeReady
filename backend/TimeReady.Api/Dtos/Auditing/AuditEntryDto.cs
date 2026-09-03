namespace TimeReady.Api.Dtos.Auditing;

/// <summary>A recorded change, as returned by the API.</summary>
/// <param name="Id">Entry id.</param>
/// <param name="EntityName">Entity type that changed.</param>
/// <param name="EntityId">Primary key of the changed record.</param>
/// <param name="Action">Created, Updated or Deleted.</param>
/// <param name="UserId">Identity user id, or null for system changes.</param>
/// <param name="UserName">Who made the change.</param>
/// <param name="TimestampUtc">When it happened.</param>
/// <param name="ChangedColumns">Columns that changed, for updates.</param>
/// <param name="OldValues">Values before the change.</param>
/// <param name="NewValues">Values after the change.</param>
/// <param name="TraceId">Trace identifier, to find the request in the logs.</param>
public record AuditEntryDto(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string? UserId,
    string UserName,
    DateTimeOffset TimestampUtc,
    IReadOnlyList<string>? ChangedColumns,
    IReadOnlyDictionary<string, string?>? OldValues,
    IReadOnlyDictionary<string, string?>? NewValues,
    string? TraceId);
