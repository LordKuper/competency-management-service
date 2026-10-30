namespace Kalibr.Promotion;

/// <summary>
/// A replacement of the raters of an application: who proposes the list, and who vouches for the addresses it gives.
/// </summary>
/// <param name="Raters">The raters the application is to have.</param>
/// <param name="Source">Who puts the new raters on the list.</param>
/// <param name="ConfirmedBy">The account of the head or the responsible HR whose changed addresses count as confirmed; null when the candidate edits.</param>
/// <param name="Now">The moment of the edit.</param>
internal sealed record RaterEdit(IReadOnlyCollection<RaterInput> Raters, RaterSource Source, Guid? ConfirmedBy, DateTimeOffset Now);
