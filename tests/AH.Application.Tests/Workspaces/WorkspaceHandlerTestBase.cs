using AH.Application.Abstractions;
using AH.Application.Administration.EnsureGlobalAdministrator;
using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Users;
using AH.Application.Users.GetOrCreateUser;
using AH.Application.Workspaces;
using AH.Application.Workspaces.AddWorkspaceMember;
using AH.Application.Workspaces.ChangeWorkspaceMemberRole;
using AH.Application.Workspaces.GetMyWorkspaces;
using AH.Application.Workspaces.GetWorkspaceMembers;
using AH.Application.Workspaces.RemoveWorkspaceMember;
using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AH.Application.Tests.Workspaces;

// Every call runs on a fresh context, like a gRPC request scope in IdentityService.
public abstract class WorkspaceHandlerTestBase(PostgresFixture database) : DatabaseTestBase(database)
{
    protected static readonly DateTimeOffset Start = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    protected FakeTimeProvider Time { get; } = new(Start);

    protected FakeFeatureFlags Flags { get; } = new();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Signs the user in the way every provider does, so a first sign-in gets its own workspace.
    protected async Task<int> SignInAsync(string email, string firstName = "")
    {
        await using var db = CreateDbContext();
        var handler = new GetOrCreateUserHandler(db, new GetOrCreateUserValidator(), Flags, Time, NullLogger<GetOrCreateUserHandler>.Instance);
        var result = await handler.HandleAsync(new GetOrCreateUserCommand(email, firstName, "", null), Ct);
        return result.Value.Id;
    }

    // The workspace a user got on first sign-in (their only one in these tests).
    protected async Task<int> OwnWorkspaceIdAsync(int userId)
    {
        await using var db = CreateDbContext();
        return await db.Workspaces
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .Select(w => w.Id)
            .SingleAsync(Ct);
    }

    protected async Task<Result<IReadOnlyList<WorkspaceSummaryResponse>>> GetMineAsync(int userId)
    {
        await using var db = CreateDbContext();
        return await new GetMyWorkspacesHandler(db).HandleAsync(new GetMyWorkspacesQuery(userId), Ct);
    }

    protected async Task<Result<WorkspaceMembersResponse>> GetMembersAsync(int workspaceId, int requesterId)
    {
        await using var db = CreateDbContext();
        return await new GetWorkspaceMembersHandler(db).HandleAsync(new GetWorkspaceMembersQuery(workspaceId, requesterId), Ct);
    }

    protected async Task<Result<WorkspaceMembersResponse>> AddMemberAsync(int workspaceId, int requesterId, string email, WorkspaceRole role)
    {
        await using var db = CreateDbContext();
        var handler = new AddWorkspaceMemberHandler(db, new AddWorkspaceMemberValidator(), Time, NullLogger<AddWorkspaceMemberHandler>.Instance);
        return await handler.HandleAsync(new AddWorkspaceMemberCommand(workspaceId, requesterId, email, role), Ct);
    }

    protected async Task<Result<WorkspaceMembersResponse>> ChangeRoleAsync(int workspaceId, int requesterId, int userId, WorkspaceRole role)
    {
        await using var db = CreateDbContext();
        var handler = new ChangeWorkspaceMemberRoleHandler(db, new ChangeWorkspaceMemberRoleValidator(), NullLogger<ChangeWorkspaceMemberRoleHandler>.Instance);
        return await handler.HandleAsync(new ChangeWorkspaceMemberRoleCommand(workspaceId, requesterId, userId, role), Ct);
    }

    protected async Task<Result<WorkspaceMembersResponse>> RemoveMemberAsync(int workspaceId, int requesterId, int userId)
    {
        await using var db = CreateDbContext();
        var handler = new RemoveWorkspaceMemberHandler(db, NullLogger<RemoveWorkspaceMemberHandler>.Instance);
        return await handler.HandleAsync(new RemoveWorkspaceMemberCommand(workspaceId, requesterId, userId), Ct);
    }

    protected static int MemberId(Result<WorkspaceMembersResponse> result, string email) =>
        result.Value.Members.Single(m => m.Email == email).UserId;

    protected const string AdminEmail = "admin@example.com";

    // A certificate the Global Administrator can sign in with, as AH.CertTool would describe it.
    protected static CertificateDetails BootstrapCertificate(char fill = 'B') =>
        new(new string(fill, UserCertificate.ThumbprintLength), "0A0B", Start.AddDays(-1), Start.AddYears(2));

    protected async Task<Result<GlobalAdministratorResponse>> EnsureGlobalAdministratorAsync(
        string email = AdminEmail,
        CertificateDetails? certificate = null)
    {
        await using var db = CreateDbContext();
        var handler = new EnsureGlobalAdministratorHandler(
            db, new EnsureGlobalAdministratorValidator(), Time, NullLogger<EnsureGlobalAdministratorHandler>.Instance);
        return await handler.HandleAsync(new EnsureGlobalAdministratorCommand(email, "Ada", "Admin", certificate), Ct);
    }

    protected async Task<Result<UserResponse>> SignInWithCertificateAsync(string email, string thumbprint)
    {
        await using var db = CreateDbContext();
        var handler = new GetOrCreateUserHandler(db, new GetOrCreateUserValidator(), Flags, Time, NullLogger<GetOrCreateUserHandler>.Instance);
        return await handler.HandleAsync(
            new GetOrCreateUserCommand(email, "", "", null, SignInMethod.Certificate, thumbprint), Ct);
    }

    protected async Task<Result<UserResponse>> SignInExternallyAsync(string email, string firstName = "")
    {
        await using var db = CreateDbContext();
        var handler = new GetOrCreateUserHandler(db, new GetOrCreateUserValidator(), Flags, Time, NullLogger<GetOrCreateUserHandler>.Instance);
        return await handler.HandleAsync(new GetOrCreateUserCommand(email, firstName, "", null), Ct);
    }
}
