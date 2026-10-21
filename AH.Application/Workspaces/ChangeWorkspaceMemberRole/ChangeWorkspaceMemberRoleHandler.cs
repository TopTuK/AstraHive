using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Workspaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AH.Application.Workspaces.ChangeWorkspaceMemberRole;

public sealed class ChangeWorkspaceMemberRoleHandler(
    IAppDbContext db,
    IValidator<ChangeWorkspaceMemberRoleCommand> validator,
    ILogger<ChangeWorkspaceMemberRoleHandler> logger)
{
    public async Task<Result<WorkspaceMembersResponse>> HandleAsync(ChangeWorkspaceMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("Workspace.InvalidRole", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

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

        if (command.Role != WorkspaceRole.Administrator && workspace.IsProtectedMember(command.UserId))
        {
            return WorkspaceAccess.GlobalAdministratorFixed(command.UserId);
        }

        if (command.Role != WorkspaceRole.Administrator && workspace.IsLastAdministrator(command.UserId))
        {
            return WorkspaceAccess.LastAdministrator(command.UserId);
        }

        workspace.ChangeRole(command.UserId, command.Role);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {RequesterId} set the role of user {UserId} in workspace {WorkspaceId} to {Role}",
            command.RequesterId,
            command.UserId,
            workspace.Id,
            command.Role);

        return WorkspaceMembersResponse.From(workspace, command.RequesterId);
    }
}
