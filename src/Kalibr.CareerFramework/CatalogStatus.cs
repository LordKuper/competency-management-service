using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// Where a career catalogue entry stands. Member names are stored and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogStatus>))]
internal enum CatalogStatus
{
    /// <summary>
    /// Proposed by a unit head and not yet approved by the unit's responsible HR.
    /// </summary>
    Draft,

    /// <summary>
    /// In use and readable by everyone.
    /// </summary>
    Active,

    /// <summary>
    /// Out of use; kept for the matrices that already refer to it.
    /// </summary>
    Inactive,
}
