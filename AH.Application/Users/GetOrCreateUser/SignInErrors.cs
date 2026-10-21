using AH.Application.Common;

namespace AH.Application.Users.GetOrCreateUser;

// Codes are stable: WebApi maps them to the SPA's /login?error= values.
public static class SignInErrors
{
    public const string ExternalLoginDisabledCode = "SignIn.ExternalLoginDisabled";
    public const string RegistrationDisabledCode = "SignIn.RegistrationDisabled";
    public const string CertificateRequiredCode = "SignIn.CertificateRequired";
    public const string CertificateUnknownCode = "SignIn.CertificateUnknown";

    public static Error ExternalLoginDisabled() =>
        Error.Forbidden(ExternalLoginDisabledCode, "Sign-in with Google or Yandex is disabled; use a client certificate.");

    public static Error RegistrationDisabled(string email) =>
        Error.Forbidden(RegistrationDisabledCode, $"{email} has no account and registration is disabled.");

    public static Error CertificateRequired(int userId) =>
        Error.Forbidden(CertificateRequiredCode, $"User {userId} signs in with a client certificate only.");

    public static Error CertificateUnknown(string email) =>
        Error.Forbidden(CertificateUnknownCode, $"The certificate is not registered for {email}, revoked or expired.");
}
