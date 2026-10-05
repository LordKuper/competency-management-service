using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Competency.Audit;

/// <summary>
/// Contributes the <see cref="AuditEvent"/> table to the shared model: snake_case names, a client-generated Guid v4 key
/// and one index per journal filter, each followed by the time so a filtered page is read already ordered.
/// </summary>
internal sealed class AuditEntityConfiguration : IEntityConfigurationContributor
{
    private const int ActorMaxLength = 256;
    private const int RoleMaxLength = 64;
    private const int ActionMaxLength = 128;
    private const int EntityTypeMaxLength = 128;
    private const int EntityIdMaxLength = 128;
    private const int RequestIdMaxLength = 64;

    public void Configure(ModelBuilder modelBuilder) => modelBuilder.Entity<AuditEvent>(entity =>
    {
        entity.ToTable("audit_events");
        entity.Property(e => e.Id).HasColumnName("id").HasValueGenerator<GuidValueGenerator>();
        entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        entity.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(ActorMaxLength);
        entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(RoleMaxLength);
        entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(ActionMaxLength);
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(EntityTypeMaxLength);
        entity.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(EntityIdMaxLength);
        entity.Property(e => e.OldValue).HasColumnName("old_value").HasColumnType("jsonb");
        entity.Property(e => e.NewValue).HasColumnName("new_value").HasColumnType("jsonb");
        entity.Property(e => e.Reason).HasColumnName("reason");
        entity.Property(e => e.RequestId).HasColumnName("request_id").HasMaxLength(RequestIdMaxLength);

        entity.HasIndex(e => new { e.Timestamp, e.Id }).IsDescending(true, false);
        entity.HasIndex(e => new { e.Actor, e.Timestamp }).IsDescending(false, true);
        entity.HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp }).IsDescending(false, false, true);
        entity.HasIndex(e => e.RequestId);
    });
}
