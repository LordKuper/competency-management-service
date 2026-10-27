using System.Text.Json.Serialization;

namespace Competency.Promotion;

/// <summary>
/// Where an application stands. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationStatus>))]
internal enum ApplicationStatus
{
    /// <summary>
    /// Filed, and waiting for the formal check.
    /// </summary>
    Submitted,

    /// <summary>
    /// Passed the formal check; goes on to the raters and then to the commission.
    /// </summary>
    Admitted,

    /// <summary>
    /// Rejected by the formal check; final.
    /// </summary>
    FormalRejected,

    /// <summary>
    /// Before the commission, from the start of its stage; the chair may record and change the decision until the stage ends.
    /// </summary>
    UnderReview,

    /// <summary>
    /// The chair's decision stands; final.
    /// </summary>
    Decided,

    /// <summary>
    /// The commission stage ended with no decision; final.
    /// </summary>
    NoDecision,

    /// <summary>
    /// Withdrawn by the candidate; final.
    /// </summary>
    Withdrawn,
}
