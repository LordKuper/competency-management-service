using Microsoft.AspNetCore.Mvc;

namespace Kalibr.OrgStructure;

/// <summary>
/// The 409 response to deleting a unit that something still refers to: the reason and one line per kind of referring record.
/// </summary>
internal sealed class UnitDeletionRefusal : ProblemDetails
{
    /// <summary>
    /// What keeps the unit from being deleted, addressed to the user.
    /// </summary>
    public required IReadOnlyList<string> Obstacles { get; init; }
}
