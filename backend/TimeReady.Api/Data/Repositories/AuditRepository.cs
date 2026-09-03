using Microsoft.EntityFrameworkCore;
using TimeReady.Api.Dtos;
using TimeReady.Api.Dtos.Auditing;
using TimeReady.Api.Models.Auditing;

namespace TimeReady.Api.Data.Repositories;

/// <inheritdoc cref="IAuditRepository" />
public sealed class AuditRepository(AppDbContext context) : IAuditRepository
{
    /// <inheritdoc />
    public Task<PagedResult<AuditEntry>> SearchAsync(
        AuditQueryParameters parameters,
        CancellationToken cancellationToken) =>
        SearchAsync(context.AuditEntries.AsNoTracking(), parameters, cancellationToken);

    /// <inheritdoc />
    public Task<PagedResult<AuditArchiveEntry>> SearchArchiveAsync(
        AuditQueryParameters parameters,
        CancellationToken cancellationToken) =>
        SearchAsync(context.AuditArchiveEntries.AsNoTracking(), parameters, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountLiveAsync(CancellationToken cancellationToken) =>
        context.AuditEntries.CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountArchivedAsync(CancellationToken cancellationToken) =>
        context.AuditArchiveEntries.CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<AuditEntry?> FindAsync(long id, CancellationToken cancellationToken) =>
        context.AuditEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    private static async Task<PagedResult<T>> SearchAsync<T>(
        IQueryable<T> source,
        AuditQueryParameters parameters,
        CancellationToken cancellationToken)
        where T : class, IAuditSearchRow
    {
        var page = Math.Max(parameters.Page, 1);
        var pageSize = Math.Clamp(parameters.PageSize, 1, AuditQueryParameters.MaxPageSize);

        var query = ApplyFilters(source, parameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(entry => entry.TimestampUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    private static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, AuditQueryParameters parameters)
        where T : class, IAuditSearchRow
    {
        if (!string.IsNullOrWhiteSpace(parameters.EntityName))
        {
            query = query.Where(entry => entry.EntityName == parameters.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(parameters.EntityId))
        {
            query = query.Where(entry => entry.EntityId == parameters.EntityId);
        }

        if (parameters.Action is not null)
        {
            query = query.Where(entry => entry.Action == parameters.Action);
        }

        if (!string.IsNullOrWhiteSpace(parameters.User))
        {
            // LIKE is case sensitive on PostgreSQL, so both sides are lowered.
            // ILIKE would be shorter but would tie the query to one provider.
            var term = parameters.User.Trim().ToLowerInvariant();

            query = query.Where(entry =>
                EF.Functions.Like(entry.UserName.ToLower(), $"%{term}%") ||
                (entry.UserId != null && entry.UserId == parameters.User));
        }

        if (parameters.From is not null)
        {
            query = query.Where(entry => entry.TimestampUtc >= parameters.From);
        }

        if (parameters.To is not null)
        {
            query = query.Where(entry => entry.TimestampUtc <= parameters.To);
        }

        return query;
    }
}
