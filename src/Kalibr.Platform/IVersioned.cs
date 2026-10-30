namespace Competency.Platform;

/// <summary>
/// An entity guarded by optimistic concurrency; its version is the HTTP entity tag.
/// </summary>
public interface IVersioned
{
    /// <summary>
    /// The number of committed updates. The platform increments it on every update; application code never assigns it.
    /// </summary>
    int Version { get; }
}
