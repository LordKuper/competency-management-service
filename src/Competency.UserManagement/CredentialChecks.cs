using System.Net.Mail;

namespace Competency.UserManagement;

/// <summary>
/// The shape checks of e-mail addresses and passwords that every request carrying them shares; the password policy itself is Identity's.
/// </summary>
internal static class CredentialChecks
{
    public static void CheckEmail(Dictionary<string, string[]> errors, string field, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors[field] = ["E-mail: укажите значение."];
        }
        else if (email.Trim().Length > AppUser.EmailMaxLength)
        {
            errors[field] = [$"E-mail: не длиннее {AppUser.EmailMaxLength} символов."];
        }
        else if (!IsEmailAddress(email.Trim()))
        {
            errors[field] = ["E-mail: укажите корректный адрес."];
        }
    }

    /// <summary>
    /// Whether the text is exactly one plain address, without a display name or comments.
    /// </summary>
    /// <param name="value">The trimmed text.</param>
    /// <returns><see langword="true"/> when the text parses as an address and equals it.</returns>
    public static bool IsEmailAddress(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;

    public static void CheckPassword(Dictionary<string, string[]> errors, string field, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors[field] = ["Пароль: укажите значение."];
        }
        else if (password.Length > AppUser.PasswordMaxLength)
        {
            errors[field] = [$"Пароль: не длиннее {AppUser.PasswordMaxLength} символов."];
        }
    }
}
