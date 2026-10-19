using System.Globalization;
using System.Security.Claims;
using AH.IdentityService;

namespace AH.WebApi.Auth;

// The session cookie carries the AstraHive identity, so sign-in and profile edits build it the same way.
public static class UserClaims
{
    public static ClaimsPrincipal CreatePrincipal(GetUserReply user, string provider, string? authenticationType)
    {
        var lastName = user.LastName ?? string.Empty;
        var displayName = $"{user.FirstName} {lastName}".Trim();

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, displayName.Length > 0 ? displayName : user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, lastName),
            new(AstraHiveClaimTypes.Provider, provider),
        ];
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            claims.Add(new Claim(AstraHiveClaimTypes.Picture, user.AvatarUrl));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static bool TryGetUserId(this ClaimsPrincipal principal, out int id) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
}
