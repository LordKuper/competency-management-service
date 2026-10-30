namespace Competency.Promotion;

/// <summary>
/// The numbers of the stages of a wave, in their order. The end of a stage is the deadline of its work, which may be done earlier. The results are
/// published at the instant the commission stage ends, so the feedback stage is the first one in which a candidate sees the result.
/// </summary>
internal static class PromotionStage
{
    /// <summary>
    /// Candidates draft and file applications.
    /// </summary>
    public const int Submission = 1;

    /// <summary>
    /// The responsible HR enters the dates and the head settles the raters; the formal check is settled when it ends.
    /// </summary>
    public const int FormalCheck = 2;

    /// <summary>
    /// The raters answer.
    /// </summary>
    public const int Rating = 3;

    /// <summary>
    /// Candidates who fall short of the mandatory requirements give the mandatory artifacts.
    /// </summary>
    public const int Artifacts = 4;

    /// <summary>
    /// The chair records the final scores and the decisions; the results are published when the stage ends.
    /// </summary>
    public const int Review = 5;

    /// <summary>
    /// Candidates see their results and mark the feedback they received.
    /// </summary>
    public const int Feedback = 6;
}
