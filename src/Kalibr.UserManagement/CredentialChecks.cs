using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// The shape checks of e-mail addresses and passwords that every request carrying them shares; the password policy itself is Identity's, applied through its validators.
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

    /// <summary>
    /// Applies every password rule to a password that is set without going through the password-changing operations of the user manager,
    /// which would save the account twice and leave it without a password in between, and could not save an audit event with it.
    /// </summary>
    /// <param name="users">The user manager that holds the password rules.</param>
    /// <param name="user">The account the password is for.</param>
    /// <param name="password">The new password.</param>
    /// <returns>Success, or every rule the password breaks.</returns>
    public static async Task<IdentityResult> ValidatePasswordAsync(this UserManager<AppUser> users, AppUser user, string password)
    {
        var problems = new List<IdentityError>();
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            problems.AddRange(result.Errors);
        }

        return problems.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. problems]);
    }
}
