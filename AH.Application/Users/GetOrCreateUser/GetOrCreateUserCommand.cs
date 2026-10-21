using AH.AppDomain.Users;

namespace AH.Application.Users.GetOrCreateUser;

// CertificateThumbprint is required for SignInMethod.Certificate (SHA-256, hex).
public sealed record GetOrCreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    SignInMethod Method = SignInMethod.External,
    string? CertificateThumbprint = null);
