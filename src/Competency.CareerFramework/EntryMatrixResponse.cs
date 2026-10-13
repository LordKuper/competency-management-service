namespace Competency.CareerFramework;

/// <summary>
/// A matrix version that uses a catalogue entry, named by its matrix's unit and track.
/// </summary>
internal sealed record EntryMatrixResponse(
    Guid MatrixId,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid TrackId,
    string TrackName,
    Guid VersionId,
    int Number,
    MatrixVersionStatus Status);
