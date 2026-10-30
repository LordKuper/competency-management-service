namespace Kalibr.CareerFramework;

/// <summary>
/// One record that keeps a career entry from being deleted, as a refused deletion lists it.
/// </summary>
/// <param name="Kind">What the record is.</param>
/// <param name="Id">The record; for a competency version, its competency.</param>
/// <param name="Name">The record's name; for a competency version, its competency's current name; for a matrix, its track's or position's name.</param>
/// <param name="Context">Where the record sits: a matrix's unit name; null for the other kinds.</param>
internal sealed record DeletionBlocker(DeletionBlockerKind Kind, Guid Id, string Name, string? Context);
