using System.Linq.Expressions;

namespace Kalibr.OrgStructure;

/// <summary>
/// A legal entity of the directory as the API returns it.
/// </summary>
internal sealed record LegalEntityResponse(Guid Id, string Name)
{
    public static readonly Expression<Func<LegalEntity, LegalEntityResponse>> Projection = entity => new LegalEntityResponse(entity.Id, entity.Name);

    private static readonly Func<LegalEntity, LegalEntityResponse> Compiled = Projection.Compile();

    public static LegalEntityResponse From(LegalEntity entity) => Compiled(entity);
}
