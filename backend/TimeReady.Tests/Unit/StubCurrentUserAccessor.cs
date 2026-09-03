using TimeReady.Api.Services.Auditing;

namespace TimeReady.Tests.Unit;

/// <summary>Fixed identity for the interceptor tests.</summary>
internal sealed class StubCurrentUserAccessor(
    string? userId = "user-1",
    string userName = "anna@timeready.test",
    string? traceId = "trace-1") : ICurrentUserAccessor
{
    public string? UserId => userId;

    public string UserName => userName;

    public string? TraceId => traceId;
}
