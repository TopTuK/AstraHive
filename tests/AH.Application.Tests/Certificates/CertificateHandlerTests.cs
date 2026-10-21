using System.Security.Cryptography.X509Certificates;
using AH.Application.Certificates;
using AH.Application.Certificates.DownloadCertificate;
using AH.Application.Certificates.GetUserCertificates;
using AH.Application.Certificates.IssueCertificate;
using AH.Application.Certificates.RevokeCertificate;
using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Tests.Workspaces;
using AH.Application.Users.GetOrCreateUser;
using AH.AppDomain.Workspaces;
using AH.Infrastructure.Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AH.Application.Tests.Certificates;

// Issues real certificates from a throwaway root, so the tests also cover CertificateAuthority.
public sealed class CertificateHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    private const string MemberEmail = "operator@example.com";

    // Valid before Start: the issuer must already be valid when a client certificate starts.
    private static readonly Lazy<CertificateAuthorityOptions> RootOptions = new(() =>
    {
        using var root = ClientCertificateFactory.CreateRoot("CN=Test Root CA", Start.AddYears(-1), 10);
        return new CertificateAuthorityOptions
        {
            Certificate = root.ExportCertificatePem(),
            PrivateKey = root.GetECDsaPrivateKey()!.ExportPkcs8PrivateKeyPem(),
        };
    });

    private readonly FakeObjectStorage _storage = new();

    // The Global Administrator, the main workspace and a pending member in it.
    private async Task<(int AdminId, int WorkspaceId, int MemberId)> ArrangeMainWorkspaceAsync()
    {
        var admin = await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate());
        var members = await AddMemberAsync(admin.Value.MainWorkspaceId, admin.Value.UserId, MemberEmail, WorkspaceRole.Viewer);
        return (admin.Value.UserId, admin.Value.MainWorkspaceId, MemberId(members, MemberEmail));
    }

    private async Task<Result<IssuedCertificateResponse>> IssueAsync(int workspaceId, int requesterId, int userId, CertificateAuthorityOptions? root = null)
    {
        await using var db = CreateDbContext();
        using var authority = new CertificateAuthority(Options.Create(root ?? RootOptions.Value), Time);
        var handler = new IssueCertificateHandler(db, authority, _storage, Time, NullLogger<IssueCertificateHandler>.Instance);
        return await handler.HandleAsync(new IssueCertificateCommand(workspaceId, requesterId, userId), Ct);
    }

    private async Task<Result<CertificateFileResponse>> DownloadAsync(int workspaceId, int requesterId, int userId, int certificateId)
    {
        await using var db = CreateDbContext();
        var handler = new DownloadCertificateHandler(db, _storage, NullLogger<DownloadCertificateHandler>.Instance);
        return await handler.HandleAsync(new DownloadCertificateQuery(workspaceId, requesterId, userId, certificateId), Ct);
    }

    private async Task<Result<IReadOnlyList<CertificateResponse>>> RevokeAsync(int workspaceId, int requesterId, int userId, int certificateId)
    {
        await using var db = CreateDbContext();
        var handler = new RevokeCertificateHandler(db, _storage, Time, NullLogger<RevokeCertificateHandler>.Instance);
        return await handler.HandleAsync(new RevokeCertificateCommand(workspaceId, requesterId, userId, certificateId), Ct);
    }

    private async Task<Result<IReadOnlyList<CertificateResponse>>> ListAsync(int workspaceId, int requesterId, int userId)
    {
        await using var db = CreateDbContext();
        return await new GetUserCertificatesHandler(db).HandleAsync(new GetUserCertificatesQuery(workspaceId, requesterId, userId), Ct);
    }

    [Fact]
    public async Task Issue_GlobalAdministrator_StoresPfxThatOpensWithReturnedPassword()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();

        var result = await IssueAsync(workspaceId, adminId, memberId);

        Assert.True(result.IsSuccess);
        var (content, contentType) = Assert.Single(_storage.Objects).Value;
        Assert.Equal(IssueCertificateHandler.PfxContentType, contentType);
        using var pfx = X509CertificateLoader.LoadPkcs12(content, result.Value.Password);
        Assert.True(pfx.HasPrivateKey);
        Assert.Equal(MemberEmail, pfx.GetNameInfo(X509NameType.EmailName, forIssuer: false));
        Assert.Equal(ClientCertificateFactory.Thumbprint(pfx), result.Value.Certificate.Thumbprint);
        Assert.True(result.Value.Certificate.CanDownload);
    }

    [Fact]
    public async Task Issue_ThenMemberSignsInWithIt_Succeeds()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();
        var issued = await IssueAsync(workspaceId, adminId, memberId);

        var signIn = await SignInWithCertificateAsync(MemberEmail, issued.Value.Certificate.Thumbprint);

        Assert.True(signIn.IsSuccess);
        Assert.Equal(memberId, signIn.Value.Id);
    }

    [Fact]
    public async Task Issue_RequesterIsAdministratorButNotGlobalAdministrator_ReturnsForbidden()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();
        await ChangeRoleAsync(workspaceId, adminId, memberId, WorkspaceRole.Administrator);

        var result = await IssueAsync(workspaceId, memberId, adminId);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        Assert.Empty(_storage.Objects);
    }

    [Fact]
    public async Task Issue_RequesterNotMember_ReturnsNotFound()
    {
        var (_, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();
        var stranger = await SignInAsync("stranger@example.com");

        var result = await IssueAsync(workspaceId, stranger, memberId);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Issue_RegularWorkspace_ReturnsForbidden()
    {
        var owner = await SignInAsync("owner@example.com");

        var result = await IssueAsync(await OwnWorkspaceIdAsync(owner), owner, owner);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task Issue_NoRootConfigured_ReturnsConflict()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();

        var result = await IssueAsync(workspaceId, adminId, memberId, new CertificateAuthorityOptions());

        Assert.Equal("Certificates.AuthorityUnavailable", result.Error!.Code);
    }

    [Fact]
    public async Task Download_IssuedCertificate_ReturnsStoredPfx()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();
        var issued = await IssueAsync(workspaceId, adminId, memberId);

        var file = await DownloadAsync(workspaceId, adminId, memberId, issued.Value.Certificate.Id);

        Assert.Equal($"{MemberEmail}.pfx", file.Value.FileName);
        Assert.Equal(Assert.Single(_storage.Objects).Value.Content, file.Value.Content);
    }

    [Fact]
    public async Task Download_BootstrapCertificate_ReturnsNotFound()
    {
        var (adminId, workspaceId, _) = await ArrangeMainWorkspaceAsync();
        var bootstrap = Assert.Single((await ListAsync(workspaceId, adminId, adminId)).Value);

        var file = await DownloadAsync(workspaceId, adminId, adminId, bootstrap.Id);

        Assert.False(bootstrap.CanDownload);
        Assert.Equal(ErrorType.NotFound, file.Error!.Type);
    }

    [Fact]
    public async Task Revoke_IssuedCertificate_DeletesPfxAndBlocksSignIn()
    {
        var (adminId, workspaceId, memberId) = await ArrangeMainWorkspaceAsync();
        var issued = await IssueAsync(workspaceId, adminId, memberId);

        var result = await RevokeAsync(workspaceId, adminId, memberId, issued.Value.Certificate.Id);

        var revoked = Assert.Single(result.Value);
        Assert.NotNull(revoked.RevokedAt);
        Assert.False(revoked.CanDownload);
        Assert.Empty(_storage.Objects);
        var signIn = await SignInWithCertificateAsync(MemberEmail, issued.Value.Certificate.Thumbprint);
        Assert.Equal(SignInErrors.CertificateUnknownCode, signIn.Error!.Code);
        var download = await DownloadAsync(workspaceId, adminId, memberId, issued.Value.Certificate.Id);
        Assert.Equal(ErrorType.NotFound, download.Error!.Type);
    }

    [Fact]
    public async Task Revoke_LastGlobalAdministratorCertificate_ReturnsConflict()
    {
        var (adminId, workspaceId, _) = await ArrangeMainWorkspaceAsync();
        var bootstrap = Assert.Single((await ListAsync(workspaceId, adminId, adminId)).Value);

        var result = await RevokeAsync(workspaceId, adminId, adminId, bootstrap.Id);

        Assert.Equal("Certificates.LastGlobalAdministratorCertificate", result.Error!.Code);
    }

    [Fact]
    public async Task Revoke_GlobalAdministratorCertificateWithAnotherActive_Succeeds()
    {
        var (adminId, workspaceId, _) = await ArrangeMainWorkspaceAsync();
        var bootstrap = Assert.Single((await ListAsync(workspaceId, adminId, adminId)).Value);
        await IssueAsync(workspaceId, adminId, adminId);

        var result = await RevokeAsync(workspaceId, adminId, adminId, bootstrap.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }
}
