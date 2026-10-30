using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Competency.CareerFramework;

/// <summary>
/// The 412 response to a publishing action whose <c>?cascade=</c> tag is missing or no longer matches what it would publish. The address
/// of the fresh preview tells it apart from a 412 for a stale <c>If-Match</c>, which carries none.
/// </summary>
internal sealed class PublicationChanged : ProblemDetails
{
    private const string ChangedMessage = "Перечень публикуемого изменился — подтвердите заново";

    /// <summary>
    /// The address of the fresh preview of the root; absent when the precondition that failed is <c>If-Match</c>.
    /// </summary>
    public string? Publication { get; init; }

    /// <summary>
    /// Refuses the action and points to the fresh preview.
    /// </summary>
    /// <param name="publication">The address of the root's preview.</param>
    /// <returns>A 412 response; nothing has changed.</returns>
    public static ProblemHttpResult Refuse(string publication) => TypedResults.Problem(new PublicationChanged
    {
        Status = StatusCodes.Status412PreconditionFailed,
        Detail = ChangedMessage,
        Publication = publication,
    });
}
