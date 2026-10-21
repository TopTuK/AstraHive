namespace AH.WebApi.Auth;

// Sign-in sources do not always split the name (certificate CN, some OAuth profiles), so the display name
// is split on the first space.
public static class PersonName
{
    public static (string FirstName, string LastName) Split(string? displayName, string fallbackLastName = "")
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return (string.Empty, fallbackLastName);
        }

        var parts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : fallbackLastName);
    }
}
