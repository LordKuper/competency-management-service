namespace Kalibr.Promotion;

/// <summary>
/// A member of a commission as it stands now.
/// </summary>
/// <param name="EmployeeId">The employee.</param>
/// <param name="FullName">The employee's full name.</param>
/// <param name="IsResponsibleHr">Whether the employee is the responsible HR of the unit, which makes them a member for as long as they are.</param>
/// <param name="IsHead">Whether the employee heads the unit, which makes them a member for as long as they do.</param>
/// <param name="IsActing">Whether the employee works and has an account that can act; a member who does not act cannot be the chair.</param>
internal sealed record CommissionMember(Guid EmployeeId, string FullName, bool IsResponsibleHr, bool IsHead, bool IsActing)
{
    /// <summary>
    /// Whether the member belongs to the commission by the structure of the unit and cannot be removed.
    /// </summary>
    public bool IsRequired => IsResponsibleHr || IsHead;
}
