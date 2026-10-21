namespace AH.Application.Abstractions;

// Issues client certificates from the configured root CA (the root private key stays in the implementation).
public interface ICertificateAuthority
{
    // False when no root certificate and key are configured; issuing then fails.
    bool IsAvailable { get; }

    IssuedCertificate Issue(string email, string displayName, string pfxPassword);
}

public sealed record CertificateDetails(string Thumbprint, string SerialNumber, DateTimeOffset NotBefore, DateTimeOffset NotAfter);

// Pfx holds the certificate with its private key, encrypted with the password passed to Issue.
public sealed record IssuedCertificate(byte[] Pfx, CertificateDetails Details);
