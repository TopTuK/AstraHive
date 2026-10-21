using System.Security.Claims;
using AH.IdentityService;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace AH.WebApi.Auth;

// Runs inside the OAuth handler after the provider returned the user profile:
// registers the user in IdentityService and swaps the provider identity for an
// AstraHive identity keyed by the internal user id.
public sealed class ExternalUserProvisioner(SignInProvisioner provisioner)
{
    public async Task ProvisionAsync(OAuthCreatingTicketContext context)
    {
        var external = context.Identity!;
        var (firstName, lastName) = ResolveNames(external);
        var profile = new SignInProfile(
            context.Scheme.Name,
            external.FindFirst(ClaimTypes.Email)?.Value,
            firstName,
            lastName,
            external.FindFirst(AstraHiveClaimTypes.Picture)?.Value,
            SignInMethod.External,
            EmailVerified: external.FindFirst(AstraHiveClaimTypes.EmailVerified)?.Value != "false");

        var user = await provisioner.ProvisionAsync(profile, context.HttpContext.RequestAborted);

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

        return PersonName.Split(identity.FindFirst(ClaimTypes.Name)?.Value, family);
    }
}

// Thrown from OnCreatingTicket: the OAuth handler ignores context.Fail() there, but turns
// exceptions into a RemoteFailure event, where the code becomes the SPA's ?error= value.
public sealed class ExternalSignInException(string code, Exception? innerException = null)
    : Exception($"External sign-in failed: {code}", innerException)
{
    public const string EmailRequired = "email_required";
    public const string EmailInvalid = "email_invalid";
    public const string EmailUnverified = "email_unverified";
    public const string SignInFailed = "sign_in_failed";
    public const string AccessDenied = "access_denied";
    public const string CertificateRequired = "certificate_required";
    public const string CertificateInvalid = "certificate_invalid";
    public const string CertificateUnavailable = "certificate_unavailable";
    public const string CertificateUnknown = "certificate_unknown";
    public const string ExternalLoginDisabled = "external_login_disabled";
    public const string RegistrationDisabled = "registration_disabled";

    public string Code { get; } = code;
}
