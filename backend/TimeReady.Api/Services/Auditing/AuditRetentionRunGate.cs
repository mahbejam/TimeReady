namespace TimeReady.Api.Services.Auditing;

/// <summary>
/// Ensures only one retention run executes at a time, whether started by the
/// background job or by the Admin "run now" endpoint.
/// </summary>
public sealed class AuditRetentionRunGate
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>Runs <paramref name="action"/> exclusively; concurrent callers wait.</summary>
    public async Task<T> RunExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
}
