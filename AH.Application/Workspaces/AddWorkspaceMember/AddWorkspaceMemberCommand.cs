using AH.AppDomain.Workspaces;

namespace AH.Application.Workspaces.AddWorkspaceMember;

public sealed record AddWorkspaceMemberCommand(int WorkspaceId, int RequesterId, string Email, WorkspaceRole Role);
