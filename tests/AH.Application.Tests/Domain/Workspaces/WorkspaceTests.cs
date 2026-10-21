using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;

namespace AH.Application.Tests.Domain.Workspaces;

// Rules that depend on persisted user ids (last administrator) are covered by the handler tests.
public sealed class WorkspaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static User NewUser(string email = "owner@example.com") => User.Create(email, "Owner", "", null, Now);

    [Fact]
    public void Create_Owner_AddsOwnerAsOnlyAdministrator()
    {
        var owner = NewUser();

        var workspace = Workspace.Create("  Ops  ", owner, Now);

        Assert.Equal("Ops", workspace.Name);
        Assert.Equal(Now, workspace.CreatedAt);
        var member = Assert.Single(workspace.Members);
        Assert.Same(owner, member.User);
        Assert.Equal(WorkspaceRole.Administrator, member.Role);
        Assert.Equal(Now, member.AddedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_ThrowsArgumentException(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Workspace.Create(name, NewUser(), Now));
    }

    [Fact]
    public void Create_TooLongName_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Workspace.Create(new string('a', Workspace.NameMaxLength + 1), NewUser(), Now));
    }

    [Fact]
    public void AddMember_NewUser_AddsWithRole()
    {
        var workspace = Workspace.Create("Ops", NewUser(), Now);
        var viewer = User.CreatePending("viewer@example.com", Now);

        var member = workspace.AddMember(viewer, WorkspaceRole.Viewer, Now.AddHours(1));

        Assert.Equal(2, workspace.Members.Count);
        Assert.Same(viewer, member.User);
        Assert.Equal(WorkspaceRole.Viewer, member.Role);
        Assert.Equal(Now.AddHours(1), member.AddedAt);
    }

    [Fact]
    public void AddMember_SameUserTwice_ThrowsInvalidOperationException()
    {
        var owner = NewUser();
        var workspace = Workspace.Create("Ops", owner, Now);

        Assert.Throws<InvalidOperationException>(() => workspace.AddMember(owner, WorkspaceRole.Viewer, Now));
    }

    [Theory]
    [InlineData("Sergey", "s@example.com", "Sergey's workspace")]
    [InlineData("  Anna ", "a@example.com", "Anna's workspace")]
    [InlineData("", "John.Doe@Example.com", "john.doe's workspace")]
    public void DefaultName_NameOrEmail_UsesFirstNameOrEmailLocalPart(string firstName, string email, string expected)
    {
        Assert.Equal(expected, Workspace.DefaultName(firstName, email));
    }

    [Fact]
    public void DefaultName_VeryLongFirstName_FitsNameMaxLength()
    {
        var name = Workspace.DefaultName(new string('a', 200), "a@example.com");

        Assert.Equal(Workspace.NameMaxLength, name.Length);
    }

    [Fact]
    public void CreateMain_GlobalAdministrator_CreatesMainWorkspaceWithAdministrator()
    {
        var admin = User.CreateGlobalAdministrator("admin@example.com", "Ada", "", Now);

        var workspace = Workspace.CreateMain(admin, Now);

        Assert.True(workspace.IsMain);
        Assert.False(workspace.CanOwnClusters);
        Assert.Equal(Workspace.MainName, workspace.Name);
        Assert.Equal(WorkspaceRole.Administrator, Assert.Single(workspace.Members).Role);
    }

    [Fact]
    public void CreateMain_RegularUser_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Workspace.CreateMain(NewUser(), Now));
    }

    [Fact]
    public void IsProtectedMember_GlobalAdministratorInRegularWorkspace_ReturnsFalse()
    {
        var admin = User.CreateGlobalAdministrator("admin@example.com", "Ada", "", Now);

        var workspace = Workspace.Create("Ops", admin, Now);

        Assert.False(workspace.IsProtectedMember(admin.Id));
        Assert.True(workspace.CanOwnClusters);
    }

    [Fact]
    public void ChangeRole_GlobalAdministratorToViewer_ThrowsEvenWithAnotherAdministrator()
    {
        var admin = User.CreateGlobalAdministrator("admin@example.com", "Ada", "", Now);
        var workspace = Workspace.CreateMain(admin, Now);
        workspace.AddMember(NewUser("other@example.com"), WorkspaceRole.Administrator, Now);

        Assert.True(workspace.IsProtectedMember(admin.Id));
        Assert.Throws<InvalidOperationException>(() => workspace.ChangeRole(admin.Id, WorkspaceRole.Viewer));
        Assert.Throws<InvalidOperationException>(() => workspace.RemoveMember(admin.Id));
    }
}
