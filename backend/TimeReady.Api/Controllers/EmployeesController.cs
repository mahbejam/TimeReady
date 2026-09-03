using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeReady.Api.Authorization;
using TimeReady.Api.Data.Repositories;
using TimeReady.Api.Dtos;
using TimeReady.Api.Mapping;

namespace TimeReady.Api.Controllers;

/// <summary>
/// CRUD endpoints for employee records.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/employees")]
[Produces("application/json")]
[Authorize(Policy = Policies.ReadEmployees)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class EmployeesController(IEmployeeRepository repository) : ControllerBase
{
    /// <summary>Returns all employees, upcoming vacations first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The employee list.</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<EmployeeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await repository.ListAsync(cancellationToken);

        return Ok(employees.Select(employee => employee.ToDto()));
    }

    /// <summary>Returns a single employee.</summary>
    /// <param name="id">Employee id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The employee.</response>
    /// <response code="404">No employee with this id exists.</response>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var employee = await repository.FindAsync(id, tracked: false, cancellationToken);

        return employee is null ? EmployeeNotFound(id) : Ok(employee.ToDto());
    }

    /// <summary>Creates a new employee.</summary>
    /// <param name="request">The employee data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">The employee was created.</response>
    /// <response code="400">The request did not pass validation.</response>
    [HttpPost]
    [Authorize(Policy = Policies.ManageEmployees)]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeDto>> Create(
        EmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var employee = request.ToEntity();

        repository.Add(employee);
        await repository.SaveChangesAsync(cancellationToken);

        return CreatedAtRoute(nameof(GetById), new { id = employee.Id }, employee.ToDto());
    }

    /// <summary>Updates an existing employee.</summary>
    /// <param name="id">Employee id.</param>
    /// <param name="request">The new employee data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">The employee was updated.</response>
    /// <response code="400">The request did not pass validation.</response>
    /// <response code="404">No employee with this id exists.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.UpdateEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        EmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var employee = await repository.FindAsync(id, tracked: true, cancellationToken);

        if (employee is null)
        {
            return EmployeeNotFound(id);
        }

        request.ApplyTo(employee);
        await repository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>Deletes an employee.</summary>
    /// <param name="id">Employee id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">The employee was deleted.</response>
    /// <response code="404">No employee with this id exists.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.ManageEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var employee = await repository.FindAsync(id, tracked: true, cancellationToken);

        if (employee is null)
        {
            return EmployeeNotFound(id);
        }

        repository.Remove(employee);
        await repository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private ObjectResult EmployeeNotFound(int id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Employee not found.",
        detail: $"No employee exists with id {id}.");
}
