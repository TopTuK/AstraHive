using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Tests.Workspaces.RemoveWorkspaceMember;

public sealed class RemoveWorkspaceMemberHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_Viewer_RemovesMembershipButKeepsUser()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        var added = await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = MemberId(added, "viewer@example.com");

        var result = await RemoveMemberAsync(workspaceId, adminId, viewerId);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(result.Value.Members, m => m.UserId == viewerId);
        await using var db = CreateDbContext();
        Assert.True(await db.Users.AnyAsync(u => u.Id == viewerId, Ct));
    }

    [Fact]
    public async Task HandleAsync_LastAdministrator_ReturnsConflict()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);

        var result = await RemoveMemberAsync(workspaceId, adminId, adminId);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_AdministratorLeavesWithAnotherAdministrator_ReportsNoRole()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "second@example.com", WorkspaceRole.Administrator);

        var result = await RemoveMemberAsync(workspaceId, adminId, adminId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.YourRole);
        Assert.Empty((await GetMineAsync(adminId)).Value);
    }

    [Fact]
    public async Task HandleAsync_RequesterIsViewer_ReturnsForbidden()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = await SignInAsync("viewer@example.com");

        var result = await RemoveMemberAsync(workspaceId, viewerId, adminId);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }
}
