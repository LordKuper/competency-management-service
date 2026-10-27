using System.Text.Json.Serialization;

namespace Competency.OrgStructure.Import;

/// <summary>
/// A problem that makes the file unusable.
/// </summary>
/// <param name="Row">The row number in the sheet; null for a problem of the file or of the data as a whole.</param>
/// <param name="Column">The column name; null when no single column is at fault.</param>
/// <param name="Message">The reason, addressed to the user.</param>
internal sealed record ImportErrorResponse(int? Row, string? Column, string Message)
{
    public static ImportErrorResponse From(ImportError error) => new(error.Row, error.Column, error.Message);
}

/// <summary>
/// An active unit of the system that no path of the file names, for the user to keep or deactivate.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Path">The names from the root to the unit, joined by a slash.</param>
/// <param name="ParentId">The parent unit; null for a root.</param>
internal sealed record MissingUnitResponse(Guid Id, string Path, Guid? ParentId);

/// <summary>
/// A working employee of the system whom the file does not name, for the user to keep or dismiss.
/// </summary>
/// <param name="Id">The employee identifier.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="OrgUnitIds">The units of the employee's main and secondary assignments.</param>
/// <param name="IsLastActiveAdministrator">Whether the account bound to the employee is the last active global administrator, so that dismissing the employee is refused.</param>
internal sealed record MissingEmployeeResponse(Guid Id, string FullName, IReadOnlyList<Guid> OrgUnitIds, bool IsLastActiveAdministrator);

/// <summary>
/// What applying the file would change, by group, and what the file does not name. Every group lists one line per change.
/// </summary>
/// <param name="Fingerprint">Identifies this plan for this file; the apply request carries it back, and the apply is refused when the file or the data no longer give the same plan.</param>
/// <param name="NewUnits">The units to create, by path.</param>
/// <param name="ActivatedUnits">The inactive units the file names, which become active.</param>
/// <param name="NewPositions">The positions to add to the catalogue.</param>
/// <param name="ReactivatedPositions">The inactive catalogue positions the file names, which become active.</param>
/// <param name="NewLegalEntities">The legal entities to add to the directory.</param>
/// <param name="NewEmployees">The employees to create, with their main assignment.</param>
/// <param name="RehiredEmployees">The dismissed employees the file names, who work again.</param>
/// <param name="Transfers">The employees whose main assignment changes.</param>
/// <param name="AddedAssignments">The secondary assignments to add.</param>
/// <param name="RemovedAssignments">The secondary assignments to remove.</param>
/// <param name="ChangedLegalEntities">The assignments whose legal entities change.</param>
/// <param name="UnitPositionsAdded">The positions to put on units' own lists.</param>
/// <param name="UnitPositionsRemoved">The positions to take off units' own lists.</param>
/// <param name="MissingUnits">The active units the file does not name.</param>
/// <param name="MissingEmployees">The working employees the file does not name.</param>
/// <param name="Heads">The units of the file with head candidates or a head, and the employees they refer to.</param>
internal sealed record OrgImportPlanResponse(
    string Fingerprint,
    IReadOnlyList<string> NewUnits,
    IReadOnlyList<string> ActivatedUnits,
    IReadOnlyList<string> NewPositions,
    IReadOnlyList<string> ReactivatedPositions,
    IReadOnlyList<string> NewLegalEntities,
    IReadOnlyList<string> NewEmployees,
    IReadOnlyList<string> RehiredEmployees,
    IReadOnlyList<string> Transfers,
    IReadOnlyList<string> AddedAssignments,
    IReadOnlyList<string> RemovedAssignments,
    IReadOnlyList<string> ChangedLegalEntities,
    IReadOnlyList<string> UnitPositionsAdded,
    IReadOnlyList<string> UnitPositionsRemoved,
    IReadOnlyList<MissingUnitResponse> MissingUnits,
    IReadOnlyList<MissingEmployeeResponse> MissingEmployees,
    ImportHeadsResponse Heads);

/// <summary>
/// An employee a head step refers to, named once however many units list them.
/// </summary>
/// <param name="Key">The employee key: the identifier of an existing employee, or the normalised full name of one the import creates.</param>
/// <param name="FullName">The employee's full name.</param>
internal sealed record ImportHeadEmployeeResponse(string Key, string FullName);

/// <summary>
/// A unit of the file that has head candidates or a head, for the user to choose its head.
/// </summary>
/// <param name="Key">The unit key the head decision names; opaque.</param>
/// <param name="Path">The names from the root to the unit, joined by a slash.</param>
/// <param name="UnitId">The unit identifier; null for a unit the import creates.</param>
/// <param name="CurrentHead">The key of the unit's head now; null when it has none.</param>
/// <param name="CurrentHeadStays">Whether the file keeps the current head working in the unit. False for a head the file does not name, whom the decision about the missing employees may still keep.</param>
/// <param name="Candidates">The keys of the file's employees in the unit whose position name starts with a head word.</param>
/// <param name="Employees">The keys of every employee of the file with an assignment in the unit, the main or a secondary one; a missing employee the user keeps is not listed, the missing list names their units.</param>
/// <param name="Suggested">The key of the default head, ignoring the decisions about missing employees: the current head when the file keeps them in the unit, otherwise the only candidate; null when the unit is left without a head or has several candidates.</param>
internal sealed record ImportHeadUnitResponse(
    string Key,
    string Path,
    Guid? UnitId,
    string? CurrentHead,
    bool CurrentHeadStays,
    IReadOnlyList<string> Candidates,
    IReadOnlyList<string> Employees,
    string? Suggested);

