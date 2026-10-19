using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Users.GetOrCreateUser;

// Users are matched by email only, so the same address signed in through
// different external providers resolves to a single account.
public sealed class GetOrCreateUserHandler(
    IAppDbContext db,
    IValidator<GetOrCreateUserCommand> validator,
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

        var email = User.NormalizeEmail(command.Email);
        var now = timeProvider.GetUtcNow();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        var isNew = user is null;
        if (user is null)
        {
            user = User.Create(email, command.FirstName, command.LastName, command.AvatarUrl, now);
            db.Users.Add(user);
        }
        else
        {
            user.SyncFromProvider(command.FirstName, command.LastName, command.AvatarUrl);
            user.RecordLogin(now);
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

        logger.LogInformation("User {UserId} signed in (new: {IsNew})", user.Id, isNew);

        return new UserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.PreferredLanguage,
            user.CreatedAt);
    }
}
