using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TimeReady.Api.Authorization;
using TimeReady.Api.Configuration;
using TimeReady.Api.Data.Repositories;
using TimeReady.Api.Dtos.Auditing;
using TimeReady.Api.Mapping;
using TimeReady.Api.Services.Auditing;

namespace TimeReady.Api.Controllers;

/// <summary>
/// The audit retention policy: what it is configured to do, how the background
/// job is doing, and a way to run it immediately.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/audit/retention")]
[Produces("application/json")]
[Authorize(Policy = Policies.ReadAuditTrail)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AuditRetentionController(
    IOptions<AuditRetentionOptions> options,
    IAuditRetentionMonitor monitor,
    IAuditRetentionService retentionService,
    IAuditRepository repository,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Returns the configured policy, the job status and the table sizes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The retention overview.</response>
    [HttpGet]
    [ProducesResponseType<AuditRetentionOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditRetentionOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var live = await repository.CountLiveAsync(cancellationToken);
        var archived = await repository.CountArchivedAsync(cancellationToken);

        return Ok(new AuditRetentionOverviewDto(
            options.Value.ToDto(),
            monitor.Current.ToDto(),
            live,
            archived));
    }

    /// <summary>
    /// Runs the retention policy immediately instead of waiting for the next
    /// tick. Useful after changing the configuration, and for verifying the
    /// policy on a copy of the data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">What the run archived and purged.</response>
    [HttpPost("run")]
    [ProducesResponseType<AuditRetentionRunDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditRetentionRunDto>> Run(CancellationToken cancellationToken)
    {
        var result = await retentionService.RunAsync(cancellationToken);

        monitor.RecordSuccess(result, timeProvider.GetUtcNow());

        return Ok(result.ToDto());
    }
}
