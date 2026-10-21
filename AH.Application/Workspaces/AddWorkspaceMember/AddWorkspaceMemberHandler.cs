using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Workspaces.AddWorkspaceMember;

// Members are added by email. An address that never signed in becomes a pending user, which the first
// sign-in with that email (any provider, matched like GetOrCreateUser) turns into a regular account.
public sealed class AddWorkspaceMemberHandler(
    IAppDbContext db,
    IValidator<AddWorkspaceMemberCommand> validator,
    TimeProvider timeProvider,
    ILogger<AddWorkspaceMemberHandler> logger)
{
    public async Task<Result<WorkspaceMembersResponse>> HandleAsync(AddWorkspaceMemberCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("Workspace.InvalidMember", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var loaded = await WorkspaceAccess.LoadForAdministratorAsync(db, command.WorkspaceId, command.RequesterId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var workspace = loaded.Value;
        var email = User.NormalizeEmail(command.Email);
        var now = timeProvider.GetUtcNow();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is not null && workspace.FindMember(user.Id) is not null)
        {
            return Error.Conflict("Workspace.AlreadyMember", $"{email} is already a member of this workspace.");
        }

        if (user is null)
        {
            user = User.CreatePending(email, now);
            db.Users.Add(user);
        }

        workspace.AddMember(user, command.Role, now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request added the same member, or the same email signed in for the first time.
            return Error.Conflict("Workspace.Conflict", "The members were changed concurrently; retry the request.");
        }

        logger.LogInformation(
            "User {RequesterId} added user {UserId} (pending: {IsPending}) to workspace {WorkspaceId} as {Role}",
            command.RequesterId,
            user.Id,
            user.IsPending,
            workspace.Id,
            command.Role);

        return WorkspaceMembersResponse.From(workspace, command.RequesterId);
    }
}
