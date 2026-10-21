using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AH.IdentityService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;

namespace AH.WebApi.Auth;

// Runs on the certificate login port: validates the TLS client certificate against the configured root
// (Certificate handler), then provisions its email through SignInProvisioner like an OAuth sign-in (same
// email check, same GetOrCreateUser). IdentityService also requires the certificate's thumbprint to be
// registered to that user and not revoked. Returns the AstraHive identity for the session cookie.
public sealed class CertificateSignIn(SignInProvisioner provisioner, ILogger<CertificateSignIn> logger)
{
    public const string ProviderName = "Certificate";

    public async Task<CertificateSignInResult> SignInAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var authentication = await httpContext.AuthenticateAsync(CertificateAuthenticationDefaults.AuthenticationScheme);
        if (authentication.None)
        {
            logger.LogInformation("Certificate sign-in: the browser presented no client certificate");
            return CertificateSignInResult.Failed(ExternalSignInException.CertificateRequired);
        }

        var certificate = httpContext.Connection.ClientCertificate;
        if (!authentication.Succeeded || certificate is null)
        {
            logger.LogWarning(
                authentication.Failure,
                "Certificate sign-in rejected certificate {Subject} issued by {Issuer}",
                certificate?.Subject,
                certificate?.Issuer);
            return CertificateSignInResult.Failed(ExternalSignInException.CertificateInvalid);
        }

        var email = certificate.GetNameInfo(X509NameType.EmailName, forIssuer: false);
        var commonName = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        var (firstName, lastName) = PersonName.Split(string.Equals(commonName, email, StringComparison.OrdinalIgnoreCase) ? null : commonName);

        GetUserReply user;
        try
        {
            var profile = new SignInProfile(
                ProviderName,
                email,
                firstName,
                lastName,
                Method: SignInMethod.Certificate,
                CertificateThumbprint: certificate.GetCertHashString(HashAlgorithmName.SHA256));
            user = await provisioner.ProvisionAsync(profile, cancellationToken);
        }
        catch (ExternalSignInException ex)
        {
            logger.LogWarning("Certificate sign-in for {Subject} ({Thumbprint}) failed: {ErrorCode}", certificate.Subject, certificate.Thumbprint, ex.Code);
            return CertificateSignInResult.Failed(ex.Code);
        }

        logger.LogInformation("Certificate {Thumbprint} for {Email} mapped to user {UserId}", certificate.Thumbprint, email, user.Id);

        return CertificateSignInResult.Succeeded(
            UserClaims.CreatePrincipal(user, ProviderName, CertificateAuthenticationDefaults.AuthenticationScheme));
    }
}

public sealed record CertificateSignInResult(ClaimsPrincipal? Principal, string? Error)
{
    public static CertificateSignInResult Succeeded(ClaimsPrincipal principal) => new(principal, null);

    public static CertificateSignInResult Failed(string error) => new(null, error);
}
