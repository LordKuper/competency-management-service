namespace Competency.Promotion;

/// <summary>
/// A member of a commission as the API returns it; a required member cannot be removed.
/// </summary>
internal sealed record CommissionMemberResponse(Guid EmployeeId, string FullName, bool IsRequired, bool IsResponsibleHr, bool IsHead, bool IsActing)
{
    public static CommissionMemberResponse From(CommissionMember member) =>
        new(member.EmployeeId, member.FullName, member.IsRequired, member.IsResponsibleHr, member.IsHead, member.IsActing);
}
