namespace Competency.Platform;

/// <summary>
/// The values a desk finding's reason names; the client words the reason from its kind, so each kind fills only the values it names.
/// </summary>
/// <param name="EmployeeCount">The number of working employees the reason counts.</param>
/// <param name="Step">The step the reason names: of the matrix builder, or of the approval route.</param>
/// <param name="Person">The employee the reason names, such as a unit head, a responsible HR or whoever holds the turn.</param>
/// <param name="AccountState">Why the account the reason concerns cannot act.</param>
/// <param name="LinkState">Why an invitation link no longer works or soon will not.</param>
/// <param name="Entries">The other records the reason names, in the order it names them.</param>
public sealed record DeskDetails(
    int? EmployeeCount = null,
    int? Step = null,
    DeskObject? Person = null,
    DeskAccountState? AccountState = null,
    DeskLinkState? LinkState = null,
    IReadOnlyList<DeskObject>? Entries = null);
