namespace AH.Application.Common;

// Unleash toggles (see IFeatureFlags). Both are off unless enabled on the Unleash server.
public static class FeatureFlags
{
    // Google/Yandex sign-in. Off: every user signs in with a client certificate.
    public const string ExternalLogin = "ah.external_login";

    // Self-registration through Google/Yandex and creating additional workspaces.
    public const string RegistrationEnabled = "ah.registration_enabled";
}
