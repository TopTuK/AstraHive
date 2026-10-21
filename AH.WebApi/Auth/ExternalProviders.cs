using AspNet.Security.OAuth.Yandex;
using Microsoft.AspNetCore.Authentication.Google;

namespace AH.WebApi.Auth;

public static class ExternalProviders
{
    private static readonly Dictionary<string, string> SchemesByRouteName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["google"] = GoogleDefaults.AuthenticationScheme,
        ["yandex"] = YandexAuthenticationDefaults.AuthenticationScheme,
    };

    public static IEnumerable<string> RouteNames => SchemesByRouteName.Keys;

    public static bool TryGetScheme(string provider, out string scheme) =>
        SchemesByRouteName.TryGetValue(provider, out scheme!);
}

public static class AstraHiveClaimTypes
{
    public const string Picture = "picture";
    // "Google", "Yandex" or CertificateSignIn.ProviderName.
    public const string Provider = "provider";
    // Only during the OAuth callback (Google's "true"/"false"); never stored in the session cookie.
    public const string EmailVerified = "email_verified";
}
