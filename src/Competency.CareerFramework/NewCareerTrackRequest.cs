namespace Competency.CareerFramework;

/// <summary>
/// A new career track: its name, unit and direction, of which only the name may change afterwards.
/// </summary>
internal sealed record NewCareerTrackRequest : NewCatalogEntryRequest
{
    public required Guid DirectionId { get; init; }
}
