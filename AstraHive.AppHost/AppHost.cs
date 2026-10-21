var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("postgres-user", "astrahive");
var pgPassword = builder.AddParameter("postgres-password", "astrahive", secret: true);
var postgres = builder
    .AddPostgres("postgres", port: 5432)
    .WithEnvironment("POSTGRES_USER", "astrahive") // for scripts
    .WithEnvironment("POSTGRES_PASSWORD", "astrahive") // for scripts
    .WithEnvironment("POSTGRES_DB", "astrahive") // for scripts
    .WithUserName(pgUser)
    .WithPassword(pgPassword)
    // Keeps users across `aspire run` restarts; without it each run starts with an empty database
    // while browsers still hold valid session cookies for the users it lost.
    .WithDataVolume("astrahive-db")
    .AddDatabase("astrahive");

// Object storage (issued client certificates, later uploads). Only IdentityService talks to it.
var minio = builder
    .AddMinioContainer("minio")
    .WithDataVolume("astrahive-minio");

// OAuth credentials: set once with `dotnet user-secrets --project AstraHive.AppHost set "Parameters:<name>" "<value>"`.
var googleClientId = builder.AddParameter("google-client-id", secret: true);
var googleClientSecret = builder.AddParameter("google-client-secret", secret: true);
var yandexClientId = builder.AddParameter("yandex-client-id", secret: true);
var yandexClientSecret = builder.AddParameter("yandex-client-secret", secret: true);

// Feature flags (ah.external_login, ah.registration_enabled). The token is a server-side (client API) token:
// set it once with `dotnet user-secrets --project AstraHive.AppHost set "Parameters:unleash-api-token" "<token>"`.
var unleashApiUrl = builder.AddParameter("unleash-api-url", "https://features.s-sidorov.ru/api/");
var unleashApiToken = builder.AddParameter("unleash-api-token", secret: true);

// Certificate sign-in: only this port asks the browser for a client certificate, so Google/Yandex users never
// see a certificate picker. Not proxied: WebApi recognises the port by the socket's local endpoint.
const int certificateLoginPort = 7094;

// The root CA (public part for WebApi, plus its key for IdentityService, which issues member certificates) and
// the Global Administrator, whose certificate IdentityService registers on startup. In run mode they come from
// the dev certificates that the EnsureDevCertificates target of this project's Debug build creates (see
// AstraHive.AppHost.csproj); for a deployment, from parameters filled with the output of `AH.CertTool init`.
CertificateSettings certificates;
if (builder.ExecutionContext.IsRunMode)
{
    var devCertsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstraHive", "dev-certs");
    // Must match DevCertUserEmail in AstraHive.AppHost.csproj.
    const string devAdminEmail = "admin@astrahive.local";
    certificates = new CertificateSettings(
        Root: ExistingFile(Path.Combine(devCertsDir, "root.pem")),
        RootKey: ExistingFile(Path.Combine(devCertsDir, "root.key.pem")),
        AdminEmail: devAdminEmail,
        AdminName: "Global Admin",
        AdminCertificate: ExistingFile(Path.Combine(devCertsDir, $"{devAdminEmail}.pem")));
}
else
{
    certificates = new CertificateSettings(
        Root: builder.AddParameter("certificate-root"),
        RootKey: builder.AddParameter("certificate-root-key", secret: true),
        AdminEmail: builder.AddParameter("global-admin-email"),
        AdminName: builder.AddParameter("global-admin-name"),
        AdminCertificate: builder.AddParameter("global-admin-certificate"));
}

// IdentityService owns the users schema (applies migrations on startup in development).
var identityService = builder.AddProject<Projects.AH_IdentityService>("ah-identityservice")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(minio)
    .WaitFor(minio)
    .WithEnvironment("Unleash__ApiUrl", unleashApiUrl)
    .WithEnvironment("Unleash__ApiToken", unleashApiToken)
    .WithSetting("CertificateAuthority__Certificate", certificates.Root)
    .WithSetting("CertificateAuthority__PrivateKey", certificates.RootKey)
    .WithSetting("GlobalAdmin__Email", certificates.AdminEmail)
    .WithSetting("GlobalAdmin__Name", certificates.AdminName)
    .WithSetting("GlobalAdmin__Certificate", certificates.AdminCertificate);

// The browser-facing host. The "https" launch profile pins https://localhost:7093, the origin of
// the redirect URIs registered with Google/Yandex (/auth/signin-google, /auth/signin-yandex).
var webApi = builder.AddProject<Projects.AH_WebApi>("ah-webapi", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithEnvironment("Unleash__ApiUrl", unleashApiUrl)
    .WithEnvironment("Unleash__ApiToken", unleashApiToken)
    .WithEnvironment("Authentication__Google__ClientId", googleClientId)
    .WithEnvironment("Authentication__Google__ClientSecret", googleClientSecret)
    .WithEnvironment("Authentication__Yandex__ClientId", yandexClientId)
    .WithEnvironment("Authentication__Yandex__ClientSecret", yandexClientSecret)
    .WithHttpsEndpoint(port: certificateLoginPort, name: "certificate", isProxied: false)
    .WithEnvironment("Authentication__Certificate__LoginPort", certificateLoginPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
    .WithSetting("Authentication__Certificate__RootCertificate", certificates.Root);
webApi.WithEnvironment("Authentication__Certificate__AppOrigin", webApi.GetEndpoint("https"));

// Development only: `vite build --watch` rebuilds the SPA into AH.WebApi/wwwroot on every change and
// AH.WebApi serves it, so the SPA, /api and the OAuth callbacks share one origin. Published output
// needs no frontend resource: a Release build of AH.WebApi runs `npm run build` itself (BuildSpa target).
if (builder.ExecutionContext.IsRunMode)
{
    var webApp = builder.AddJavaScriptApp("ah-webapp", "../ah.webapp", runScriptName: "build:watch");

    // A watcher never reports healthy, so wait only until it has started.
    webApi.WaitForStart(webApp);
}

builder.AddProject<Projects.AH_SiloHost>("ah-silohost");

builder
    .Build()
    .Run();

// A dev certificate file that the build has not created yet stays unset: the feature it feeds is then off.
static string? ExistingFile(string path) => File.Exists(path) ? path : null;

// Each value is a dev file path (run mode) or a deployment parameter holding the PEM text or the name/email.
internal sealed record CertificateSettings(object? Root, object? RootKey, object AdminEmail, object AdminName, object? AdminCertificate);

internal static class SettingExtensions
{
    // A setting is a plain value (dev file path) or a deployment parameter; null leaves it unset.
    public static IResourceBuilder<T> WithSetting<T>(this IResourceBuilder<T> builder, string name, object? value)
        where T : IResourceWithEnvironment => value switch
        {
            null => builder,
            string text => builder.WithEnvironment(name, text),
            IResourceBuilder<ParameterResource> parameter => builder.WithEnvironment(name, parameter),
            _ => throw new ArgumentException($"Unsupported setting value {value.GetType()} for {name}.", nameof(value)),
        };
}
