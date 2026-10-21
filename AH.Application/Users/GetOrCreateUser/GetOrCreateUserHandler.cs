using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Users.GetOrCreateUser;

// Users are matched by email only, so the same address signed in through different methods resolves to a
// single account. Who may sign in how:
// - Google/Yandex only while ah.external_login is on, and never the Global Administrator.
// - A client certificate only when it is registered to that user and still active (issued by the Global
//   Administrator, or the bootstrap certificate); a certificate never creates a user.
// - An unknown email is registered (with its own workspace) only while ah.registration_enabled is on. A user
//   added to a workspace by email before signing in is already a member there and gets no workspace.
public sealed class GetOrCreateUserHandler(
    IAppDbContext db,
    IValidator<GetOrCreateUserCommand> validator,
    IFeatureFlags featureFlags,
    TimeProvider timeProvider,
    ILogger<GetOrCreateUserHandler> logger)
{
    public async Task<Result<UserResponse>> HandleAsync(GetOrCreateUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("User.Invalid", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var external = command.Method == SignInMethod.External;
        if (external && !featureFlags.IsEnabled(FeatureFlags.ExternalLogin))
        {
            return SignInErrors.ExternalLoginDisabled();
        }

        var email = User.NormalizeEmail(command.Email);
        var now = timeProvider.GetUtcNow();
        var registrationEnabled = featureFlags.IsEnabled(FeatureFlags.RegistrationEnabled);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        var isNew = user is null;
        if (user is null)
        {
            if (!external)
            {
                return SignInErrors.CertificateUnknown(email);
            }

            if (!registrationEnabled)
            {
                return SignInErrors.RegistrationDisabled(email);
            }

            user = User.Create(email, command.FirstName, command.LastName, command.AvatarUrl, now);
            db.Users.Add(user);
        }
        else
        {
            if (external && user.IsGlobalAdministrator)
            {
                return SignInErrors.CertificateRequired(user.Id);
            }

            if (!external && !await HasActiveCertificateAsync(user.Id, command.CertificateThumbprint!, now, cancellationToken))
            {
                return SignInErrors.CertificateUnknown(email);
            }

            user.SyncFromProvider(command.FirstName, command.LastName, command.AvatarUrl);
            user.RecordLogin(now);
        }

        // Without registration a user left without workspaces (removed everywhere) still signs in, to an empty console.
        Workspace? workspace = null;
        var hasWorkspace = !isNew && await db.Workspaces.AnyAsync(w => w.Members.Any(m => m.UserId == user.Id), cancellationToken);
        if (!hasWorkspace && registrationEnabled)
        {
            workspace = Workspace.Create(Workspace.DefaultName(user.FirstName, user.Email), user, now);
            db.Workspaces.Add(workspace);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (isNew)
        {
            // Two first-time sign-ins with the same email raced; the other one won the unique index.
            return Error.Conflict("User.Conflict", "The user was created concurrently; retry the request.");
        }

        logger.LogInformation("User {UserId} signed in with {SignInMethod} (new: {IsNew})", user.Id, command.Method, isNew);
        if (workspace is not null)
        {
            logger.LogInformation("Created workspace {WorkspaceId} for user {UserId}", workspace.Id, user.Id);
        }

        return UserResponse.From(user);
    }

    private async Task<bool> HasActiveCertificateAsync(int userId, string thumbprint, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalized = UserCertificate.NormalizeThumbprint(thumbprint);
        return await db.UserCertificates.AnyAsync(
            c => c.UserId == userId && c.Thumbprint == normalized && c.RevokedAt == null && c.NotBefore <= now && c.NotAfter > now,
            cancellationToken);
    }
}
