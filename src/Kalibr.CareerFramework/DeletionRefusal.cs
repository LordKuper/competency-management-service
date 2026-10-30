using Microsoft.AspNetCore.Mvc;

namespace Competency.CareerFramework;

/// <summary>
/// The 409 response to deleting a career entry something still refers to: the reason, the first referring records the user can read,
/// and how many other records refer to it, the ones the user cannot read included.
/// </summary>
internal sealed class DeletionRefusal : ProblemDetails
{
    /// <summary>
    /// At most five referring records the user can read; empty when the refusal is not about references.
    /// </summary>
    public required IReadOnlyList<DeletionBlocker> Blockers { get; init; }

    /// <summary>
    /// The number of referring records not listed in <see cref="Blockers"/>.
    /// </summary>
    public required int MoreCount { get; init; }
}
