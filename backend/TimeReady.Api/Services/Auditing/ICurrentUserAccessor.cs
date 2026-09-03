namespace TimeReady.Api.Services.Auditing;

/// <summary>Who is behind the current request, as far as the audit trail cares.</summary>
public interface ICurrentUserAccessor
{
    /// <summary>Identity user id, or null when there is no signed-in user.</summary>
    string? UserId { get; }

    /// <summary>Email of the user, or <c>system</c> outside a request.</summary>
    string UserName { get; }

    /// <summary>Trace identifier of the current request, when there is one.</summary>
    string? TraceId { get; }
}
