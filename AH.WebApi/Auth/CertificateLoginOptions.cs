using System.Security.Cryptography.X509Certificates;

namespace AH.WebApi.Auth;

// Bound from "Authentication:Certificate". Under `aspire run` the AppHost sets all three (the root is the
// per-machine dev CA); elsewhere configure them directly. Without a root or a port the feature is off.
public sealed class CertificateLoginOptions
{
    public const string SectionName = "Authentication:Certificate";

    // The CA that issues client certificates: a PEM file path, or the PEM text itself. Public part only.
    public string? RootCertificate { get; set; }

    // The only TLS port that asks the browser for a client certificate.
    public int? LoginPort { get; set; }

    // Where the browser returns after sign-in (the SPA origin, e.g. https://localhost:7093).
    public string? AppOrigin { get; set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(RootCertificate) && LoginPort is > 0;

    public static CertificateLoginOptions From(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<CertificateLoginOptions>() ?? new CertificateLoginOptions();

    public X509Certificate2 LoadRootCertificate()
    {
        var value = RootCertificate?.Trim() ?? throw new InvalidOperationException($"{SectionName}:RootCertificate is not set.");
        // CreateFromPem(cert) loads the public certificate only; CreateFromPemFile would also demand a key.
        var pem = value.StartsWith("-----BEGIN", StringComparison.Ordinal) ? value : File.ReadAllText(value);
        return X509Certificate2.CreateFromPem(pem);
    }
}
