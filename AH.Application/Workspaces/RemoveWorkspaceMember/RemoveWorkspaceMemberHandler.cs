using AH.Application.Abstractions;
using AH.Application.Common;
using Microsoft.Extensions.Logging;

namespace AH.Application.Workspaces.RemoveWorkspaceMember;

// The removed user keeps their account and any other workspaces. Administrators may remove themselves
// as long as another Administrator remains.
public sealed class RemoveWorkspaceMemberHandler(IAppDbContext db, ILogger<RemoveWorkspaceMemberHandler> logger)
{
    public async Task<Result<WorkspaceMembersResponse>> HandleAsync(RemoveWorkspaceMemberCommand command, CancellationToken cancellationToken)
    {
        var loaded = await WorkspaceAccess.LoadForAdministratorAsync(db, command.WorkspaceId, command.RequesterId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var workspace = loaded.Value;
        if (workspace.FindMember(command.UserId) is null)
        {
            return WorkspaceAccess.MemberNotFound(command.UserId);
        }

        if (workspace.IsProtectedMember(command.UserId))
        {
            return WorkspaceAccess.GlobalAdministratorFixed(command.UserId);
        }

        if (workspace.IsLastAdministrator(command.UserId))
        {
            return WorkspaceAccess.LastAdministrator(command.UserId);
        }

        workspace.RemoveMember(command.UserId);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {RequesterId} removed user {UserId} from workspace {WorkspaceId}",
            command.RequesterId,
            command.UserId,
            workspace.Id);

        return WorkspaceMembersResponse.From(workspace, command.RequesterId);
    }
}
