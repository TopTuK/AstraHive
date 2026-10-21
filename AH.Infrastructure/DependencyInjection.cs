using AH.Application.Abstractions;
using AH.Infrastructure.Certificates;
using AH.Infrastructure.Features;
using AH.Infrastructure.Persistence;
using AH.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AH.Infrastructure;

public static class DependencyInjection
{
    // Match the resource names declared in the AppHost.
    public const string DatabaseName = "astrahive";
    public const string ObjectStorageName = "minio";

    // IFeatureFlags needs the IUnleash client the host registers with AddFeatureFlagClient (ServiceDefaults).
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>(DatabaseName);
        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IFeatureFlags, UnleashFeatureFlags>();

        builder.Services.Configure<CertificateAuthorityOptions>(builder.Configuration.GetSection(CertificateAuthorityOptions.SectionName));
        builder.Services.AddSingleton<ICertificateAuthority, CertificateAuthority>();

        builder.AddMinioClient(connectionName: ObjectStorageName);
        builder.Services.Configure<ObjectStorageOptions>(builder.Configuration.GetSection(ObjectStorageOptions.SectionName));
        builder.Services.AddSingleton<IObjectStorage, MinioObjectStorage>();

        return builder;
    }
}
