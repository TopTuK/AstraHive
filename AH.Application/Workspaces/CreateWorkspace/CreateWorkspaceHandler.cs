using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Workspaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Workspaces.CreateWorkspace;

// Additional workspaces are part of self-service registration, so they share the ah.registration_enabled flag.
public sealed class CreateWorkspaceHandler(
    IAppDbContext db,
    IValidator<CreateWorkspaceCommand> validator,
    IFeatureFlags featureFlags,
    TimeProvider timeProvider,
    ILogger<CreateWorkspaceHandler> logger)
{
    public async Task<Result<WorkspaceSummaryResponse>> HandleAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        if (!featureFlags.IsEnabled(FeatureFlags.RegistrationEnabled))
        {
            return Error.Forbidden("Workspace.CreationDisabled", "Creating workspaces is disabled.");
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("Workspace.Invalid", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.RequesterId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User {command.RequesterId} was not found.");
        }

        var workspace = Workspace.Create(command.Name, user, timeProvider.GetUtcNow());
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created workspace {WorkspaceId}", user.Id, workspace.Id);

        return new WorkspaceSummaryResponse(workspace.Id, workspace.Name, WorkspaceRole.Administrator, 1, workspace.IsMain);
    }
}
