namespace Kalibr.Platform;

/// <summary>
/// A record a desk finding is about or names, with what the desk shows of it.
/// </summary>
/// <param name="Type">What kind of record it is.</param>
/// <param name="Id">The record identifier.</param>
/// <param name="Name">The record's name as shown: a title, a full name or an e-mail address.</param>
/// <param name="Context">What places the record, such as the units above a unit from the root down; null when the name is enough.</param>
public sealed record DeskObject(DeskObjectType Type, Guid Id, string Name, IReadOnlyList<string>? Context = null);
