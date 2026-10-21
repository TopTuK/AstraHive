using AH.AppDomain.Workspaces;

namespace AH.Application.Workspaces;

public sealed record WorkspaceSummaryResponse(int Id, string Name, WorkspaceRole Role, int MemberCount, bool IsMain);

// YourRole is null only after the requester removed themselves. CanManageCertificates: the requester is the
// Global Administrator of this (main) workspace.
public sealed record WorkspaceMembersResponse(
    int Id,
    string Name,
    bool IsMain,
    WorkspaceRole? YourRole,
    bool CanManageCertificates,
    IReadOnlyList<WorkspaceMemberResponse> Members)
{
    // Expects the workspace loaded through WorkspaceAccess (members with their users).
    internal static WorkspaceMembersResponse From(Workspace workspace, int requesterId) => new(
        workspace.Id,
        workspace.Name,
        workspace.IsMain,
        workspace.FindMember(requesterId)?.Role,
        workspace.IsProtectedMember(requesterId),
        [.. workspace.Members
            .OrderBy(m => m.Role)
            .ThenBy(m => m.User.Email, StringComparer.Ordinal)
            .Select(m => new WorkspaceMemberResponse(
                m.UserId,
                m.User.Email,
                m.User.FirstName,
                m.User.LastName,
                m.User.AvatarUrl,
                m.Role,
                m.User.IsPending,
                workspace.IsProtectedMember(m.UserId),
                m.AddedAt))]);
}

// IsGlobalAdministrator: the member's role is fixed (cannot be changed or removed).
public sealed record WorkspaceMemberResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    WorkspaceRole Role,
    bool IsPending,
    bool IsGlobalAdministrator,
    DateTimeOffset AddedAt);
