using System.Text.Json.Serialization;

namespace Kalibr.Promotion;

/// <summary>
/// The decision the chair of a commission records on an application. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PromotionDecision>))]
internal enum PromotionDecision
{
    /// <summary>
    /// The candidate meets the criteria for promotion.
    /// </summary>
    Meets,

    /// <summary>
    /// The candidate does not meet the criteria for promotion.
    /// </summary>
    DoesNotMeet,
}
