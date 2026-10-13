using Competency.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Competency.CareerFramework;

/// <summary>
/// The 409 response to making a career entry active, or to a change that would leave an active one unfit, while entries keep it from
/// being active: the reason as a heading, the first entries in the way and how many others there are.
/// </summary>
internal sealed class PublicationRefusal : ProblemDetails
{
    private const int ShownLimit = 5;

    /// <summary>
    /// At most five entries in the way, each with its reason.
    /// </summary>
    public required IReadOnlyList<PublicationBlocker> Blockers { get; init; }

    /// <summary>
    /// The number of entries in the way not listed in <see cref="Blockers"/>.
    /// </summary>
    public required int MoreCount { get; init; }

    /// <summary>
    /// The refusal listing the first entries in the way; the caller passes only entries the user can read.
    /// </summary>
    /// <param name="detail">The heading of the refusal, addressed to the user.</param>
    /// <param name="blockers">Every entry in the way.</param>
    /// <returns>A 409 response.</returns>
    public static ProblemHttpResult Refuse(string detail, IReadOnlyCollection<PublicationBlocker> blockers) => TypedResults.Problem(new PublicationRefusal
    {
        Status = StatusCodes.Status409Conflict,
        Title = Rejection.ConflictTitle,
        Detail = detail,
        Blockers = [.. blockers.Take(ShownLimit)],
        MoreCount = Math.Max(0, blockers.Count - ShownLimit),
    });
}
