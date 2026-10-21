using System.Text.Json.Serialization;
using AH.WebApi.Auth;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Enums (workspace roles) travel as their names, which the SPA reads and sends.
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails();

    builder.AddFeatureFlagClient();
    builder.AddAstraHiveAuthentication();

    var app = builder.Build();

    app.LogExternalProviders();

    app.MapDefaultEndpoints();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // The browser talks to this host only (API + SPA on one origin), and OAuth redirect URIs
    // registered with Google/Yandex are https, so plain-http requests are bounced to https.
    app.UseHttpsRedirection();

    // The SPA bundle (vite build → wwwroot; rebuilt by `build:watch` under Aspire) is served from here.
    // UseStaticFiles (not MapStaticAssets) reads wwwroot from disk, so runtime rebuilds are picked up.
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Unknown API routes must stay 404s instead of falling through to the SPA's index.html.
    app.MapFallback("/api/{**path}", () => Results.NotFound());

    app.MapFallbackToFile("index.html");

    await app.RunAsync();
}
// Last-chance fatal log for startup failures; HostAbortedException is how EF tooling stops the host.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
