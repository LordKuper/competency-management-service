namespace Kalibr.Promotion;

/// <summary>
/// An application to file: its content and the wave it goes to.
/// </summary>
internal sealed record NewApplicationRequest : ApplicationRequest
{
    public required Guid WaveId { get; init; }
}
