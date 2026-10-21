using AH.AppDomain.Users;

namespace AH.Application.Users;

public sealed record UserResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string? PreferredLanguage,
    DateTimeOffset CreatedAt)
{
    internal static UserResponse From(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.AvatarUrl, user.PreferredLanguage, user.CreatedAt);
}
