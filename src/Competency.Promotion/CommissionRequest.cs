namespace Competency.Promotion;

/// <summary>
/// The commission a unit is to have: the members besides those it always has, and the chair. Both are replaced.
/// </summary>
internal sealed record CommissionRequest
{
    public const int MaxMembers = 100;

    public required Guid[] MemberEmployeeIds { get; init; }

    /// <summary>
    /// The chair, a given member or a required one; null for none.
    /// </summary>
    public Guid? ChairEmployeeId { get; init; }

    /// <summary>
    /// Checks the size of the member list.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() =>
        MemberEmployeeIds is { Length: <= MaxMembers }
            ? []
            : new Dictionary<string, string[]> { ["memberEmployeeIds"] = [$"Укажите не больше {MaxMembers} членов"] };
}
