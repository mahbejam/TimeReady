using TimeReady.Api.Dtos;
using TimeReady.Api.Models.Readiness;

namespace TimeReady.Api.Mapping;

public static class ReadinessMappings
{
    public static ReadinessResultDto ToDto(this ReadinessResult result) => new(
        result.EmployeeId,
        result.FullName,
        result.IsReady,
        result.IsReady ? "Ready" : "Not Ready",
        result.Warnings.Select(w => w.ToDto()).ToList());

    public static ReadinessWarningDto ToDto(this ReadinessWarning warning) => new(
        warning.Code,
        warning.Severity.ToString(),
        warning.Message,
        warning.Recommendation);
}
