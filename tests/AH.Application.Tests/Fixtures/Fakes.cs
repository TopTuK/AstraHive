using AH.Application.Abstractions;
using AH.Application.Common;

namespace AH.Application.Tests.Fixtures;

// Self-service defaults (external sign-in and registration on), so tests that are not about the flags see the
// behaviour users get on an open deployment. Flag tests switch them off explicitly.
public sealed class FakeFeatureFlags : IFeatureFlags
{
    private readonly Dictionary<string, bool> _flags = new(StringComparer.Ordinal)
    {
        [FeatureFlags.ExternalLogin] = true,
        [FeatureFlags.RegistrationEnabled] = true,
    };

    public bool IsEnabled(string name) => _flags.GetValueOrDefault(name);

    public FakeFeatureFlags Set(string name, bool enabled)
    {
        _flags[name] = enabled;
        return this;
    }
}

public sealed class FakeObjectStorage : IObjectStorage
{
    public Dictionary<string, (byte[] Content, string ContentType)> Objects { get; } = new(StringComparer.Ordinal);

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Objects[key] = (buffer.ToArray(), contentType);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Objects.TryGetValue(key, out var stored) ? stored.Content : null);

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        Objects.Remove(key);
        return Task.CompletedTask;
    }
}
