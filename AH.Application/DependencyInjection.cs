using AH.Application.Users.GetOrCreateUser;
using AH.Application.Users.GetUser;
using AH.Application.Users.UpdateUserProfile;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AH.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<GetOrCreateUserHandler>();
        services.AddScoped<GetUserHandler>();
        services.AddScoped<UpdateUserProfileHandler>();

        return services;
    }
}
