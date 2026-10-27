using System.Text.Json.Serialization;

namespace Competency.Promotion;

/// <summary>
/// What the signed-in user is to an application; one user can be several, except that its candidate is only ever its candidate.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationRole>))]
internal enum ApplicationRole
{
    /// <summary>
    /// Nothing: the application is not theirs to see.
    /// </summary>
    None = 0,

    /// <summary>
    /// The employee who filed it.
    /// </summary>
    Candidate = 1,

    /// <summary>
    /// A global administrator.
    /// </summary>
    Administrator = 2,

    /// <summary>
    /// The account that announced the wave.
    /// </summary>
    Author = 4,

    /// <summary>
    /// The responsible HR of the unit of the target.
    /// </summary>
    ResponsibleHr = 8,

    /// <summary>
    /// The head of the unit of the target.
    /// </summary>
    Head = 16,

    /// <summary>
    /// A member of the commission of the unit of the target, who acts; the responsible HR and the head are members.
    /// </summary>
    Member = 32,
}
