using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Tests.Workspaces;
using AH.Application.Users.GetOrCreateUser;
using AH.AppDomain.Users;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Tests.Users.GetOrCreateUser;

// The feature flags, the Global Administrator and the certificate registry (see GetOrCreateUserHandler).
public sealed class SignInRulesTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    private async Task<int> RegisterCertificateAsync(int userId, string thumbprint, DateTimeOffset? notAfter = null, bool revoked = false)
    {
        await using var db = CreateDbContext();
        var certificate = UserCertificate.Register(userId, thumbprint, "01", Start.AddDays(-1), notAfter ?? Start.AddYears(1), null, null, Start);
        if (revoked)
        {
            certificate.Revoke(Start);
        }

        db.UserCertificates.Add(certificate);
        await db.SaveChangesAsync(Ct);
        return certificate.Id;
    }

    private static string Thumbprint(char fill) => new(fill, UserCertificate.ThumbprintLength);

    [Fact]
    public async Task HandleAsync_ExternalLoginOff_ReturnsExternalLoginDisabledAndPersistsNothing()
    {
        Flags.Set(FeatureFlags.ExternalLogin, false);

        var result = await SignInExternallyAsync("john@example.com");

        Assert.Equal(SignInErrors.ExternalLoginDisabledCode, result.Error!.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        await using var db = CreateDbContext();
        Assert.False(await db.Users.AnyAsync(Ct));
    }

    [Fact]
    public async Task HandleAsync_RegistrationOffUnknownEmail_ReturnsRegistrationDisabled()
    {
        Flags.Set(FeatureFlags.RegistrationEnabled, false);

        var result = await SignInExternallyAsync("john@example.com");

        Assert.Equal(SignInErrors.RegistrationDisabledCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_RegistrationOffInvitedEmail_SignsInWithoutNewWorkspace()
    {
        var owner = await SignInAsync("owner@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(owner);
        await AddMemberAsync(workspaceId, owner, "jane@example.com", AH.AppDomain.Workspaces.WorkspaceRole.Viewer);
        Flags.Set(FeatureFlags.RegistrationEnabled, false);

        var result = await SignInExternallyAsync("jane@example.com", "Jane");

        Assert.True(result.IsSuccess);
        var mine = await GetMineAsync(result.Value.Id);
        Assert.Equal(workspaceId, Assert.Single(mine.Value).Id);
    }

    [Fact]
    public async Task HandleAsync_RegistrationOffUserWithoutWorkspace_SignsInWithNoWorkspace()
    {
        // For example removed from every workspace it belonged to.
        await using (var db = CreateDbContext())
        {
            db.Users.Add(User.Create("john@example.com", "John", "", null, Start));
            await db.SaveChangesAsync(Ct);
        }

        Flags.Set(FeatureFlags.RegistrationEnabled, false);

        var result = await SignInExternallyAsync("john@example.com");

        Assert.True(result.IsSuccess);
        Assert.Empty((await GetMineAsync(result.Value.Id)).Value);
    }

    [Fact]
    public async Task HandleAsync_GlobalAdministratorExternally_ReturnsCertificateRequired()
    {
        await EnsureGlobalAdministratorAsync();

        var result = await SignInExternallyAsync(AdminEmail);

        Assert.Equal(SignInErrors.CertificateRequiredCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_GlobalAdministratorWithBootstrapCertificate_SignsInWithoutPersonalWorkspace()
    {
        var bootstrap = BootstrapCertificate();
        var admin = await EnsureGlobalAdministratorAsync(certificate: bootstrap);

        var result = await SignInWithCertificateAsync(AdminEmail, bootstrap.Thumbprint.ToLowerInvariant());

        Assert.True(result.IsSuccess);
        Assert.Equal(admin.Value.UserId, result.Value.Id);
        var mine = await GetMineAsync(result.Value.Id);
        Assert.True(Assert.Single(mine.Value).IsMain);
    }

    [Fact]
    public async Task HandleAsync_CertificateForUnknownEmail_ReturnsCertificateUnknown()
    {
        var result = await SignInWithCertificateAsync("stranger@example.com", Thumbprint('C'));

        Assert.Equal(SignInErrors.CertificateUnknownCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_CertificateOfAnotherUser_ReturnsCertificateUnknown()
    {
        var john = await SignInAsync("john@example.com");
        await SignInAsync("jane@example.com");
        await RegisterCertificateAsync(john, Thumbprint('C'));

        var result = await SignInWithCertificateAsync("jane@example.com", Thumbprint('C'));

        Assert.Equal(SignInErrors.CertificateUnknownCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_RevokedCertificate_ReturnsCertificateUnknown()
    {
        var john = await SignInAsync("john@example.com");
        await RegisterCertificateAsync(john, Thumbprint('C'), revoked: true);

        var result = await SignInWithCertificateAsync("john@example.com", Thumbprint('C'));

        Assert.Equal(SignInErrors.CertificateUnknownCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCertificate_ReturnsCertificateUnknown()
    {
        var john = await SignInAsync("john@example.com");
        await RegisterCertificateAsync(john, Thumbprint('C'), notAfter: Start.AddDays(1));
        Time.Advance(TimeSpan.FromDays(2));

        var result = await SignInWithCertificateAsync("john@example.com", Thumbprint('C'));

        Assert.Equal(SignInErrors.CertificateUnknownCode, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_RegisteredCertificateWhileExternalLoginOff_SignsIn()
    {
        var john = await SignInAsync("john@example.com");
        await RegisterCertificateAsync(john, Thumbprint('C'));
        Flags.Set(FeatureFlags.ExternalLogin, false).Set(FeatureFlags.RegistrationEnabled, false);

        var result = await SignInWithCertificateAsync("john@example.com", Thumbprint('C'));

        Assert.True(result.IsSuccess);
        Assert.Equal(john, result.Value.Id);
    }

    [Fact]
    public async Task HandleAsync_CertificateWithoutThumbprint_ReturnsValidationError()
    {
        var result = await SignInWithCertificateAsync("john@example.com", "");

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}
