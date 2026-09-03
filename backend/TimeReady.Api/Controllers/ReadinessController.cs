using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeReady.Api.Authorization;
using TimeReady.Api.Data.Repositories;
using TimeReady.Api.Dtos;
using TimeReady.Api.Mapping;
using TimeReady.Api.Services;

namespace TimeReady.Api.Controllers;

/// <summary>
/// Readiness evaluation. The rules are applied on demand, so a result always
/// reflects the current data and the current date.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/readiness")]
[Produces("application/json")]
[Authorize(Policy = Policies.ReadEmployees)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReadinessController(
    IEmployeeRepository repository,
    IReadinessService readinessService) : ControllerBase
{
    /// <summary>Evaluates every employee. This is what the notifications page uses.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One result per employee.</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ReadinessResultDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReadinessResultDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var employees = await repository.ListAsync(cancellationToken);

        var results = employees
            .Select(employee => readinessService.Evaluate(employee).ToDto())
            .ToList();

        return Ok(results);
    }

    /// <summary>Evaluates a single stored employee.</summary>
    /// <param name="employeeId">Employee id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The readiness result.</response>
    /// <response code="404">No employee with this id exists.</response>
    [HttpGet("{employeeId:int}")]
    [ProducesResponseType<ReadinessResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadinessResultDto>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await repository.FindAsync(employeeId, tracked: false, cancellationToken);

        if (employee is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Employee not found.",
                detail: $"No employee exists with id {employeeId}.");
        }

        return Ok(readinessService.Evaluate(employee).ToDto());
    }

    /// <summary>
    /// Evaluates data that has not been saved yet. The employee form uses this
    /// to preview the effect of a change before it is stored.
    /// </summary>
    /// <param name="request">The employee data to evaluate.</param>
    /// <response code="200">The readiness result for the submitted data.</response>
    /// <response code="400">The request did not pass validation.</response>
    [HttpPost("evaluate")]
    [ProducesResponseType<ReadinessResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<ReadinessResultDto> Evaluate(EmployeeRequest request)
    {
        var result = readinessService.Evaluate(request.ToEntity());

        return Ok(result.ToDto());
    }
}
