using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Administration.EnsureGlobalAdministrator;

// Runs on every IdentityService start, so it is idempotent: creates (or promotes) the Global Administrator,
// their main workspace and the registration of their bootstrap certificate, whichever is missing. There is
// one Global Administrator: a configured email that differs from the stored one is reported, not applied.
public sealed class EnsureGlobalAdministratorHandler(
    IAppDbContext db,
    IValidator<EnsureGlobalAdministratorCommand> validator,
    TimeProvider timeProvider,
    ILogger<EnsureGlobalAdministratorHandler> logger)
{
    public async Task<Result<GlobalAdministratorResponse>> HandleAsync(EnsureGlobalAdministratorCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("GlobalAdministrator.Invalid", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var email = User.NormalizeEmail(command.Email);
        var now = timeProvider.GetUtcNow();

        var user = await db.Users.FirstOrDefaultAsync(u => u.IsGlobalAdministrator, cancellationToken);
        if (user is not null && user.Email != email)
        {
            return Error.Conflict(
                "GlobalAdministrator.AlreadyExists",
                $"The Global Administrator is already {user.Email}; the configured {email} is ignored.");
        }

        user ??= await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            user = User.CreateGlobalAdministrator(email, command.FirstName, command.LastName, now);
            db.Users.Add(user);
            logger.LogInformation("Creating the Global Administrator {Email}", email);
        }
        else if (!user.IsGlobalAdministrator)
        {
            user.PromoteToGlobalAdministrator();
            logger.LogInformation("Promoting existing user {UserId} to Global Administrator", user.Id);
        }

        var main = await db.Workspaces
            .Include(w => w.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.IsMain, cancellationToken);
        if (main is null)
        {
            main = Workspace.CreateMain(user, now);
            db.Workspaces.Add(main);
            logger.LogInformation("Creating the main workspace for the Global Administrator");
        }
        else if (main.FindMember(user.Id) is null)
        {
            main.AddMember(user, WorkspaceRole.Administrator, now);
        }
        else
        {
            main.ChangeRole(user.Id, WorkspaceRole.Administrator);
        }

        // Saved first: the certificate row needs the user's id.
        await db.SaveChangesAsync(cancellationToken);

        if (command.Certificate is { } certificate)
        {
            await RegisterCertificateAsync(user, certificate, now, cancellationToken);
        }

        return new GlobalAdministratorResponse(user.Id, user.Email, main.Id);
    }

    private async Task RegisterCertificateAsync(User user, CertificateDetails certificate, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var thumbprint = UserCertificate.NormalizeThumbprint(certificate.Thumbprint);
        var existing = await db.UserCertificates.FirstOrDefaultAsync(c => c.Thumbprint == thumbprint, cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId != user.Id)
            {
                logger.LogWarning("Bootstrap certificate {Thumbprint} is registered to user {UserId}, not the Global Administrator", thumbprint, existing.UserId);
            }

            // A revoked bootstrap certificate stays revoked: issue a new one with AH.CertTool.
            return;
        }

        db.UserCertificates.Add(UserCertificate.Register(
            user.Id,
            thumbprint,
            certificate.SerialNumber,
            certificate.NotBefore,
            certificate.NotAfter,
            objectKey: null,
            issuedById: null,
            now));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered bootstrap certificate {Thumbprint} for the Global Administrator {UserId}", thumbprint, user.Id);
    }
}

public sealed record GlobalAdministratorResponse(int UserId, string Email, int MainWorkspaceId);
