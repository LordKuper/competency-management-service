using System.Text.Json.Serialization;

namespace Kalibr.Promotion;

/// <summary>
/// What going back to an earlier stage resets or computes again. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RollbackEffectKind>))]
internal enum RollbackEffectKind
{
    /// <summary>
    /// The formal check of an application that passed it is cancelled; the application is filed again.
    /// </summary>
    FormalAdmittedReset,

    /// <summary>
    /// The formal check of an application that failed it is cancelled; the application is filed again.
    /// </summary>
    FormalRejectedReset,

    /// <summary>
    /// The mark that the commission recorded no decision is removed.
    /// </summary>
    NoDecisionCleared,

    /// <summary>
    /// The result of the circular rating is computed again when its stage ends.
    /// </summary>
    RatingRecalculated,

    /// <summary>
    /// The mark that mandatory artifacts are missing is computed again when their stage ends.
    /// </summary>
    ArtifactsRecalculated,

    /// <summary>
    /// The candidates stop seeing the published result until it is published again.
    /// </summary>
    ResultsHidden,
}
