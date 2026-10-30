using Kalibr.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kalibr.CareerFramework;

/// <summary>
/// The 409 response to making a career entry active, or to a change that would leave an active one unfit, while entries keep it from
/// being active: the reason as a heading, the first entries in the way the user can read and how many others there are.
/// </summary>
internal sealed class PublicationRefusal : ProblemDetails
{
    /// <summary>
    /// How many entries in the way a refusal or a preview lists at most.
    /// </summary>
    public const int ShownLimit = 5;

    /// <summary>
    /// At most five entries in the way the user can read, each with its reason; empty when the refusal is not about other entries.
    /// </summary>
    public required IReadOnlyList<PublicationBlocker> Blockers { get; init; }

    /// <summary>
    /// The number of entries in the way not listed in <see cref="Blockers"/>, the ones the user cannot read included.
    /// </summary>
    public required int MoreCount { get; init; }

    /// <summary>
    /// The refusal listing the first entries in the way.
    /// </summary>
    /// <param name="detail">The heading of the refusal, addressed to the user.</param>
    /// <param name="visible">Every entry in the way the user can read.</param>
    /// <param name="hiddenCount">How many entries in the way the user cannot read.</param>
    /// <returns>A 409 response.</returns>
    public static ProblemHttpResult Refuse(string detail, IReadOnlyCollection<PublicationBlocker> visible, int hiddenCount = 0) =>
        TypedResults.Problem(new PublicationRefusal
        {
            Status = StatusCodes.Status409Conflict,
            Title = Rejection.ConflictTitle,
            Detail = detail,
            Blockers = [.. visible.Take(ShownLimit)],
            MoreCount = Math.Max(0, visible.Count - ShownLimit) + hiddenCount,
        });
}
