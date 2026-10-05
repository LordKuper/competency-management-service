using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// Describes in Russian the Identity failures an account or password change can produce, because administrators and users read them in the interface.
/// Codes stay those of the base describer; only the codes this application can raise are translated.
/// </summary>
internal sealed class RussianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Describe(nameof(DefaultError), "Не удалось выполнить операцию.");

    public override IdentityError ConcurrencyFailure() =>
        Describe(nameof(ConcurrencyFailure), "Учётная запись изменена другим пользователем. Обновите данные и повторите.");

    public override IdentityError PasswordMismatch() =>
        Describe(nameof(PasswordMismatch), "Текущий пароль указан неверно.");

    public override IdentityError InvalidUserName(string? userName) =>
        Describe(nameof(InvalidUserName), "Имя пользователя может содержать только латинские буквы, цифры и символы - . _ @ +");

    public override IdentityError DuplicateUserName(string userName) =>
        Describe(nameof(DuplicateUserName), $"Имя пользователя «{userName}» уже занято.");

    public override IdentityError PasswordTooShort(int length) =>
        Describe(nameof(PasswordTooShort), $"Пароль должен содержать не менее {length} символов.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Describe(nameof(PasswordRequiresUniqueChars), $"Пароль должен содержать не менее {uniqueChars} разных символов.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Describe(nameof(PasswordRequiresNonAlphanumeric), "Пароль должен содержать хотя бы один специальный символ.");

    public override IdentityError PasswordRequiresDigit() =>
        Describe(nameof(PasswordRequiresDigit), "Пароль должен содержать хотя бы одну цифру.");

    public override IdentityError PasswordRequiresLower() =>
        Describe(nameof(PasswordRequiresLower), "Пароль должен содержать хотя бы одну строчную букву.");

    public override IdentityError PasswordRequiresUpper() =>
        Describe(nameof(PasswordRequiresUpper), "Пароль должен содержать хотя бы одну заглавную букву.");

    private static IdentityError Describe(string code, string description) => new() { Code = code, Description = description };
}
