using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Competency.Platform;

/// <summary>
/// The responses that reject a change a module's rules forbid.
/// </summary>
public static class Rejection
{
    /// <summary>
    /// The title of a 409 response.
    /// </summary>
    public const string ConflictTitle = "Операция отклонена";

    /// <summary>
    /// Rejects a request that conflicts with the current state.
    /// </summary>
    /// <param name="detail">The reason, addressed to the user.</param>
    /// <returns>A 409 response.</returns>
    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: ConflictTitle);

    /// <summary>
    /// Rejects a request whose field is invalid.
    /// </summary>
    /// <param name="field">The request field.</param>
    /// <param name="message">The reason, addressed to the user.</param>
    /// <returns>A 400 response naming the field.</returns>
    public static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
