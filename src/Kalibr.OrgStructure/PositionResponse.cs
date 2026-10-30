using System.Linq.Expressions;

namespace Competency.OrgStructure;

/// <summary>
/// A catalogue position as the API returns it. The version equals the <c>ETag</c> a change returns and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record PositionResponse(Guid Id, string Name, bool IsActive, int Version)
{
    public static readonly Expression<Func<Position, PositionResponse>> Projection = position => new PositionResponse(
        position.Id,
        position.Name,
        position.IsActive,
        position.Version);

    private static readonly Func<Position, PositionResponse> Compiled = Projection.Compile();

    public static PositionResponse From(Position position) => Compiled(position);
}
