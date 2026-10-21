using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AH.IdentityService;
using AspNet.Security.OAuth.Yandex;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.WebUtilities;

namespace AH.WebApi.Auth;

public static class AuthenticationSetup
{
    public const string SessionCookieName = "astrahive.session";

    // Handled by the OAuth middleware (not a controller). WebApi also serves the SPA, so these sit on
    // the browser-facing origin and must match the redirect URIs registered with the providers:
    // https://localhost:7093/auth/signin-google and https://localhost:7093/auth/signin-yandex in development.
    public const string GoogleCallbackPath = "/auth/signin-google";
    public const string YandexCallbackPath = "/auth/signin-yandex";

    public const string SignInLogCategory = "AH.WebApi.Auth.ExternalSignIn";

    private static readonly Uri IdentityServiceAddress = new("http://ah-identityservice");

    public static IHostApplicationBuilder AddAstraHiveAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // GrpcChannel rejects Aspire's "https+http" scheme (no address resolver), so the scheme is explicit;
        // the host is still resolved by the service-discovery handler on the client's HttpClient. IdentityService
        // runs its "http" launch profile with HTTP/2-only endpoints, so this is h2c.
        services.AddGrpcClient<UserServiceRpc.UserServiceRpcClient>(o => o.Address = IdentityServiceAddress);
        services.AddGrpcClient<WorkspaceServiceRpc.WorkspaceServiceRpcClient>(o => o.Address = IdentityServiceAddress);
        services.AddGrpcClient<CertificateServiceRpc.CertificateServiceRpcClient>(o => o.Address = IdentityServiceAddress);
        services.AddTransient<SignInProvisioner>();
        services.AddTransient<ExternalUserProvisioner>();
        services.AddTransient<CertificateSignIn>();

