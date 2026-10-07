using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Competency.Audit;

/// <summary>
/// Journals every create, update and delete of an <see cref="AuditedAttribute"/> entity in the same save,
/// so the journal row commits or fails with the change. Keys must be assigned before the save, and bulk <c>ExecuteUpdate</c>
/// or <c>ExecuteDelete</c> bypass the change tracker and so the journal: they must not be used on audited entities.
/// </summary>
internal sealed class AuditSaveChangesInterceptor(AuditEventFactory factory) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions ValuesJson = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Record(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Record(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var events = context.ChangeTracker.Entries()
            .Select(Describe)
            .OfType<AuditEntry>()
            .Select(factory.Create)
            .ToList();
        context.Set<AuditEvent>().AddRange(events);
    }

    private static AuditEntry? Describe(EntityEntry entry)
    {
        var verb = entry.State switch
        {
            EntityState.Added => "Created",
            EntityState.Modified => "Updated",
            EntityState.Deleted => "Deleted",
            _ => null,
        };
        if (verb is null || !Attribute.IsDefined(entry.Metadata.ClrType, typeof(AuditedAttribute)))
        {
            return null;
        }

        var recorded = entry.Properties.Where(IsAllowListed).ToList();
        var changed = entry.State == EntityState.Modified ? recorded.Where(property => property.IsModified).ToList() : recorded;
        if (entry.State == EntityState.Modified && changed.Count == 0)
        {
            return null;
        }

        var entityType = entry.Metadata.ClrType.Name;
        return new AuditEntry(
            $"{entityType}.{verb}",
            entityType,
            GetEntityId(entry),
            OldValue: entry.State == EntityState.Added ? null : ToJson(changed, property => property.OriginalValue),
            NewValue: entry.State == EntityState.Deleted ? null : ToJson(changed, property => property.CurrentValue));
    }

    private static bool IsAllowListed(PropertyEntry property) =>
        property.Metadata.PropertyInfo is { } info && Attribute.IsDefined(info, typeof(AuditedAttribute));

    private static string GetEntityId(EntityEntry entry) => string.Join(
        ',',
        entry.Properties
            .Where(property => property.Metadata.IsPrimaryKey())
            .Select(property => Convert.ToString(property.CurrentValue, CultureInfo.InvariantCulture)));

    private static string? ToJson(IReadOnlyCollection<PropertyEntry> properties, Func<PropertyEntry, object?> selectValue) =>
        properties.Count == 0
            ? null
            : JsonSerializer.Serialize(properties.ToDictionary(property => property.Metadata.Name, selectValue), ValuesJson);
}
