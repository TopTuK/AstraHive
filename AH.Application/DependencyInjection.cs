using AH.Application.Administration.EnsureGlobalAdministrator;
using AH.Application.Certificates.DownloadCertificate;
using AH.Application.Certificates.GetUserCertificates;
using AH.Application.Certificates.IssueCertificate;
using AH.Application.Certificates.RevokeCertificate;
using AH.Application.Users.GetOrCreateUser;
using AH.Application.Users.GetUser;
using AH.Application.Users.UpdateUserProfile;
using AH.Application.Workspaces.AddWorkspaceMember;
using AH.Application.Workspaces.ChangeWorkspaceMemberRole;
using AH.Application.Workspaces.CreateWorkspace;
using AH.Application.Workspaces.GetMyWorkspaces;
using AH.Application.Workspaces.GetWorkspaceMembers;
using AH.Application.Workspaces.RemoveWorkspaceMember;
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

        services.AddScoped<GetMyWorkspacesHandler>();
        services.AddScoped<GetWorkspaceMembersHandler>();
        services.AddScoped<AddWorkspaceMemberHandler>();
        services.AddScoped<ChangeWorkspaceMemberRoleHandler>();
        services.AddScoped<RemoveWorkspaceMemberHandler>();
        services.AddScoped<CreateWorkspaceHandler>();

        services.AddScoped<EnsureGlobalAdministratorHandler>();

        services.AddScoped<IssueCertificateHandler>();
        services.AddScoped<GetUserCertificatesHandler>();
        services.AddScoped<DownloadCertificateHandler>();
        services.AddScoped<RevokeCertificateHandler>();

        return services;
    }
}
