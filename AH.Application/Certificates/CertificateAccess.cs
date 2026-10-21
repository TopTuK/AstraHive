using AH.Application.Abstractions;
using AH.Application.Common;
using AH.Application.Workspaces;
using AH.AppDomain.Workspaces;

namespace AH.Application.Certificates;

// Certificates are managed by the Global Administrator only, for members of the main workspace. Non-members
// of the workspace get NotFound (as in WorkspaceAccess); other members get Forbidden.
internal static class CertificateAccess
{
    public static async Task<Result<WorkspaceMember>> LoadMemberForGlobalAdministratorAsync(
        IAppDbContext db,
        int workspaceId,
        int requesterId,
        int userId,
        CancellationToken cancellationToken)
    {
        var loaded = await WorkspaceAccess.LoadForMemberAsync(db, workspaceId, requesterId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var workspace = loaded.Value;
        if (!workspace.IsProtectedMember(requesterId))
        {
            return Error.Forbidden("Certificates.GlobalAdministratorRequired", "Only the Global Administrator manages client certificates.");
        }

        return workspace.FindMember(userId) is { } member
            ? member
            : WorkspaceAccess.MemberNotFound(userId);
    }

    public static Error CertificateNotFound(int certificateId) =>
        Error.NotFound("Certificates.NotFound", $"Certificate {certificateId} was not found.");
}
