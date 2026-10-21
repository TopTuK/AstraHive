using System.Net.Mail;
using AH.IdentityService;
using Grpc.Core;

namespace AH.WebApi.Auth;

// The one path every sign-in method (Google, Yandex, client certificate) takes to an AstraHive user:
// the same email check, then IdentityService GetOrCreateUser, which matches the user by email and applies
// the sign-in rules (feature flags, Global Administrator, registered certificates). Failures are
// ExternalSignInException codes for the SPA's ?error=.
public sealed class SignInProvisioner(UserServiceRpc.UserServiceRpcClient users, ILogger<SignInProvisioner> logger)
{
    // Matches User.EmailMaxLength in AH.AppDomain (WebApi talks to IdentityService over gRPC only).
    private const int EmailMaxLength = 320;

    // IdentityService's "error-code" trailer (SignInErrors in AH.Application) → the SPA's ?error= value.
    private const string ErrorCodeTrailer = "error-code";
    private static readonly Dictionary<string, string> SignInErrorCodes = new(StringComparer.Ordinal)
    {
        ["SignIn.ExternalLoginDisabled"] = ExternalSignInException.ExternalLoginDisabled,
        ["SignIn.RegistrationDisabled"] = ExternalSignInException.RegistrationDisabled,
        ["SignIn.CertificateRequired"] = ExternalSignInException.CertificateRequired,
        ["SignIn.CertificateUnknown"] = ExternalSignInException.CertificateUnknown,
    };

    public async Task<GetUserReply> ProvisionAsync(SignInProfile profile, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            logger.LogWarning("{Provider} sign-in returned no email", profile.Provider);
            throw new ExternalSignInException(ExternalSignInException.EmailRequired);
        }

        var email = profile.Email.Trim();
        if (email.Length > EmailMaxLength || !MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            logger.LogWarning("{Provider} sign-in returned an invalid email {Email}", profile.Provider, email);
            throw new ExternalSignInException(ExternalSignInException.EmailInvalid);
        }

        if (!profile.EmailVerified)
        {
            logger.LogWarning("{Provider} sign-in returned unverified email {Email}", profile.Provider, email);
            throw new ExternalSignInException(ExternalSignInException.EmailUnverified);
        }

        var request = new GetUserRequest
        {
            Email = email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            AvatarUrl = profile.AvatarUrl,
            SignInMethod = profile.Method,
            CertificateThumbprint = profile.CertificateThumbprint,
        };

        GetUserReply user;
        try
        {
            user = await users.GetOrCreateUserAsync(request, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied
            && ex.Trailers.GetValue(ErrorCodeTrailer) is { } errorCode
            && SignInErrorCodes.TryGetValue(errorCode, out var code))
        {
            logger.LogWarning("{Provider} sign-in for {Email} rejected: {ErrorCode} ({Detail})", profile.Provider, email, code, ex.Status.Detail);
            throw new ExternalSignInException(code, ex);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "IdentityService failed to provision {Provider} user {Email}: {StatusCode} {Detail}", profile.Provider, email, ex.StatusCode, ex.Status.Detail);
            throw new ExternalSignInException(ExternalSignInException.SignInFailed, ex);
        }

        logger.LogInformation("{Provider} user {Email} mapped to user {UserId}", profile.Provider, email, user.Id);
        return user;
    }
}

// EmailVerified is false only when the source says so explicitly (Google's email_verified); Yandex returns
// its verified default email, and a certificate's email is vouched for by the trusted root.
// CertificateThumbprint (SHA-256 hex) is set for SignInMethod.Certificate.
public sealed record SignInProfile(
    string Provider,
    string? Email,
    string FirstName,
    string LastName,
    string? AvatarUrl = null,
    SignInMethod Method = SignInMethod.External,
    bool EmailVerified = true,
    string? CertificateThumbprint = null);
