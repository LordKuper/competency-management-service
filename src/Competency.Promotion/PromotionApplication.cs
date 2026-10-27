using Competency.CareerFramework;
using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// An application of an employee for a level of a track in a wave. The target is a level of the matrix version the wave bound for a unit; the
/// unit is the one whose commission reviews the application.
/// </summary>
[Audited]
internal sealed class PromotionApplication : EntityBase
{
    /// <summary>
    /// The statuses of an application that is still going, in which it counts against the one-application rule.
    /// </summary>
    public static readonly IReadOnlyList<ApplicationStatus> UnfinishedStatuses = [ApplicationStatus.Submitted, ApplicationStatus.Admitted, ApplicationStatus.UnderReview];

    [Audited]
    public Guid WaveId { get; init; }

    /// <summary>
    /// The candidate.
    /// </summary>
    [Audited]
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// The unit the matrix of the target belongs to.
    /// </summary>
    [Audited]
    public Guid OrgUnitId { get; set; }

    [Audited]
    public Guid MatrixId { get; set; }

    /// <summary>
    /// The version of the matrix the wave bound; the application is assessed by it alone.
    /// </summary>
    [Audited]
    public Guid VersionId { get; set; }

    /// <summary>
    /// The level of the version the candidate applies for.
    /// </summary>
    [Audited]
    public Guid LevelId { get; set; }

    /// <summary>
    /// The candidate's own statement of their education.
    /// </summary>
    [Audited]
    public EducationLevel Education { get; set; }

    [Audited]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    /// <summary>
    /// The date of hiring or of the last promotion, entered by the responsible HR.
    /// </summary>
    [Audited]
    public DateOnly? HiredOrPromotedOn { get; set; }

    /// <summary>
    /// Whether the interval since that date is not to be held against the application.
    /// </summary>
    [Audited]
    public bool IgnoreInterval { get; set; }

    /// <summary>
    /// The account that set <see cref="IgnoreInterval"/>.
    /// </summary>
    [Audited]
    public Guid? IgnoreIntervalBy { get; set; }

    [Audited]
    public DateTimeOffset? IgnoreIntervalAt { get; set; }

    /// <summary>
    /// The conditions of the formal check the application did not meet, recorded when the check stage ends; empty until then.
    /// </summary>
    [Audited]
    public FormalFailure[] FormalFailures { get; set; } = [];

    /// <summary>
    /// Whether the candidate may still withdraw the application.
    /// </summary>
    public bool CanBeWithdrawn => UnfinishedStatuses.Contains(Status);
}
