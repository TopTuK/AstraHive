using AH.AppDomain.Users;

namespace AH.AppDomain.Workspaces;

public sealed class WorkspaceMember
{
    private WorkspaceMember() { }

    public int WorkspaceId { get; private set; }
    public int UserId { get; private set; }
    // Set when the member is added, so a user that is not saved yet gets its id through the navigation.
    public User User { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    internal static WorkspaceMember Create(User user, WorkspaceRole role, DateTimeOffset now) => new()
    {
        User = user,
        UserId = user.Id,
        Role = role,
        AddedAt = now,
    };

    internal void ChangeRole(WorkspaceRole role) => Role = role;
}
