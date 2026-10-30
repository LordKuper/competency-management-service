using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// Where a matrix version stands. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixVersionStatus>))]
internal enum MatrixVersionStatus
{
    /// <summary>
    /// The unit head is working on it.
    /// </summary>
    Draft,

    /// <summary>
    /// Waiting for the approver of its current step.
    /// </summary>
    OnReview,

    /// <summary>
    /// Sent back to the unit head with changes or a comment, waiting for the head's decision.
    /// </summary>
    Returned,

    /// <summary>
    /// Approved by the responsible HR; recorded in the history only, because approval publishes the version at once.
    /// </summary>
    Approved,

    /// <summary>
    /// In force; never changes again.
    /// </summary>
    Published,

    /// <summary>
    /// Replaced by a later published version.
    /// </summary>
    Archived,
}
