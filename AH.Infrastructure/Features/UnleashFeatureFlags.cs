using AH.Application.Abstractions;
using Unleash;

namespace AH.Infrastructure.Features;

// The IUnleash singleton comes from AddFeatureFlagClient (ServiceDefaults), registered by the host.
internal sealed class UnleashFeatureFlags(IUnleash unleash) : IFeatureFlags
{
    public bool IsEnabled(string name) => unleash.IsEnabled(name, defaultSetting: false);
}
