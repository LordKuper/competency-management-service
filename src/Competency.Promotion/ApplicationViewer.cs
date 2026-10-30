namespace Competency.Promotion;

/// <summary>
/// What the signed-in user is to the applications of one wave: their employee, what they are to the wave and what they are to the units of its targets.
/// </summary>
/// <param name="EmployeeId">The employee the account is bound to; null when it is bound to none.</param>
/// <param name="WaveRoles">What the user is to the wave itself.</param>
/// <param name="UnitRoles">What the user is to the units they have a role in.</param>
internal sealed record ApplicationViewer(Guid? EmployeeId, ApplicationRole WaveRoles, IReadOnlyDictionary<Guid, ApplicationRole> UnitRoles)
{
    /// <summary>
    /// Finds what the user is to an application. A candidate is only ever their own application's candidate, whatever else they are to its unit,
    /// and a draft is seen by its candidate alone.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The roles, or <see cref="ApplicationRole.None"/> when the application is not theirs to see.</returns>
    public ApplicationRole RolesOf(PromotionApplication application)
    {
        if (application.EmployeeId == EmployeeId)
        {
            return ApplicationRole.Candidate;
        }

        return application.Status == ApplicationStatus.Draft ? ApplicationRole.None : WaveRoles | UnitRoles.GetValueOrDefault(application.OrgUnitId);
    }
}
