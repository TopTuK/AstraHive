namespace AH.Application.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(int UserId, string FirstName, string LastName);
