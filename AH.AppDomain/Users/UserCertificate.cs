namespace AH.AppDomain.Users;

// A client certificate issued from the configured root. Certificate sign-in accepts only a registered,
// unrevoked certificate, so revoking works without a CRL. The PFX itself lives in object storage
// (ObjectKey); the Global Administrator's bootstrap certificate is registered without one.
public sealed class UserCertificate
{
    public const int ThumbprintLength = 64;
    public const int SerialNumberMaxLength = 64;
    public const int ObjectKeyMaxLength = 512;

    private UserCertificate() { }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    // SHA-256 of the DER certificate, upper-case hex (X509Certificate2.GetCertHashString(SHA256)).
    public string Thumbprint { get; private set; } = null!;
    public string SerialNumber { get; private set; } = null!;
    public DateTimeOffset NotBefore { get; private set; }
    public DateTimeOffset NotAfter { get; private set; }
    public string? ObjectKey { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    // Null for the bootstrap certificate, which the certificate tool created outside the product.
    public int? IssuedById { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static UserCertificate Register(
        int userId,
        string thumbprint,
        string serialNumber,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        string? objectKey,
        int? issuedById,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        if (notAfter <= notBefore)
        {
            throw new ArgumentException("A certificate must expire after it becomes valid.", nameof(notAfter));
        }

        var normalized = NormalizeThumbprint(thumbprint);
        ArgumentOutOfRangeException.ThrowIfNotEqual(normalized.Length, ThumbprintLength, nameof(thumbprint));

        return new UserCertificate
        {
            UserId = userId,
            Thumbprint = normalized,
            SerialNumber = serialNumber.Trim(),
            NotBefore = notBefore,
            NotAfter = notAfter,
            ObjectKey = objectKey,
            IssuedAt = now,
            IssuedById = issuedById,
        };
    }

    public static string NormalizeThumbprint(string thumbprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        return thumbprint.Trim().Replace(":", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    public bool IsActive(DateTimeOffset now) => !IsRevoked && NotBefore <= now && now < NotAfter;

    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException($"Certificate {Id} is already revoked.");
        }

        RevokedAt = now;
        // A revoked certificate is never downloaded again; the stored PFX is deleted with it.
        ObjectKey = null;
    }
}
