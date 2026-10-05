using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// The responses that reject a request the account rules forbid, with reasons addressed to the administrator or the user.
/// </summary>
internal static class Rejections
{
    private const string ConflictTitle = "Операция отклонена";
    private const string InvalidTitle = "Проверьте введённые данные";
    private const string CurrentPasswordField = "currentPassword";
    private const string UserNameField = "userName";
    private const string GeneralField = "";
    private const string DefaultPasswordField = "newPassword";

    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: ConflictTitle);

    public static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>
    /// Turns a failed Identity operation into the response that fits it: a stale version, a taken name, or the problems with the input.
    /// </summary>
    /// <param name="failure">The failed result.</param>
    /// <param name="passwordField">The request field that carries the password, which password rule violations are reported against.</param>
    /// <returns>412 for a concurrent change, 409 for a taken name, otherwise 400 listing the problems by field.</returns>
    public static ProblemHttpResult From(IdentityResult failure, string passwordField = DefaultPasswordField)
    {
        if (failure.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status412PreconditionFailed);
        }

        if (failure.Errors.FirstOrDefault(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName)) is { } taken)
        {
            return Conflict(taken.Description);
        }

        var problems = failure.Errors
            .GroupBy(error => FieldOf(error.Code, passwordField))
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
        return TypedResults.Problem(new HttpValidationProblemDetails(problems)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = InvalidTitle,
        });
    }

    private static string FieldOf(string code, string passwordField) => code switch
    {
        nameof(IdentityErrorDescriber.PasswordMismatch) => CurrentPasswordField,
        nameof(IdentityErrorDescriber.InvalidUserName) => UserNameField,
        _ when code.StartsWith("Password", StringComparison.Ordinal) => passwordField,
        _ => GeneralField,
    };
}
