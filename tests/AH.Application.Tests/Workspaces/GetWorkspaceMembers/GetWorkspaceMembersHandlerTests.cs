using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.AppDomain.Workspaces;

namespace AH.Application.Tests.Workspaces.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_Viewer_SeesMembersAndOwnRole()
    {
        var adminId = await SignInAsync("admin@example.com", "Anna");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = await SignInAsync("viewer@example.com");

        var result = await GetMembersAsync(workspaceId, viewerId);

        Assert.Equal("Anna's workspace", result.Value.Name);
        Assert.Equal(WorkspaceRole.Viewer, result.Value.YourRole);
        Assert.Equal(["admin@example.com", "viewer@example.com"], result.Value.Members.Select(m => m.Email));
        Assert.All(result.Value.Members, m => Assert.False(m.IsPending));
    }

    [Fact]
    public async Task HandleAsync_NotMember_ReturnsNotFound()
    {
        var adminId = await SignInAsync("admin@example.com");
        var strangerId = await SignInAsync("stranger@example.com");

        var result = await GetMembersAsync(await OwnWorkspaceIdAsync(adminId), strangerId);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_UnknownWorkspace_ReturnsNotFound()
    {
        var adminId = await SignInAsync("admin@example.com");

        var result = await GetMembersAsync(999, adminId);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
