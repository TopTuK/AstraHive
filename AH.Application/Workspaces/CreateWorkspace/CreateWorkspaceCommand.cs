namespace AH.Application.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceCommand(int RequesterId, string Name);
