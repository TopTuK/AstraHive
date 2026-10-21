using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AH.Infrastructure.Certificates;

// The one definition of the AstraHive root CA and client certificates. BCL only: tools/AH.CertTool compiles
// this file as a link, so the deployment tool and the product issue identical certificates.
internal static class ClientCertificateFactory
{
    public const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    // AES-256 instead of the legacy 3DES default; Windows 10+, macOS and Firefox import it.
    public static readonly PbeParameters PfxEncryption = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);

    public static X509Certificate2 CreateRoot(string subject, DateTimeOffset now, int validYears)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(now.AddDays(-1), now.AddYears(validYears));
    }

    // The email goes into the SAN (CertificateSignIn reads it from there), the display name into the CN.
    // root must carry its private key.
    public static X509Certificate2 CreateClient(X509Certificate2 root, string email, string displayName, DateTimeOffset now, int validYears)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var subject = new X500DistinguishedNameBuilder();
        subject.AddCommonName(displayName);
        subject.AddOrganizationName("AstraHive");
        var request = new CertificateRequest(subject.Build(), key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ClientAuthenticationOid)], false));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(root, true, false));

        var san = new SubjectAlternativeNameBuilder();
        san.AddEmailAddress(email);
        request.CertificateExtensions.Add(san.Build());

        var notAfter = now.AddYears(validYears);
        if (notAfter > root.NotAfter)
        {
            // A client certificate cannot outlive its issuer.
            notAfter = root.NotAfter;
        }

        var serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F; // keep the serial positive
        using var certificate = request.Create(root, now.AddDays(-1), notAfter, serial);
        return certificate.CopyWithPrivateKey(key);
    }

    public static byte[] ExportPfx(X509Certificate2 certificate, string password) =>
        certificate.ExportPkcs12(PfxEncryption, password);

    public static string Thumbprint(X509Certificate2 certificate) => certificate.GetCertHashString(HashAlgorithmName.SHA256);

    // A PEM text, or a path to a PEM file (configuration accepts both).
    public static string ReadPem(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("-----BEGIN", StringComparison.Ordinal) ? trimmed : File.ReadAllText(trimmed);
    }
}
