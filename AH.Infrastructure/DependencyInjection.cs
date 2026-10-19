using AH.Application.Abstractions;
using AH.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AH.Infrastructure;

public static class DependencyInjection
{
    // Matches the database resource name declared in the AppHost.
    public const string DatabaseName = "astrahive";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>(DatabaseName);
        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return builder;
    }
}
