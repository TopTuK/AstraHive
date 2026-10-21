using AH.AppDomain.Workspaces;

namespace AH.Application.Workspaces.ChangeWorkspaceMemberRole;

public sealed record ChangeWorkspaceMemberRoleCommand(int WorkspaceId, int RequesterId, int UserId, WorkspaceRole Role);
