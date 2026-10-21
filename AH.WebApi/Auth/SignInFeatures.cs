using Unleash;

namespace AH.WebApi.Auth;

// Unleash flags that shape sign-in; the names match FeatureFlags in AH.Application. IdentityService enforces
// them on every sign-in. WebApi reads them only to skip a doomed OAuth round trip and to tell the SPA what to offer.
public static class SignInFeatures
{
    public const string ExternalLogin = "ah.external_login";
    public const string Registration = "ah.registration_enabled";

    public static bool IsExternalLoginEnabled(this IUnleash unleash) => unleash.IsEnabled(ExternalLogin, defaultSetting: false);

    public static bool IsRegistrationEnabled(this IUnleash unleash) => unleash.IsEnabled(Registration, defaultSetting: false);
}
