using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Competency.Platform;

/// <summary>
/// Turns every unhandled exception into a ProblemDetails response and logs only its type, root type, database error code
/// and, for a server error, its stack trace, never its message, which may carry personal data or database values.
/// </summary>
internal sealed class ProblemExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemExceptionHandler> logger) : IExceptionHandler
{
    private const string ForeignKeyViolation = "23503";
    private const string UniqueViolation = "23505";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var sqlState = FindPostgresException(exception)?.SqlState;
        var status = ResolveStatus(exception, sqlState);
        var isServerError = status >= StatusCodes.Status500InternalServerError;

        logger.Log(
            isServerError ? LogLevel.Error : LogLevel.Warning,
            "Request failed with status {Status}: {ExceptionType}, root {RootExceptionType}, sqlstate {SqlState}, stack {StackTrace}",
            status,
            exception.GetType().FullName,
            exception.GetBaseException().GetType().FullName,
            sqlState,
            isServerError ? exception.StackTrace : null);

        httpContext.Response.StatusCode = status;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = status },
        });
        return true;
    }

    private static PostgresException? FindPostgresException(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;

    private static int ResolveStatus(Exception exception, string? sqlState) => exception switch
    {
        DbUpdateConcurrencyException => StatusCodes.Status412PreconditionFailed,
        BadHttpRequestException badRequest => badRequest.StatusCode,
        _ when sqlState is UniqueViolation or ForeignKeyViolation => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };
}
