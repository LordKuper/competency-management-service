using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// What a user may do with a matrix version at the moment. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatrixAction>))]
internal enum MatrixAction
{
    Edit,
    Submit,
    Approve,
    Return,
    AcceptChanges,
    AcceptAndRevise,
    RollbackChanges,
}
