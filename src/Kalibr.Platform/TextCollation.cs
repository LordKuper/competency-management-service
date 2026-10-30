using System.Globalization;

namespace Kalibr.Platform;

/// <summary>
/// The Russian ordering of names and full names, in the database and in memory, so a list reads the same wherever the server sorts it:
/// "ё" stands next to "е" and letter case does not decide the order.
/// </summary>
public static class TextCollation
{
    /// <summary>
    /// The built-in Russian ICU collation of the database, present in every database of the image, so no migration creates it.
    /// </summary>
    public const string Russian = "ru-x-icu";

    /// <summary>
    /// Orders text in memory by the same Russian rules as <see cref="Russian"/>.
    /// </summary>
    public static readonly StringComparer Comparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: false);
}
