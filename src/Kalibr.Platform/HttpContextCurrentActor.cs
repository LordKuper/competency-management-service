using Microsoft.AspNetCore.Http;

namespace Competency.Platform;

/// <summary>
/// Reads the current actor from the claims and items of the ambient HTTP request.
/// </summary>
internal sealed class HttpContextCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public Guid? UserId => ParseGuid(FindClaim(PlatformClaims.Subject));

    public string? Role => UserId is null
        ? null
        : PlatformClaims.JournalRole(accessor.HttpContext!.User.FindAll(PlatformClaims.Role).Select(claim => claim.Value));

    public string? RequestId => accessor.HttpContext?.Items[RequestIdMiddleware.ItemKey] as string;

    public bool IsInRole(string role) => accessor.HttpContext?.User.HasClaim(PlatformClaims.Role, role) == true;

    private string? FindClaim(string claimType) => accessor.HttpContext?.User.FindFirst(claimType)?.Value;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var guid) ? guid : null;
}