        var authentication = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                // This is an API: answer with status codes instead of redirecting to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        // A provider without credentials is skipped so the API still starts; its login route then returns 400.
        var google = configuration.GetSection("Authentication:Google");
        if (IsConfigured(google))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
                options.CallbackPath = GoogleCallbackPath;
                options.ClaimActions.MapJsonKey(AstraHiveClaimTypes.Picture, "picture");
                // A Google account can carry an address it never verified; such sign-ins are rejected.
                options.ClaimActions.MapCustomJson(AstraHiveClaimTypes.EmailVerified, user =>
                    user.TryGetProperty("email_verified", out var verified) && verified.ValueKind == JsonValueKind.False ? "false" : "true");
                ConfigureRemote(options);
            });
        }

        var yandex = configuration.GetSection("Authentication:Yandex");
        if (IsConfigured(yandex))
        {
            authentication.AddYandex(options =>
            {
                options.ClientId = yandex["ClientId"]!;
                options.ClientSecret = yandex["ClientSecret"]!;
                options.CallbackPath = YandexCallbackPath;
                // Yandex returns the email, real name and avatar only when these scopes are granted.
                options.Scope.Add("login:email");
                options.Scope.Add("login:info");
                options.Scope.Add("login:avatar");
                // The default maps Name to the Yandex login; the real name is what we display.
                options.ClaimActions.Remove(ClaimTypes.Name);
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "real_name");
                options.ClaimActions.MapCustomJson(AstraHiveClaimTypes.Picture, YandexAvatarUrl);
                ConfigureRemote(options);
            });
        }

        var certificateLogin = CertificateLoginOptions.From(configuration);
        services.Configure<CertificateLoginOptions>(configuration.GetSection(CertificateLoginOptions.SectionName));
        if (certificateLogin.IsEnabled)
        {
            AddCertificateLogin(services, authentication, certificateLogin);
        }

        services.AddAuthorization();

        return builder;
    }

    private static void AddCertificateLogin(IServiceCollection services, AuthenticationBuilder authentication, CertificateLoginOptions login)
    {
        var root = login.LoadRootCertificate();
        var loginPort = login.LoginPort!.Value;

        services.Configure<KestrelServerOptions>(kestrel => kestrel.ConfigureHttpsDefaults(https =>
        {
            // Allow (not Require): a cancelled certificate picker must still reach the endpoint, which then
            // redirects back with certificate_required instead of failing the TLS handshake.
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            // TLS accepts any certificate; trust is decided by the Certificate handler against the configured
            // root only, never the OS trust store.
            https.ClientCertificateValidation = (_, _, _) => true;
            // Only the login port requests a certificate, so the browser shows no picker on the app origin.
            https.OnAuthenticate = (connection, ssl) =>
            {
                if (connection.LocalEndPoint is not IPEndPoint { Port: var port } || port != loginPort)
                {
                    ssl.ClientCertificateRequired = false;
                }
            };
        }));

        // Not the default scheme: it only runs on demand in CertificateSignIn, the session stays in the cookie.
        authentication.AddCertificate(options =>
        {
            options.AllowedCertificateTypes = CertificateTypes.Chained;
            options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
            options.CustomTrustStore = [root];
            // The dev root publishes no CRL; a production CA with revocation can switch this on.
            options.RevocationMode = X509RevocationMode.NoCheck;
            options.ValidateCertificateUse = true;
            options.ValidateValidityPeriod = true;
            options.Events = new CertificateAuthenticationEvents
            {
                OnAuthenticationFailed = context =>
                {
                    SignInLogger(context.HttpContext).LogWarning(context.Exception, "Client certificate rejected");
                    return Task.CompletedTask;
                },
            };
        });
    }

    // Called after Build: a provider skipped for missing credentials otherwise fails silently with a 400.
    public static WebApplication LogExternalProviders(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(SignInLogCategory);
        foreach (var provider in (string[])["Google", "Yandex"])
        {
            var section = app.Configuration.GetSection($"Authentication:{provider}");
            if (IsConfigured(section))
            {
                logger.LogInformation("{Provider} sign-in enabled (client id {ClientId})", provider, section["ClientId"]);
            }
            else
            {
                logger.LogWarning(
                    "{Provider} sign-in disabled: ClientId present: {HasClientId}, ClientSecret present: {HasClientSecret}. Set the AppHost secret parameters",
                    provider,
                    !string.IsNullOrWhiteSpace(section["ClientId"]),
                    !string.IsNullOrWhiteSpace(section["ClientSecret"]));
            }
        }

        var certificate = CertificateLoginOptions.From(app.Configuration);
        if (certificate.IsEnabled)
        {
            using var root = certificate.LoadRootCertificate();
            logger.LogInformation(
                "Certificate sign-in enabled on port {LoginPort}, trusting root {RootSubject} ({RootThumbprint})",
                certificate.LoginPort,
                root.Subject,
                root.Thumbprint);
        }
        else
        {
            logger.LogWarning(
                "Certificate sign-in disabled: RootCertificate present: {HasRoot}, LoginPort: {LoginPort}. Build AstraHive.AppHost (Debug) to create the dev root CA",
                !string.IsNullOrWhiteSpace(certificate.RootCertificate),
                certificate.LoginPort);
        }

        return app;
    }

    private static bool IsConfigured(IConfigurationSection section) =>
        !string.IsNullOrWhiteSpace(section["ClientId"]) && !string.IsNullOrWhiteSpace(section["ClientSecret"]);

    private static string? YandexAvatarUrl(JsonElement user) =>
        user.TryGetProperty("is_avatar_empty", out var empty) && empty.ValueKind == JsonValueKind.False
        && user.TryGetProperty("default_avatar_id", out var avatarId) && avatarId.ValueKind == JsonValueKind.String
            ? $"https://avatars.yandex.net/get-yapic/{avatarId.GetString()}/islands-200"
            : null;

    private static ILogger SignInLogger(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(SignInLogCategory);

    private static void ConfigureRemote(OAuthOptions options)
    {
        // The provider redirects back with a top-level GET, so Lax is enough.
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // A redirect_uri that differs from the one registered with the provider is the most common
        // cause of a failed sign-in, and the provider only shows it on its own error page.
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var query = QueryHelpers.ParseQuery(new Uri(context.RedirectUri).Query);
            SignInLogger(context.HttpContext).LogInformation(
                "{Provider} sign-in: redirecting to {AuthorizationEndpoint} with redirect_uri {RedirectUri} and scope {Scope}",
                context.Scheme.Name,
                context.Options.AuthorizationEndpoint,
                query.TryGetValue("redirect_uri", out var redirectUri) ? redirectUri.ToString() : "<none>",
                query.TryGetValue("scope", out var scope) ? scope.ToString() : "<none>");
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnCreatingTicket = context =>
            context.HttpContext.RequestServices.GetRequiredService<ExternalUserProvisioner>().ProvisionAsync(context);

        options.Events.OnTicketReceived = context =>
        {
            SignInLogger(context.HttpContext).LogInformation(
                "{Provider} sign-in succeeded for user {UserId}, returning to {ReturnUrl}",
                context.Scheme.Name,
                context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                context.ReturnUri);
            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = context =>
        {
            var error = context.Failure switch
            {
                ExternalSignInException signIn => signIn.Code,
                _ when context.Request.Query.ContainsKey("error") => ExternalSignInException.AccessDenied,
                _ => ExternalSignInException.SignInFailed,
            };

            // error/error_description come from the provider (e.g. access_denied, redirect_uri_mismatch);
            // the failure message covers local causes such as a missing correlation cookie or a rejected
            // token exchange (wrong client secret).
            SignInLogger(context.HttpContext).LogError(
                context.Failure,
                "{Provider} sign-in failed at {Path} ({ErrorCode}). Provider error: {ProviderError} {ProviderErrorDescription}",
                context.Scheme.Name,
                context.Request.Path,
                error,
                context.Request.Query["error"].ToString(),
                context.Request.Query["error_description"].ToString());

            context.Response.Redirect($"/login?error={error}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }
}
