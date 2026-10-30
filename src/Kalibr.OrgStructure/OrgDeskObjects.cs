using Kalibr.Platform;

namespace Kalibr.OrgStructure;

/// <summary>
/// How desk findings of every module name the records of the organization structure, so that findings about one record meet in one desk item
/// and show it the same way.
/// </summary>
public static class OrgDeskObjects
{
    /// <summary>
    /// A unit as the desk shows it: its name, placed by the names of the units above it from the root down.
    /// </summary>
    /// <param name="snapshot">The organization snapshot the unit is from.</param>
    /// <param name="unit">The unit.</param>
    /// <returns>The desk object of the unit.</returns>
    public static DeskObject UnitDeskObject(this OrgSnapshot snapshot, OrgUnitStatus unit) => new(
        DeskObjectType.OrgUnit,
        unit.Id,
        unit.Name,
        [.. snapshot.Ancestry(unit.Id).Skip(1).Reverse().Select(ancestor => ancestor.Name)]);
}
