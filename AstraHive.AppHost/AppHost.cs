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

// OAuth credentials: set once with `dotnet user-secrets --project AstraHive.AppHost set "Parameters:<name>" "<value>"`.
var googleClientId = builder.AddParameter("google-client-id", secret: true);
var googleClientSecret = builder.AddParameter("google-client-secret", secret: true);
var yandexClientId = builder.AddParameter("yandex-client-id", secret: true);
var yandexClientSecret = builder.AddParameter("yandex-client-secret", secret: true);

// IdentityService owns the users schema (applies migrations on startup in development).
var identityService = builder.AddProject<Projects.AH_IdentityService>("ah-identityservice")
    .WithReference(postgres)
    .WaitFor(postgres);

// The browser-facing host. The "https" launch profile pins https://localhost:7093, the origin of
// the redirect URIs registered with Google/Yandex (/auth/signin-google, /auth/signin-yandex).
var webApi = builder.AddProject<Projects.AH_WebApi>("ah-webapi", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithEnvironment("Authentication__Google__ClientId", googleClientId)
    .WithEnvironment("Authentication__Google__ClientSecret", googleClientSecret)
    .WithEnvironment("Authentication__Yandex__ClientId", yandexClientId)
    .WithEnvironment("Authentication__Yandex__ClientSecret", yandexClientSecret);

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

builder.Build().Run();
