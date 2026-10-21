using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Workspaces;
using AH.Application.Workspaces.CreateWorkspace;
using AH.AppDomain.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace AH.Application.Tests.Workspaces;

// The main workspace's fixed Global Administrator, and the registration flag on creating workspaces.
public sealed class GlobalAdministratorWorkspaceTests(PostgresFixture database) : WorkspaceHandlerTestBase(database)
{
    private async Task<Result<WorkspaceSummaryResponse>> CreateWorkspaceAsync(int requesterId, string name)
    {
        await using var db = CreateDbContext();
        var handler = new CreateWorkspaceHandler(db, new CreateWorkspaceValidator(), Flags, Time, NullLogger<CreateWorkspaceHandler>.Instance);
        return await handler.HandleAsync(new CreateWorkspaceCommand(requesterId, name), Ct);
    }

    [Fact]
    public async Task ChangeRole_GlobalAdministratorByThemselves_ReturnsForbidden()
    {
        var admin = (await EnsureGlobalAdministratorAsync()).Value;

        var result = await ChangeRoleAsync(admin.MainWorkspaceId, admin.UserId, admin.UserId, WorkspaceRole.Viewer);

        Assert.Equal("Workspace.GlobalAdministratorFixed", result.Error!.Code);
    }

    [Fact]
    public async Task ChangeRole_GlobalAdministratorByAnotherAdministrator_ReturnsForbidden()
    {
        var admin = (await EnsureGlobalAdministratorAsync()).Value;
        var added = await AddMemberAsync(admin.MainWorkspaceId, admin.UserId, "deputy@example.com", WorkspaceRole.Administrator);
        var deputy = MemberId(added, "deputy@example.com");

        var demote = await ChangeRoleAsync(admin.MainWorkspaceId, deputy, admin.UserId, WorkspaceRole.Viewer);
        var remove = await RemoveMemberAsync(admin.MainWorkspaceId, deputy, admin.UserId);

        Assert.Equal(ErrorType.Forbidden, demote.Error!.Type);
        Assert.Equal(ErrorType.Forbidden, remove.Error!.Type);
    }

    [Fact]
    public async Task GetMembers_MainWorkspace_MarksGlobalAdministratorAndCertificateManagement()
    {
        var admin = (await EnsureGlobalAdministratorAsync()).Value;
        var added = await AddMemberAsync(admin.MainWorkspaceId, admin.UserId, "deputy@example.com", WorkspaceRole.Administrator);
        var deputy = MemberId(added, "deputy@example.com");

        var asAdmin = await GetMembersAsync(admin.MainWorkspaceId, admin.UserId);
        var asDeputy = await GetMembersAsync(admin.MainWorkspaceId, deputy);

        Assert.True(asAdmin.Value.IsMain);
        Assert.True(asAdmin.Value.CanManageCertificates);
        Assert.False(asDeputy.Value.CanManageCertificates);
        Assert.True(asAdmin.Value.Members.Single(m => m.UserId == admin.UserId).IsGlobalAdministrator);
        Assert.False(asAdmin.Value.Members.Single(m => m.UserId == deputy).IsGlobalAdministrator);
    }

    [Fact]
    public async Task Create_RegistrationEnabled_CreatesWorkspaceWithRequesterAsAdministrator()
    {
        var userId = await SignInAsync("john@example.com");

        var result = await CreateWorkspaceAsync(userId, "  Staging ");

        Assert.Equal("Staging", result.Value.Name);
        Assert.Equal(WorkspaceRole.Administrator, result.Value.Role);
        Assert.Equal(2, (await GetMineAsync(userId)).Value.Count);
    }

    [Fact]
    public async Task Create_RegistrationDisabled_ReturnsForbidden()
    {
        var userId = await SignInAsync("john@example.com");
        Flags.Set(FeatureFlags.RegistrationEnabled, false);

        var result = await CreateWorkspaceAsync(userId, "Staging");

        Assert.Equal("Workspace.CreationDisabled", result.Error!.Code);
    }

    [Fact]
    public async Task Create_BlankName_ReturnsValidationError()
    {
        var userId = await SignInAsync("john@example.com");

        var result = await CreateWorkspaceAsync(userId, "  ");

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}