/// <summary>
/// The head step of the preview.
/// </summary>
/// <param name="Units">The units of the file with candidates or a head, in file order.</param>
/// <param name="Employees">Every employee the units refer to, by key.</param>
internal sealed record ImportHeadsResponse(IReadOnlyList<ImportHeadUnitResponse> Units, IReadOnlyList<ImportHeadEmployeeResponse> Employees);

/// <summary>
/// The user's choice of head for one unit of the head step.
/// </summary>
/// <param name="Unit">The unit key from the preview.</param>
/// <param name="Employee">The employee key from the preview, or the identifier of a missing employee the user keeps; null for no head.</param>
internal sealed record ImportHeadDecisionRequest(string Unit, string? Employee);

/// <summary>
/// The outcome of reading a file: the errors that make it unusable, or the plan when there are none.
/// </summary>
/// <param name="Errors">Every problem found; empty when the file is usable.</param>
/// <param name="Plan">What applying the file would change; null when there are errors.</param>
internal sealed record OrgImportPreviewResponse(IReadOnlyList<ImportErrorResponse> Errors, OrgImportPlanResponse? Plan);

/// <summary>
/// The decision for one entity the file does not name.
/// </summary>
/// <param name="Id">The identifier of a missing unit or a missing employee.</param>
/// <param name="Decision">Keep for either kind, deactivate for a unit, dismiss for an employee.</param>
internal sealed record ImportDecisionRequest(Guid Id, ImportDecision Decision);

/// <summary>
/// What the user decides for a unit or an employee the file does not name. Member names are sent in the API.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ImportDecision>))]
internal enum ImportDecision
{
    /// <summary>
    /// Leave the unit or the employee as they are.
    /// </summary>
    Keep,

    /// <summary>
    /// Deactivate the unit.
    /// </summary>
    Deactivate,

    /// <summary>
    /// Dismiss the employee, with the cascade of a manual dismissal.
    /// </summary>
    Dismiss,
}

/// <summary>
/// The changes an applied import made, by group.
/// </summary>
/// <param name="NewUnits">The units created.</param>
/// <param name="ActivatedUnits">The units activated.</param>
/// <param name="NewPositions">The positions added to the catalogue.</param>
/// <param name="ReactivatedPositions">The positions activated.</param>
/// <param name="NewLegalEntities">The legal entities added to the directory.</param>
/// <param name="NewEmployees">The employees created.</param>
/// <param name="RehiredEmployees">The employees who work again.</param>
/// <param name="Transfers">The employees whose main assignment changed.</param>
/// <param name="AddedAssignments">The secondary assignments added.</param>
/// <param name="RemovedAssignments">The secondary assignments removed.</param>
/// <param name="ChangedLegalEntities">The assignments whose legal entities changed.</param>
/// <param name="UnitPositionsAdded">The positions put on units' own lists.</param>
/// <param name="UnitPositionsRemoved">The positions taken off units' own lists.</param>
/// <param name="DeactivatedUnits">The units deactivated by decision.</param>
/// <param name="DismissedEmployees">The employees dismissed by decision.</param>
/// <param name="AssignedHeads">The units given a head by decision.</param>
internal sealed record OrgImportResultResponse(
    int NewUnits,
    int ActivatedUnits,
    int NewPositions,
    int ReactivatedPositions,
    int NewLegalEntities,
    int NewEmployees,
    int RehiredEmployees,
    int Transfers,
    int AddedAssignments,
    int RemovedAssignments,
    int ChangedLegalEntities,
    int UnitPositionsAdded,
    int UnitPositionsRemoved,
    int DeactivatedUnits,
    int DismissedEmployees,
    int AssignedHeads)
{
    public static OrgImportResultResponse From(OrgImportPlanResponse plan, int deactivatedUnits, int dismissedEmployees, int assignedHeads) => new(
        plan.NewUnits.Count,
        plan.ActivatedUnits.Count,
        plan.NewPositions.Count,
        plan.ReactivatedPositions.Count,
        plan.NewLegalEntities.Count,
        plan.NewEmployees.Count,
        plan.RehiredEmployees.Count,
        plan.Transfers.Count,
        plan.AddedAssignments.Count,
        plan.RemovedAssignments.Count,
        plan.ChangedLegalEntities.Count,
        plan.UnitPositionsAdded.Count,
        plan.UnitPositionsRemoved.Count,
        deactivatedUnits,
        dismissedEmployees,
        assignedHeads);
}
