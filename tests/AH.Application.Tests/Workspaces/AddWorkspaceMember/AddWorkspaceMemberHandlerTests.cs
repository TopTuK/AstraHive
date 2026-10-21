using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Tests.Workspaces.AddWorkspaceMember;

public sealed class AddWorkspaceMemberHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_UnknownEmail_AddsPendingUser()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await AddMemberAsync(workspaceId, adminId, " New@Example.com ", WorkspaceRole.Viewer);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(result.Value.Members, m => m.Email == "new@example.com");
        Assert.True(added.IsPending);
        Assert.Equal(WorkspaceRole.Viewer, added.Role);
        Assert.Equal(WorkspaceRole.Administrator, result.Value.YourRole);
    }

    [Fact]
    public async Task HandleAsync_ExistingUser_AddsWithoutCreatingAnotherUser()
    {
        var adminId = await SignInAsync("admin@example.com");
        var otherId = await SignInAsync("other@example.com", "Other");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await AddMemberAsync(workspaceId, adminId, "OTHER@example.com", WorkspaceRole.Administrator);

        var added = Assert.Single(result.Value.Members, m => m.UserId == otherId);
        Assert.False(added.IsPending);
        Assert.Equal("Other", added.FirstName);
        await using var db = CreateDbContext();
        Assert.Equal(2, await db.Users.CountAsync(Ct));
    }

    [Fact]
    public async Task HandleAsync_AlreadyMember_ReturnsConflict()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);

        var result = await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Administrator);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_RequesterIsViewer_ReturnsForbidden()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);
        await AddMemberAsync(workspaceId, adminId, "viewer@example.com", WorkspaceRole.Viewer);
        var viewerId = await SignInAsync("viewer@example.com");

        var result = await AddMemberAsync(workspaceId, viewerId, "third@example.com", WorkspaceRole.Viewer);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_RequesterNotMember_ReturnsNotFound()
    {
        var adminId = await SignInAsync("admin@example.com");
        var strangerId = await SignInAsync("stranger@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await AddMemberAsync(workspaceId, strangerId, "third@example.com", WorkspaceRole.Viewer);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ReturnsValidationError()
    {
        var adminId = await SignInAsync("admin@example.com");
        var workspaceId = await OwnWorkspaceIdAsync(adminId);

        var result = await AddMemberAsync(workspaceId, adminId, "not-an-email", WorkspaceRole.Viewer);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}
