using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unleash;

namespace Microsoft.Extensions.Hosting;

// The Unleash client shared by the hosts, from the "Unleash" section (ApiUrl, ApiToken, optional AppName),
// which the AppHost sets from its unleash-* parameters. Without a URL or token every flag reads as off, which
// is the secure default (certificate sign-in only, no registration).
public static class FeatureFlagExtensions
{
    public const string SectionName = "Unleash";

    private const string LogCategory = "AstraHive.FeatureFlags";

    public static TBuilder AddFeatureFlagClient<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var section = builder.Configuration.GetSection(SectionName);
        var apiUrl = section["ApiUrl"];
        var apiToken = section["ApiToken"];
        var appName = section["AppName"] ?? builder.Environment.ApplicationName;

        builder.Services.AddSingleton<IUnleash>(services =>
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategory);
            if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiToken))
            {
                logger.LogWarning("Unleash is not configured (Unleash:ApiUrl, Unleash:ApiToken): every feature flag is off");
                return new FakeUnleash();
            }

            var settings = new UnleashSettings
            {
                AppName = appName,
                InstanceTag = Environment.MachineName,
                // The SDK appends "client/features"; without the trailing slash Uri drops the last segment.
                UnleashApi = new Uri(apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/"),
                CustomHttpHeaders = new Dictionary<string, string> { ["Authorization"] = apiToken },
                FetchTogglesInterval = TimeSpan.FromSeconds(15),
            };
            logger.LogInformation("Feature flags from Unleash at {UnleashApi} as {AppName}", settings.UnleashApi, appName);
            return new DefaultUnleash(settings);
        });

        // Created at startup, not on the first sign-in: the first toggle fetch then has time to finish.
        builder.Services.AddHostedService<FeatureFlagWarmup>();

        return builder;
    }

    private sealed class FeatureFlagWarmup(IUnleash unleash) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            GC.KeepAlive(unleash);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
