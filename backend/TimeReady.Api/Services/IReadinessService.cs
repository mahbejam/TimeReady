using TimeReady.Api.Models;
using TimeReady.Api.Models.Readiness;

namespace TimeReady.Api.Services;

/// <summary>
/// Evaluates whether an employee is ready to go on vacation.
/// </summary>
public interface IReadinessService
{
    ReadinessResult Evaluate(Employee employee);
}
