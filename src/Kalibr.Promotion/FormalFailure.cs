using System.Text.Json.Serialization;

namespace Competency.Promotion;

/// <summary>
/// A condition of the formal check an application does not meet. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FormalFailure>))]
internal enum FormalFailure
{
    /// <summary>
    /// No date of hiring or of the last promotion has been entered.
    /// </summary>
    MissingDate,

    /// <summary>
    /// Less than the minimum interval has passed since that date.
    /// </summary>
    IntervalNotPassed,

    /// <summary>
    /// The candidate's education is below what the target level requires.
    /// </summary>
    EducationBelowRequired,
}
