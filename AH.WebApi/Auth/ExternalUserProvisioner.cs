using System.Security.Claims;
using AH.IdentityService;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace AH.WebApi.Auth;

// Runs inside the OAuth handler after the provider returned the user profile:
// registers the user in IdentityService and swaps the provider identity for an
// AstraHive identity keyed by the internal user id.
public sealed class ExternalUserProvisioner(UserServiceRpc.UserServiceRpcClient users, ILogger<ExternalUserProvisioner> logger)
{
    public async Task ProvisionAsync(OAuthCreatingTicketContext context)
    {
        var external = context.Identity!;
        var email = external.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("{Provider} sign-in returned no email", context.Scheme.Name);
            throw new ExternalSignInException(ExternalSignInException.EmailRequired);
        }

        logger.LogInformation("{Provider} returned profile for {Email}; provisioning in IdentityService", context.Scheme.Name, email);

        var (firstName, lastName) = ResolveNames(external);
        var request = new GetUserRequest
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = external.FindFirst(AstraHiveClaimTypes.Picture)?.Value,
        };

        GetUserReply user;
        try
        {
            user = await users.GetOrCreateUserAsync(request, cancellationToken: context.HttpContext.RequestAborted);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "IdentityService failed to provision {Provider} user {Email}: {StatusCode} {Detail}", context.Scheme.Name, email, ex.StatusCode, ex.Status.Detail);
            throw new ExternalSignInException(ExternalSignInException.SignInFailed, ex);
        }

        logger.LogInformation("{Provider} user {Email} mapped to user {UserId}", context.Scheme.Name, email, user.Id);

        context.Principal = UserClaims.CreatePrincipal(user, context.Scheme.Name, external.AuthenticationType);
    }

    // Providers do not always split the name, so fall back to splitting the display name.
    private static (string FirstName, string LastName) ResolveNames(ClaimsIdentity identity)
    {
        var given = identity.FindFirst(ClaimTypes.GivenName)?.Value;
        var family = identity.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(given))
        {
            return (given, family);
        }

        var name = identity.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            return (string.Empty, family);
        }

        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : family);
    }
}

// Thrown from OnCreatingTicket: the OAuth handler ignores context.Fail() there, but turns
// exceptions into a RemoteFailure event, where the code becomes the SPA's ?error= value.
public sealed class ExternalSignInException(string code, Exception? innerException = null)
    : Exception($"External sign-in failed: {code}", innerException)
{
    public const string EmailRequired = "email_required";
    public const string SignInFailed = "sign_in_failed";
    public const string AccessDenied = "access_denied";

    public string Code { get; } = code;
}
