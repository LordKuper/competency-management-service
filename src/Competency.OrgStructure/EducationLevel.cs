using System.Text.Json.Serialization;

namespace Competency.OrgStructure;

/// <summary>
/// A level of education, in the order of the federal law on education: a higher member is a higher level. Member names are stored
/// and sent in the API, so renaming one changes both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EducationLevel>))]
public enum EducationLevel
{
    /// <summary>
    /// General secondary education.
    /// </summary>
    SecondaryGeneral,

    /// <summary>
    /// Vocational secondary education.
    /// </summary>
    SecondaryVocational,

    /// <summary>
    /// Higher education, bachelor's degree.
    /// </summary>
    Bachelor,

    /// <summary>
    /// Higher education, specialist's or master's degree.
    /// </summary>
    SpecialistOrMaster,

    /// <summary>
    /// Higher education, training of highly qualified personnel.
    /// </summary>
    HigherQualification,
}
