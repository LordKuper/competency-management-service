using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// What a caller may see: global administrators see everything, other users only active units, working employees and active positions.
/// An active unit has only active ancestors, so the visible part of the tree is always connected.
/// </summary>
internal static class OrgAccess
{
    public static bool IsGlobalAdmin(this ICurrentActor actor) => actor.Role == PlatformClaims.GlobalAdminRole;

    public static IQueryable<OrgUnit> VisibleTo(this IQueryable<OrgUnit> units, ICurrentActor actor) =>
        actor.IsGlobalAdmin() ? units : units.Where(unit => unit.IsActive);

    public static IQueryable<Employee> VisibleTo(this IQueryable<Employee> employees, ICurrentActor actor) =>
        actor.IsGlobalAdmin() ? employees : employees.Where(employee => employee.IsActive);

    public static IQueryable<Position> VisibleTo(this IQueryable<Position> positions, ICurrentActor actor) =>
        actor.IsGlobalAdmin() ? positions : positions.Where(position => position.IsActive);
}
