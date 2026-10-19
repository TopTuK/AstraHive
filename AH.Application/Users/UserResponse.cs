namespace AH.Application.Users;

public sealed record UserResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string? PreferredLanguage,
    DateTimeOffset CreatedAt);
