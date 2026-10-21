using AH.AppDomain.Users;

namespace AH.AppDomain.Workspaces;

// A tenant: owns clusters and is shared by its members. It always keeps at least one Administrator,
// so nobody can lock the workspace out of its own management. The main workspace is created once at
// deployment for the Global Administrator: it holds the operators, never clusters.
public sealed class Workspace
{
    public const int NameMaxLength = 100;
    public const string MainName = "AstraHive";

    private readonly List<WorkspaceMember> _members = [];

    private Workspace() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsMain { get; private set; }
    public IReadOnlyCollection<WorkspaceMember> Members => _members;

    // Checked by cluster use cases once clusters exist.
    public bool CanOwnClusters => !IsMain;

    public static Workspace Create(string name, User owner, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(owner);

        var trimmed = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, NameMaxLength, nameof(name));

        var workspace = new Workspace { Name = trimmed, CreatedAt = now };
        workspace._members.Add(WorkspaceMember.Create(owner, WorkspaceRole.Administrator, now));
        return workspace;
    }

    public static Workspace CreateMain(User globalAdministrator, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(globalAdministrator);
        if (!globalAdministrator.IsGlobalAdministrator)
        {
            throw new InvalidOperationException($"User {globalAdministrator.Id} is not the Global Administrator.");
        }

        var workspace = Create(MainName, globalAdministrator, now);
        workspace.IsMain = true;
        return workspace;
    }

    // Workspace names are not localized: the server does not know the user's language at first sign-in.
    public static string DefaultName(string firstName, string email)
    {
        var owner = string.IsNullOrWhiteSpace(firstName) ? NormalizedLocalPart(email) : firstName.Trim();
        var name = $"{owner}'s workspace";
        return name.Length <= NameMaxLength ? name : name[..NameMaxLength];
    }

    public WorkspaceMember? FindMember(int userId) => _members.Find(m => m.UserId == userId);

    public bool IsAdministrator(int userId) => FindMember(userId)?.Role == WorkspaceRole.Administrator;

    public bool IsLastAdministrator(int userId) =>
        IsAdministrator(userId) && _members.Count(m => m.Role == WorkspaceRole.Administrator) == 1;

    // The Global Administrator stays an Administrator of the main workspace: nobody (including
    // themselves) can demote or remove them. Expects members loaded with their users.
    public bool IsProtectedMember(int userId) => IsMain && FindMember(userId)?.User is { IsGlobalAdministrator: true };

    public WorkspaceMember AddMember(User user, WorkspaceRole role, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (_members.Exists(m => ReferenceEquals(m.User, user) || (user.Id != 0 && m.UserId == user.Id)))
        {
            throw new InvalidOperationException($"User {user.Id} is already a member of workspace {Id}.");
        }

        var member = WorkspaceMember.Create(user, role, now);
        _members.Add(member);
        return member;
    }

    public void ChangeRole(int userId, WorkspaceRole role)
    {
        var member = FindMember(userId) ?? throw new InvalidOperationException($"User {userId} is not a member of workspace {Id}.");
        if (role != WorkspaceRole.Administrator && IsProtectedMember(userId))
        {
            throw new InvalidOperationException($"User {userId} is the Global Administrator of workspace {Id}.");
        }

        if (role != WorkspaceRole.Administrator && IsLastAdministrator(userId))
        {
            throw new InvalidOperationException($"User {userId} is the last administrator of workspace {Id}.");
        }

        member.ChangeRole(role);
    }

    public void RemoveMember(int userId)
    {
        var member = FindMember(userId) ?? throw new InvalidOperationException($"User {userId} is not a member of workspace {Id}.");
        if (IsProtectedMember(userId))
        {
            throw new InvalidOperationException($"User {userId} is the Global Administrator of workspace {Id}.");
        }

        if (IsLastAdministrator(userId))
        {
            throw new InvalidOperationException($"User {userId} is the last administrator of workspace {Id}.");
        }

        _members.Remove(member);
    }

    private static string NormalizedLocalPart(string email)
    {
        var normalized = User.NormalizeEmail(email);
        var at = normalized.IndexOf('@', StringComparison.Ordinal);
        return at > 0 ? normalized[..at] : normalized;
    }
}
