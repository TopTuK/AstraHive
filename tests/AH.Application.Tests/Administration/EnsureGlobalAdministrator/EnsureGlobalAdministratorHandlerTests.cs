using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Tests.Workspaces;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Tests.Administration.EnsureGlobalAdministrator;

public sealed class EnsureGlobalAdministratorHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_EmptyDatabase_CreatesPendingAdministratorAndMainWorkspace()
    {
        var result = await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate());

        Assert.True(result.IsSuccess);
        await using var db = CreateDbContext();
        var admin = await db.Users.SingleAsync(Ct);
        Assert.True(admin.IsGlobalAdministrator);
        Assert.True(admin.IsPending);
        var main = await db.Workspaces.Include(w => w.Members).SingleAsync(Ct);
        Assert.True(main.IsMain);
        Assert.Equal(result.Value.MainWorkspaceId, main.Id);
        var member = Assert.Single(main.Members);
        Assert.Equal(admin.Id, member.UserId);
        Assert.Equal(WorkspaceRole.Administrator, member.Role);
        var certificate = await db.UserCertificates.SingleAsync(Ct);
        Assert.Equal(admin.Id, certificate.UserId);
        Assert.Null(certificate.ObjectKey);
    }

    [Fact]
    public async Task HandleAsync_SecondRun_ChangesNothing()
    {
        var first = await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate());

        var second = await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate());

        Assert.Equal(first.Value, second.Value);
        await using var db = CreateDbContext();
        Assert.Equal(1, await db.Users.CountAsync(Ct));
        Assert.Equal(1, await db.Workspaces.CountAsync(Ct));
        Assert.Equal(1, await db.UserCertificates.CountAsync(Ct));
    }

    [Fact]
    public async Task HandleAsync_NewCertificate_RegistersItNextToTheOldOne()
    {
        await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate('B'));

        await EnsureGlobalAdministratorAsync(certificate: BootstrapCertificate('C'));

        await using var db = CreateDbContext();
        Assert.Equal(2, await db.UserCertificates.CountAsync(Ct));
    }

    [Fact]
    public async Task HandleAsync_ExistingUser_PromotesAndKeepsPersonalWorkspace()
    {
        var userId = await SignInAsync(AdminEmail, "Ada");

        var result = await EnsureGlobalAdministratorAsync();

        Assert.Equal(userId, result.Value.UserId);
        var mine = await GetMineAsync(userId);
        Assert.Equal(2, mine.Value.Count);
        Assert.True(mine.Value[0].IsMain);
    }

    [Fact]
    public async Task HandleAsync_DifferentEmailThanStoredAdministrator_ReturnsConflict()
    {
        await EnsureGlobalAdministratorAsync();

        var result = await EnsureGlobalAdministratorAsync("someone@example.com");

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        await using var db = CreateDbContext();
        Assert.Equal(1, await db.Users.CountAsync(Ct));
    }
}
