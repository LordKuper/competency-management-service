using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// What a caller may see: global administrators and HR see everything, other users only active units, working employees and active positions.
/// An active unit has only active ancestors, so the visible part of the tree is always connected.
/// </summary>
internal static class OrgAccess
{
    /// <summary>
    /// Whether the caller sees the whole org structure, inactive and dismissed included: a global administrator or an HR, who reads but does not change it.
    /// </summary>
    /// <param name="actor">The caller.</param>
    /// <returns><see langword="true"/> when nothing is hidden from the caller.</returns>
    public static bool SeesAll(this ICurrentActor actor) => actor.IsGlobalAdmin() || actor.IsHr();

    public static IQueryable<OrgUnit> VisibleTo(this IQueryable<OrgUnit> units, ICurrentActor actor) =>
        actor.SeesAll() ? units : units.Where(unit => unit.IsActive);

    public static IQueryable<Employee> VisibleTo(this IQueryable<Employee> employees, ICurrentActor actor) =>
        actor.SeesAll() ? employees : employees.Where(employee => employee.IsActive);

    public static IQueryable<Position> VisibleTo(this IQueryable<Position> positions, ICurrentActor actor) =>
        actor.SeesAll() ? positions : positions.Where(position => position.IsActive);
}
