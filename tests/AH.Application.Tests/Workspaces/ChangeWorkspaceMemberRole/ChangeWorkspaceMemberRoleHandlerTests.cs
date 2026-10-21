using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.AppDomain.Workspaces;

namespace AH.Application.Tests.Workspaces.ChangeWorkspaceMemberRole;

public sealed class ChangeWorkspaceMemberRoleHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_PromoteViewer_ChangesRole()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        var added = await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = MemberId(added, "viewer@example.com");

        var result = await ChangeRoleAsync(workspaceId, adminId, viewerId, WorkspaceRole.Administrator);

        Assert.Equal(WorkspaceRole.Administrator, Assert.Single(result.Value.Members, m => m.UserId == viewerId).Role);
        var reloaded = await GetMembersAsync(workspaceId, adminId);
        Assert.All(reloaded.Value.Members, m => Assert.Equal(WorkspaceRole.Administrator, m.Role));
    }

    [Fact]
    public async Task HandleAsync_DemoteLastAdministrator_ReturnsConflict()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await ChangeRoleAsync(workspaceId, adminId, adminId, WorkspaceRole.Viewer);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        var reloaded = await GetMembersAsync(workspaceId, adminId);
        Assert.Equal(WorkspaceRole.Administrator, reloaded.Value.YourRole);
    }

    [Fact]
    public async Task HandleAsync_DemoteSelfWithAnotherAdministrator_Succeeds()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "second@example.com", WorkspaceRole.Administrator);

        var result = await ChangeRoleAsync(workspaceId, adminId, adminId, WorkspaceRole.Viewer);

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkspaceRole.Viewer, result.Value.YourRole);
    }

    [Fact]
    public async Task HandleAsync_RequesterIsViewer_ReturnsForbidden()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = await SignInAsync("viewer@example.com");

        var result = await ChangeRoleAsync(workspaceId, viewerId, viewerId, WorkspaceRole.Administrator);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_UserNotMember_ReturnsNotFound()
    {
        var adminId = await SignInAsync("admin@example.com");
        var strangerId = await SignInAsync("stranger@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await ChangeRoleAsync(workspaceId, adminId, strangerId, WorkspaceRole.Viewer);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_UndefinedRole_ReturnsValidationError()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await ChangeRoleAsync(workspaceId, adminId, adminId, (WorkspaceRole)42);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}
