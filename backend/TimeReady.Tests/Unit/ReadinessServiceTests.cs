using Microsoft.Extensions.Options;
using TimeReady.Api.Configuration;
using TimeReady.Api.Models;
using TimeReady.Api.Models.Readiness;
using TimeReady.Api.Services;
using Xunit;

namespace TimeReady.Tests.Unit;

public class ReadinessServiceTests
{
    private static readonly DateOnly Today = new(2026, 7, 20);

    [Fact]
    public void Evaluate_ReturnsReady_WhenEverythingIsPrepared()
    {
        var result = CreateService().Evaluate(PreparedEmployee());

        Assert.True(result.IsReady);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Evaluate_BlocksReadiness_WhenManagerIsNotInformed()
    {
        var employee = PreparedEmployee();
        employee.ManagerInformed = false;

        var result = CreateService().Evaluate(employee);

        Assert.False(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.ManagerNotInformed, ReadinessSeverity.Critical);
    }

    [Fact]
    public void Evaluate_BlocksReadiness_WhenHandoverIsIncomplete()
    {
        var employee = PreparedEmployee();
        employee.HandoverCompleted = false;

        var result = CreateService().Evaluate(employee);

        Assert.False(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.HandoverIncomplete, ReadinessSeverity.Critical);
    }

    [Fact]
    public void Evaluate_BlocksReadiness_WhenTimeBalanceIsFarNegative()
    {
        var employee = PreparedEmployee();
        employee.TimeBalanceHours = -24.5m;

        var result = CreateService().Evaluate(employee);

        Assert.False(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.NegativeTimeBalance, ReadinessSeverity.Critical);
    }

    [Fact]
    public void Evaluate_StaysReady_WhenTimeBalanceIsOnlyModeratelyNegative()
    {
        var employee = PreparedEmployee();
        employee.TimeBalanceHours = -10m;

        var result = CreateService().Evaluate(employee);

        Assert.True(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.NegativeTimeBalance, ReadinessSeverity.Warning);
    }

    [Fact]
    public void Evaluate_WarnsAboutAnImminentVacation()
    {
        var employee = PreparedEmployee();
        employee.VacationStartDate = Today.AddDays(3);

        var result = CreateService().Evaluate(employee);

        Assert.True(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.VacationStartsSoon, ReadinessSeverity.Warning);
    }

    [Fact]
    public void Evaluate_DoesNotWarn_WhenVacationIsStillFarAway()
    {
        var employee = PreparedEmployee();
        employee.VacationStartDate = Today.AddDays(60);

        var result = CreateService().Evaluate(employee);

        Assert.DoesNotContain(result.Warnings, w => w.Code == ReadinessCodes.VacationStartsSoon);
    }

    [Fact]
    public void Evaluate_IsNotReady_WhenNoVacationIsPlanned()
    {
        var employee = PreparedEmployee();
        employee.VacationStartDate = null;

        var result = CreateService().Evaluate(employee);

        Assert.False(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.NoVacationPlanned, ReadinessSeverity.Info);
    }

    [Fact]
    public void Evaluate_ReportsLowRemainingVacationDays()
    {
        var employee = PreparedEmployee();
        employee.RemainingVacationDays = 1;

        var result = CreateService().Evaluate(employee);

        Assert.True(result.IsReady);
        AssertHasWarning(result, ReadinessCodes.LowVacationDays, ReadinessSeverity.Info);
    }

    [Theory]
    [InlineData(-6, false)]
    [InlineData(-5, false)]
    [InlineData(-4, true)]
    public void Evaluate_RespectsConfiguredThresholds(int balanceHours, bool expectedReady)
    {
        var options = new ReadinessOptions
        {
            CriticalNegativeBalanceHours = -5m,
            WarningNegativeBalanceHours = -2m
        };

        var employee = PreparedEmployee();
        employee.TimeBalanceHours = balanceHours;

        var result = CreateService(options).Evaluate(employee);

        Assert.Equal(expectedReady, result.IsReady);
    }

    [Fact]
    public void Evaluate_KeepsEmployeeIdentity()
    {
        var result = CreateService().Evaluate(PreparedEmployee());

        Assert.Equal(7, result.EmployeeId);
        Assert.Equal("Anna Gruber", result.FullName);
    }

    private static ReadinessService CreateService(ReadinessOptions? options = null) =>
        new(Options.Create(options ?? new ReadinessOptions()), new FixedTimeProvider(Today));

    private static Employee PreparedEmployee() => new()
    {
        Id = 7,
        FullName = "Anna Gruber",
        TimeBalanceHours = 4m,
        RemainingVacationDays = 12,
        VacationStartDate = Today.AddDays(30),
        ManagerInformed = true,
        HandoverCompleted = true
    };

    private static void AssertHasWarning(ReadinessResult result, string code, ReadinessSeverity severity)
    {
        var warning = Assert.Single(result.Warnings, w => w.Code == code);
        Assert.Equal(severity, warning.Severity);
        Assert.False(string.IsNullOrWhiteSpace(warning.Recommendation));
    }
}
