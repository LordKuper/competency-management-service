using Competency.CareerFramework;
using Competency.OrgStructure;
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
    /// The statuses of an application that is still going, a draft included, in which it counts against the one-application rule.
    /// </summary>
    public static readonly IReadOnlyList<ApplicationStatus> UnfinishedStatuses = [ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.Admitted, ApplicationStatus.UnderReview];

    /// <summary>
    /// The statuses of an application the commission has before it or has dealt with.
    /// </summary>
    public static readonly IReadOnlyList<ApplicationStatus> ReviewedStatuses = [ApplicationStatus.UnderReview, ApplicationStatus.Decided, ApplicationStatus.NoDecision];

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
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

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
    /// Whether the average of the raters' answers meets the mandatory requirements of the matrix version, recorded when the rating stage ends;
    /// null until then.
    /// </summary>
    [Audited]
    public bool? MeetsAfterRating { get; set; }

    /// <summary>
    /// Whether the candidate gave no artifact for some competency that required one, recorded when the artifact stage ends.
    /// </summary>
    [Audited]
    public bool ArtifactsMissing { get; set; }

    /// <summary>
    /// Whether the application reached the commission and the members have not been told yet; the next pass of the scheduler tells them of all such applications in one letter.
    /// </summary>
    public bool ReviewLetterPending { get; set; }

    /// <summary>
    /// The decision of the chair; null while there is none. The chair may change it until the commission stage ends.
    /// </summary>
    [Audited]
    public PromotionDecision? Decision { get; set; }

    /// <summary>
    /// The account of the chair who recorded the current decision.
    /// </summary>
    [Audited]
    public Guid? DecidedBy { get; set; }

    [Audited]
    public DateTimeOffset? DecidedAt { get; set; }

    /// <summary>
    /// When the candidate marked that they received the feedback; null until they do.
    /// </summary>
    [Audited]
    public DateTimeOffset? FeedbackReceivedAt { get; set; }

    /// <summary>
    /// Whether the candidate may still withdraw the application: it is filed and going, and no decision is recorded. A draft is deleted instead.
    /// </summary>
    public bool CanBeWithdrawn => Status != ApplicationStatus.Draft && UnfinishedStatuses.Contains(Status) && Decision is null;

    /// <summary>
    /// Takes the application back to where it stands at the start of a stage the wave returns to. Only what a later stage settled is reset: the
    /// status, the formal failures and the results of the rating and artifact stages, which the end of those stages computes again. What the
    /// people entered, the decision of the chair included, stays. A candidate who met the mandatory requirements stays with the commission when
    /// the wave returns to the artifact stage. A withdrawn application stays withdrawn, and a draft stays a draft.
    /// </summary>
    /// <param name="stage">The stage the wave returns to, 1 to 6.</param>
    public void ReturnToStage(int stage)
    {
        if (Status is ApplicationStatus.Withdrawn or ApplicationStatus.Draft)
        {
            return;
        }

        if (stage <= PromotionStage.FormalCheck)
        {
            if (Status != ApplicationStatus.Submitted)
            {
                Status = ApplicationStatus.Submitted;
                FormalFailures = [];
            }
        }
        else if (ReviewedStatuses.Contains(Status))
        {
            if (stage <= PromotionStage.Rating || (stage == PromotionStage.Artifacts && MeetsAfterRating != true))
            {
                Status = ApplicationStatus.Admitted;
                ReviewLetterPending = false;
            }
            else if (Status == ApplicationStatus.NoDecision)
            {
                Status = ApplicationStatus.UnderReview;
            }
        }

        if (stage <= PromotionStage.Rating)
        {
            MeetsAfterRating = null;
        }

        if (stage <= PromotionStage.Artifacts)
        {
            ArtifactsMissing = false;
        }
    }

    /// <summary>
    /// Hands the application to the commission: under review, or decided again at once when the chair already decided before the wave went back
    /// to an earlier stage.
    /// </summary>
    public void ReachCommission()
    {
        Status = Decision is null ? ApplicationStatus.UnderReview : ApplicationStatus.Decided;
        ReviewLetterPending = true;
    }
}
