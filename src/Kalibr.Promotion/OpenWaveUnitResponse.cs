namespace Kalibr.Promotion;

/// <summary>
/// A unit of an open wave with the track matrices its applications can aim at.
/// </summary>
/// <param name="Id">The unit.</param>
/// <param name="Name">The unit name.</param>
/// <param name="Targets">The track matrices, by track name; before the wave starts the published ones, afterwards those it is bound to.</param>
internal sealed record OpenWaveUnitResponse(Guid Id, string Name, IReadOnlyList<WaveTargetResponse> Targets);
