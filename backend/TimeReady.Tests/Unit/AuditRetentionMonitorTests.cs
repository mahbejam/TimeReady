using TimeReady.Api.Services.Auditing;
using Xunit;

namespace TimeReady.Tests.Unit;

public class AuditRetentionMonitorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Current_StartsEmpty()
    {
        var status = new AuditRetentionMonitor().Current;

        Assert.Equal(0, status.RunCount);
        Assert.Null(status.LastRunAtUtc);
        Assert.Null(status.LastError);
    }

    [Fact]
    public void RecordSuccess_KeepsTheCountsOfTheLastRun()
    {
        var monitor = new AuditRetentionMonitor();

        monitor.RecordSuccess(
            new AuditRetentionResult(12, 3, Now.AddDays(-90), Now.AddDays(-730), TimeSpan.FromSeconds(2)),
            Now);

        var status = monitor.Current;

        Assert.Equal(1, status.RunCount);
        Assert.Equal(0, status.FailureCount);
        Assert.Equal(12, status.LastArchived);
        Assert.Equal(3, status.LastPurged);
        Assert.Equal(Now, status.LastSuccessAtUtc);
        Assert.Null(status.LastError);
    }

    [Fact]
    public void RecordFailure_KeepsTheMessageAndCountsTheFailure()
    {
        var monitor = new AuditRetentionMonitor();

        monitor.RecordFailure(new InvalidOperationException("database is locked"), Now);

        var status = monitor.Current;

        Assert.Equal(1, status.RunCount);
        Assert.Equal(1, status.FailureCount);
        Assert.Equal("database is locked", status.LastError);
        Assert.Null(status.LastSuccessAtUtc);
    }

    [Fact]
    public void RecordSuccess_ClearsAnEarlierFailure()
    {
        var monitor = new AuditRetentionMonitor();

        monitor.RecordFailure(new InvalidOperationException("transient"), Now);
        monitor.RecordSuccess(
            new AuditRetentionResult(0, 0, Now.AddDays(-90), null, TimeSpan.Zero),
            Now.AddHours(24));

        var status = monitor.Current;

        Assert.Null(status.LastError);
        Assert.Equal(1, status.FailureCount);
        Assert.Equal(2, status.RunCount);
    }
}
