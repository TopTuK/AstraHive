using AH.Application.Tests.Fixtures;
using AH.AppDomain.Workspaces;

namespace AH.Application.Tests.Workspaces.GetMyWorkspaces;

public sealed class GetMyWorkspacesHandlerTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    [Fact]
    public async Task HandleAsync_UserInTwoWorkspaces_ReturnsBothWithRoles()
    {
        var annaId = await SignInAsync("anna@example.com", "Anna");
        var borisId = await SignInAsync("boris@example.com", "Boris");
        await AddMemberAsync(await OwnWorkspaceIdAsync(borisId), borisId, "anna@example.com", WorkspaceRole.Viewer);

        var result = await GetMineAsync(annaId);

        Assert.Collection(
            result.Value,
            own =>
            {
                Assert.Equal("Anna's workspace", own.Name);
                Assert.Equal(WorkspaceRole.Administrator, own.Role);
                Assert.Equal(1, own.MemberCount);
            },
            shared =>
            {
                Assert.Equal("Boris's workspace", shared.Name);
                Assert.Equal(WorkspaceRole.Viewer, shared.Role);
                Assert.Equal(2, shared.MemberCount);
            });
    }
}
