namespace Competency.Promotion;

/// <summary>
/// One consequence of going back to an earlier stage.
/// </summary>
/// <param name="Kind">What is reset or computed again.</param>
/// <param name="Count">The applications it touches.</param>
internal sealed record RollbackEffect(RollbackEffectKind Kind, int Count);
