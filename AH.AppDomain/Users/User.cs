namespace AH.AppDomain.Users;

public sealed class User
{
    public const int EmailMaxLength = 320;
    public const int NameMaxLength = 100;
    public const int AvatarUrlMaxLength = 2048;

    private User() { }

    public int Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastLoginAt { get; private set; }

    public static User Create(string email, string firstName, string lastName, string? avatarUrl, DateTimeOffset now)
    {
        var user = new User
        {
            Email = NormalizeEmail(email),
            CreatedAt = now,
        };
        user.SyncFromProvider(firstName, lastName, avatarUrl);
        user.RecordLogin(now);
        return user;
    }

    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

    // Providers only fill gaps: once a name is stored (from an earlier sign-in or edited by the
    // user), a later sign-in must not overwrite it. The avatar has no user-facing editor, so it syncs.
    public void SyncFromProvider(string firstName, string lastName, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(firstName))
        {
            FirstName = firstName.Trim();
        }

        if (string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(lastName))
        {
            LastName = lastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            AvatarUrl = avatarUrl.Trim();
        }
    }

    public void Rename(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);

        var first = firstName.Trim();
        var last = lastName?.Trim() ?? string.Empty;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first.Length, NameMaxLength, nameof(firstName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(last.Length, NameMaxLength, nameof(lastName));

        FirstName = first;
        LastName = last;
    }

    public void RecordLogin(DateTimeOffset now) => LastLoginAt = now;
}
