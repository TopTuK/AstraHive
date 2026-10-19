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

    public static bool TryGetScheme(string provider, out string scheme) =>
        SchemesByRouteName.TryGetValue(provider, out scheme!);
}

public static class AstraHiveClaimTypes
{
    public const string Picture = "picture";
    public const string Provider = "provider";
}
