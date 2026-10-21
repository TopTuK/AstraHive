using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Workspaces;

// Membership is checked per request against the database (not cookie claims), so a removed member or a
// changed role takes effect immediately. Non-members get NotFound: they must not learn the workspace exists.
internal static class WorkspaceAccess
{
    public static async Task<Result<Workspace>> LoadForMemberAsync(IAppDbContext db, int workspaceId, int requesterId, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .Include(w => w.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        return workspace?.FindMember(requesterId) is null
            ? Error.NotFound("Workspace.NotFound", $"Workspace {workspaceId} was not found.")
            : workspace;
    }

    public static async Task<Result<Workspace>> LoadForAdministratorAsync(IAppDbContext db, int workspaceId, int requesterId, CancellationToken cancellationToken)
    {
        var result = await LoadForMemberAsync(db, workspaceId, requesterId, cancellationToken);
        if (result.IsSuccess && !result.Value.IsAdministrator(requesterId))
        {
            return Error.Forbidden("Workspace.AdministratorRequired", "Only workspace administrators can manage members.");
        }

        return result;
    }

    public static Error LastAdministrator(int userId) =>
        Error.Conflict("Workspace.LastAdministrator", $"User {userId} is the last administrator; make another member an administrator first.");

    public static Error GlobalAdministratorFixed(int userId) =>
        Error.Forbidden("Workspace.GlobalAdministratorFixed", $"User {userId} is the Global Administrator; their role cannot be changed.");

    public static Error MemberNotFound(int userId) =>
        Error.NotFound("Workspace.MemberNotFound", $"User {userId} is not a member of this workspace.");
}
