using System.Security.Cryptography.X509Certificates;
using AH.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AH.Infrastructure.Certificates;

// Signs client certificates with the deployment's root CA. Only IdentityService gets the root private key;
// WebApi trusts the root's public certificate for sign-in.
internal sealed class CertificateAuthority(IOptions<CertificateAuthorityOptions> options, TimeProvider timeProvider)
    : ICertificateAuthority, IDisposable
{
    private readonly Lazy<X509Certificate2> _root = new(() => LoadRoot(options.Value));

    public bool IsAvailable => options.Value.IsConfigured;

    public IssuedCertificate Issue(string email, string displayName, string pfxPassword)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException($"{CertificateAuthorityOptions.SectionName} is not configured.");
        }

        using var client = ClientCertificateFactory.CreateClient(
            _root.Value, email, displayName, timeProvider.GetUtcNow(), options.Value.ClientCertificateYears);

        return new IssuedCertificate(
            ClientCertificateFactory.ExportPfx(client, pfxPassword),
            new CertificateDetails(
                ClientCertificateFactory.Thumbprint(client),
                client.SerialNumber,
                new DateTimeOffset(client.NotBefore.ToUniversalTime(), TimeSpan.Zero),
                new DateTimeOffset(client.NotAfter.ToUniversalTime(), TimeSpan.Zero)));
    }

    public void Dispose()
    {
        if (_root.IsValueCreated)
        {
            _root.Value.Dispose();
        }
    }

    private static X509Certificate2 LoadRoot(CertificateAuthorityOptions options) =>
        X509Certificate2.CreateFromPem(
            ClientCertificateFactory.ReadPem(options.Certificate!),
            ClientCertificateFactory.ReadPem(options.PrivateKey!));
}

// Bound from "CertificateAuthority". Certificate and PrivateKey: PEM text or a path to a PEM file, as written
// by AH.CertTool (root.pem, root.key.pem). Without both, issuing certificates is unavailable.
public sealed class CertificateAuthorityOptions
{
    public const string SectionName = "CertificateAuthority";

    public string? Certificate { get; set; }
    public string? PrivateKey { get; set; }
    public int ClientCertificateYears { get; set; } = 2;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Certificate) && !string.IsNullOrWhiteSpace(PrivateKey);
}
