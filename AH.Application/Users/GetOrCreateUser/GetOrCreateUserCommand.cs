namespace AH.Application.Users.GetOrCreateUser;

public sealed record GetOrCreateUserCommand(string Email, string FirstName, string LastName, string? AvatarUrl);
