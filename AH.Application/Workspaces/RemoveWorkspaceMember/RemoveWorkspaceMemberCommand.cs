namespace AH.Application.Workspaces.RemoveWorkspaceMember;

public sealed record RemoveWorkspaceMemberCommand(int WorkspaceId, int RequesterId, int UserId);
