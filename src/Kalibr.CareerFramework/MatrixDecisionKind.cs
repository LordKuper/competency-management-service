using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// What happened to a matrix version at one point of its approval. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixDecisionKind>))]
internal enum MatrixDecisionKind
{
    Submitted,

    /// <summary>
    /// The superior step was passed over because no unit above has a head who can act.
    /// </summary>
    Skipped,

    Approved,

    /// <summary>
    /// The HR step counted as approved because the responsible HR submitted the version themselves.
    /// </summary>
    AutoApproved,

    Returned,

    /// <summary>
    /// The unit head took the approver's changes as they are, which counts as the approval of the returning step.
    /// </summary>
    AcceptedChanges,

    /// <summary>
    /// The unit head took the approver's changes and went on editing the version as a draft.
    /// </summary>
    AcceptedAndRevising,

    /// <summary>
    /// The unit head dropped the approver's changes and went on editing their own variant as a draft.
    /// </summary>
    RolledBack,

    Published,
}
