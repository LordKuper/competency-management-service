namespace Competency.UserManagement;

/// <summary>
/// The shape checks of user names and passwords that every request carrying them shares; the password policy itself is Identity's.
/// </summary>
internal static class CredentialChecks
{
    public static void CheckUserName(Dictionary<string, string[]> errors, string field, string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            errors[field] = ["Имя пользователя: укажите значение."];
        }
        else if (userName.Trim().Length > AppUser.UserNameMaxLength)
        {
            errors[field] = [$"Имя пользователя: не длиннее {AppUser.UserNameMaxLength} символов."];
        }
    }

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
