using System.Linq.Expressions;

namespace Competency.OrgStructure;

/// <summary>
/// A unit as the API returns it, in lists, trees and single reads alike. The version equals the <c>ETag</c> of the single read.
/// </summary>
internal sealed record OrgUnitResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    Guid? HeadEmployeeId,
    bool IsActive,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int Version)
{
    public static readonly Expression<Func<OrgUnit, OrgUnitResponse>> Projection = unit => new OrgUnitResponse(
        unit.Id,
        unit.Name,
        unit.ParentId,
        unit.HeadEmployeeId,
        unit.IsActive,
        unit.ValidFrom,
        unit.ValidTo,
        unit.Version);

    private static readonly Func<OrgUnit, OrgUnitResponse> Compiled = Projection.Compile();

    public static OrgUnitResponse From(OrgUnit unit) => Compiled(unit);
}
