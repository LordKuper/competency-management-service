namespace Competency.Platform;

/// <summary>
/// The collation of every text column the server orders by: the Russian ICU collation the PostgreSQL image ships,
/// so "ё" stands next to "е" and letter case does not decide the order.
/// </summary>
public static class TextCollation
{
    /// <summary>
    /// The built-in Russian ICU collation, present in every database of the image, so no migration creates it.
    /// </summary>
    public const string Russian = "ru-x-icu";
}
