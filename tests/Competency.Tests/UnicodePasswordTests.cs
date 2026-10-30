using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Competency.UserManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A password may hold Cyrillic: a Russian capital and a Russian small letter count as the upper and lower case the policy asks for, and a
/// Russian letter is no special character.
/// </summary>
public sealed class UnicodePasswordTests(TestEnvironment environment)
{
    private static readonly UserManager<AppUser> Manager = new(
        new NoStore(),
        Options.Create(new IdentityOptions()),
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!);

    private sealed class NoStore : IUserStore<AppUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static async Task<string[]> ErrorsAsync(string password) =>
        [.. (await new UnicodePasswordValidator().ValidateAsync(Manager, new AppUser(), password)).Errors.Select(error => error.Code)];

    [Fact]
    public async Task Ac88_RussianCapitalAndSmallLettersAreAccepted_WhenADigitAndASpecialCharacterAreThere()
    {
        (await ErrorsAsync("Пароль-12345")).Should().BeEmpty();
    }

    [Fact]
    public async Task Ac88_ARussianPasswordInOneCaseIsRefusedForTheCaseThatIsMissing()
    {
        (await ErrorsAsync("пароль-12345")).Should().Equal("PasswordRequiresUpper");
        (await ErrorsAsync("ПАРОЛЬ-12345")).Should().Equal("PasswordRequiresLower");
    }

    [Fact]
    public async Task Ac88_ARussianLetterIsNoSpecialCharacter_AndDigitAndLengthAreStillRequired()
    {
        (await ErrorsAsync("Парольпароль1")).Should().Equal("PasswordRequiresNonAlphanumeric");
        (await ErrorsAsync("Пароль-пароль")).Should().Equal("PasswordRequiresDigit");
        (await ErrorsAsync("Пар-1")).Should().Equal("PasswordTooShort");
    }

    [Fact]
    public async Task Ac88_TheHostAcceptsARussianPasswordOnTheInvitationLink_AndTheUserSignsInWithIt()
    {
        var host = await environment.SharedHostAsync();
        var invited = await (await host.AdminAsync()).InviteUserAsync();
        var token = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        const string RussianPassword = "Пароль-Русский-12345!";

        (await host.Anonymous().PostAsync("/api/v1/auth/accept-invitation", new { token, password = RussianPassword })).Expect(HttpStatusCode.NoContent);

        (await host.LoginAsync(invited.Email, RussianPassword)).Should().NotBeNull();
    }
}
