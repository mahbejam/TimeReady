using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeReady.Api.Authorization;
using TimeReady.Api.Data.Repositories;
using TimeReady.Api.Dtos;
using TimeReady.Api.Dtos.Auditing;
using TimeReady.Api.Mapping;

namespace TimeReady.Api.Controllers;

/// <summary>
/// The audit trail: who changed what, and when. Entries are written by the
/// database interceptor and are read-only – there is no endpoint that edits or
/// deletes them.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/audit")]
[Produces("application/json")]
[Authorize(Policy = Policies.ReadAuditTrail)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AuditController(IAuditRepository repository) : ControllerBase
{
    /// <summary>Searches the audit trail, newest entry first.</summary>
    /// <param name="parameters">Filter and paging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One page of audit entries.</response>
    /// <response code="400">The paging or date filter is out of range.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> Search(
        [FromQuery] AuditQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var page = await repository.SearchAsync(parameters, cancellationToken);

        return Ok(ToDtoPage(page, entry => entry.ToDto()));
    }

    /// <summary>Searches archived entries, newest first.</summary>
    /// <param name="parameters">Filter and paging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One page of archived audit entries.</response>
    /// <response code="400">The paging or date filter is out of range.</response>
    [HttpGet("archive")]
    [ProducesResponseType<PagedResult<ArchivedAuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ArchivedAuditEntryDto>>> SearchArchive(
        [FromQuery] AuditQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var page = await repository.SearchArchiveAsync(parameters, cancellationToken);

        return Ok(ToDtoPage(page, entry => entry.ToDto()));
    }

    /// <summary>Returns a single audit entry.</summary>
    /// <param name="id">Entry id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The audit entry.</response>
    /// <response code="404">No entry with this id exists.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType<AuditEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditEntryDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var entry = await repository.FindAsync(id, cancellationToken);

        if (entry is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Audit entry not found.",
                detail: $"No audit entry exists with id {id}.");
        }

        return Ok(entry.ToDto());
    }

    /// <summary>Returns the change history of one employee, newest first.</summary>
    /// <param name="employeeId">Employee id.</param>
    /// <param name="page">One-based page number.</param>
    /// <param name="pageSize">Entries per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One page of audit entries for that employee.</response>
    /// <response code="400">The paging options are out of range.</response>
    [HttpGet("employees/{employeeId:int}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> GetEmployeeHistory(
        int employeeId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AuditQueryParameters.DefaultPageSize)
    {
        var parameters = new AuditQueryParameters
        {
            EntityName = nameof(Models.Employee),
            EntityId = employeeId.ToString(),
            Page = page,
            PageSize = pageSize
        };

        var result = await repository.SearchAsync(parameters, cancellationToken);

        return Ok(ToDtoPage(result, entry => entry.ToDto()));
    }

    private static PagedResult<TDto> ToDtoPage<TEntity, TDto>(
        PagedResult<TEntity> page,
        Func<TEntity, TDto> map) =>
        new(
            page.Items.Select(map).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
}
