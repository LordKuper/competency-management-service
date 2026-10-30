using Microsoft.AspNetCore.Identity;

namespace Competency.UserManagement;

/// <summary>
/// Password validator that counts letters of any alphabet, Cyrillic included, as upper-case, lower-case and alphanumeric characters.
/// </summary>
/// <param name="errors">The describer that supplies the error texts.</param>
internal sealed class UnicodePasswordValidator(IdentityErrorDescriber? errors = null) : PasswordValidator<AppUser>(errors)
{
    /// <inheritdoc/>
    public override bool IsUpper(char c) => char.IsUpper(c);

    /// <inheritdoc/>
    public override bool IsLower(char c) => char.IsLower(c);

    /// <inheritdoc/>
    public override bool IsLetterOrDigit(char c) => char.IsLetterOrDigit(c);
}
