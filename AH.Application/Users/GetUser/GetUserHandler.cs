using AH.Application.Abstractions;
using AH.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Users.GetUser;

public sealed class GetUserHandler(IAppDbContext db)
{
    public async Task<Result<UserResponse>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserResponse(u.Id, u.Email, u.FirstName, u.LastName, u.AvatarUrl, u.PreferredLanguage, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? Error.NotFound("User.NotFound", $"User {query.UserId} was not found.")
            : user;
    }
}
