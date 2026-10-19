using AH.Application.Abstractions;
using AH.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Users.UpdateUserProfile;

public sealed class UpdateUserProfileHandler(
    IAppDbContext db,
    IValidator<UpdateUserProfileCommand> validator,
    ILogger<UpdateUserProfileHandler> logger)
{
    public async Task<Result<UserResponse>> HandleAsync(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("User.Invalid", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User {command.UserId} was not found.");
        }

        user.Rename(command.FirstName, command.LastName);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated profile", user.Id);

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
