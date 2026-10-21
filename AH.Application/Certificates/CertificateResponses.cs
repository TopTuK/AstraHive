using AH.AppDomain.Users;

namespace AH.Application.Certificates;

public sealed record CertificateResponse(
    int Id,
    int UserId,
    string Thumbprint,
    string SerialNumber,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    DateTimeOffset IssuedAt,
    DateTimeOffset? RevokedAt,
    bool CanDownload)
{
    internal static CertificateResponse From(UserCertificate certificate) => new(
        certificate.Id,
        certificate.UserId,
        certificate.Thumbprint,
        certificate.SerialNumber,
        certificate.NotBefore,
        certificate.NotAfter,
        certificate.IssuedAt,
        certificate.RevokedAt,
        certificate.ObjectKey is not null);
}

// The only response that carries the PFX password: it is not stored anywhere.
public sealed record IssuedCertificateResponse(CertificateResponse Certificate, string Password);

public sealed record CertificateFileResponse(string FileName, byte[] Content);
