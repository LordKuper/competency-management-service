namespace Competency.Platform;

/// <summary>
/// Base of module aggregates: a random identifier, an optimistic-concurrency version and the creation time.
/// </summary>
public abstract class EntityBase : IVersioned
{
    /// <summary>
    /// The identifier; a random Guid v4 assigned when the entity is first tracked, so identifiers are not guessable or sequential.
    /// </summary>
    public Guid Id { get; init; }

    /// <inheritdoc />
    public int Version { get; private set; }

    /// <summary>
    /// The UTC time the entity was first saved.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
