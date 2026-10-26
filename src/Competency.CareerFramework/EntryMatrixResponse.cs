namespace Competency.CareerFramework;

/// <summary>
/// A matrix version that uses a catalogue entry, named by its matrix's unit and its track or, for a position matrix, its position; the
/// fields of the other kind are null.
/// </summary>
internal sealed record EntryMatrixResponse(
    Guid MatrixId,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid? TrackId,
    string? TrackName,
    Guid VersionId,
    int Number,
    MatrixVersionStatus Status,
    Guid? PositionId = null,
    string? PositionName = null);
