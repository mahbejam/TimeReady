using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TimeReady.Api.Models.Auditing;
using TimeReady.Api.Services.Auditing;

namespace TimeReady.Api.Data.Auditing;

/// <summary>
/// Writes an audit entry for every insert, update and delete of an
/// <see cref="IAuditable"/> entity.
/// <para>
/// The work is split in two: the values are read before the save, because that
/// is when the original values still exist, and the rows are written after it,
/// because a generated primary key only exists then. Audit entries are not
/// auditable themselves, so the second save does not trigger the interceptor
/// again.
/// </para>
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    ICurrentUserAccessor currentUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly List<AuditDraft> _drafts = [];

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Collect(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var entries = TakeEntries();

        if (entries.Count > 0 && eventData.Context is not null)
        {
            eventData.Context.Set<AuditEntry>().AddRange(entries);
            eventData.Context.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var entries = TakeEntries();

        if (entries.Count > 0 && eventData.Context is not null)
        {
            eventData.Context.Set<AuditEntry>().AddRange(entries);
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _drafts.Clear();

        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _drafts.Clear();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Collect(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            var draft = Describe(entry);

            if (draft is not null)
            {
                _drafts.Add(draft);
            }
        }
    }

    private static AuditDraft? Describe(EntityEntry<IAuditable> entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                return new AuditDraft(entry, AuditAction.Created)
                {
                    NewValues = CurrentValues(entry)
                };

            case EntityState.Deleted:
                return new AuditDraft(entry, AuditAction.Deleted)
                {
                    EntityId = KeyOf(entry),
                    OldValues = OriginalValues(entry)
                };

            case EntityState.Modified:
                var changed = entry.Properties
                    .Where(property => property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                    .ToList();

                if (changed.Count == 0)
                {
                    return null;
                }

                return new AuditDraft(entry, AuditAction.Updated)
                {
                    EntityId = KeyOf(entry),
                    ChangedColumns = changed.Select(property => property.Metadata.Name).ToList(),
                    OldValues = changed.ToDictionary(
                        property => property.Metadata.Name,
                        property => Format(property.OriginalValue)),
                    NewValues = changed.ToDictionary(
                        property => property.Metadata.Name,
                        property => Format(property.CurrentValue))
                };

            default:
                return null;
        }
    }

    private List<AuditEntry> TakeEntries()
    {
        if (_drafts.Count == 0)
        {
            return [];
        }

        var timestamp = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;
        var userName = currentUser.UserName;
        var traceId = currentUser.TraceId;

        var entries = _drafts
            .Select(draft => draft.ToEntry(timestamp, userId, userName, traceId))
            .ToList();

        _drafts.Clear();

        return entries;
    }

    private static Dictionary<string, string?> CurrentValues(EntityEntry entry) =>
        entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => Format(property.CurrentValue));

    private static Dictionary<string, string?> OriginalValues(EntityEntry entry) =>
        entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => Format(property.OriginalValue));

    private static string KeyOf(EntityEntry entry) =>
        string.Join(
            ",",
            entry.Properties
                .Where(property => property.Metadata.IsPrimaryKey())
                .Select(property => Format(property.CurrentValue) ?? string.Empty));

    private static string? Format(object? value) => value switch
    {
        null => null,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    /// <summary>What was captured before the save, waiting for the generated key.</summary>
    private sealed class AuditDraft(EntityEntry entry, AuditAction action)
    {
        public string? EntityId { get; init; }

        public List<string>? ChangedColumns { get; init; }

        public Dictionary<string, string?>? OldValues { get; init; }

        public Dictionary<string, string?>? NewValues { get; init; }

        public AuditEntry ToEntry(
            DateTimeOffset timestamp,
            string? userId,
            string userName,
            string? traceId) => new()
        {
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = EntityId ?? KeyOf(entry),
            Action = action,
            UserId = userId,
            UserName = userName,
            TimestampUtc = timestamp,
            TraceId = traceId,
            ChangedColumns = ChangedColumns is null ? null : JsonSerializer.Serialize(ChangedColumns),
            OldValues = OldValues is null ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues is null ? null : JsonSerializer.Serialize(NewValues)
        };
    }
}
