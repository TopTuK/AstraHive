using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AH.Application.Abstractions;
using AH.Application.Administration.EnsureGlobalAdministrator;

namespace AH.IdentityService;

// Creates the Global Administrator, the main workspace and the registration of the administrator's
// certificate on startup, from the "GlobalAdmin" section: Email, Name, Certificate (the public PEM text
// or path written by AH.CertTool). The AppHost sets it from the dev certificates or deployment parameters.
public static class GlobalAdministratorBootstrap
{
    public const string SectionName = "GlobalAdmin";

    public static async Task EnsureGlobalAdministratorAsync(this WebApplication app, CancellationToken cancellationToken)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(GlobalAdministratorBootstrap).FullName!);
        var section = app.Configuration.GetSection(SectionName);
        var email = section["Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("{Section}:Email is not set: no Global Administrator is ensured", SectionName);
            return;
        }

        var certificate = ReadCertificate(section["Certificate"]);
        if (certificate is null)
        {
            logger.LogWarning("{Section}:Certificate is not set: the Global Administrator has no registered certificate to sign in with", SectionName);
        }

        var name = section["Name"]?.Trim() ?? string.Empty;
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = new EnsureGlobalAdministratorCommand(
            email,
            parts.Length > 0 ? parts[0] : string.Empty,
            parts.Length > 1 ? parts[1] : string.Empty,
            certificate);

        await using var scope = app.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<EnsureGlobalAdministratorHandler>();
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Global Administrator {Email} is user {UserId}, main workspace {WorkspaceId}",
                result.Value.Email,
                result.Value.UserId,
                result.Value.MainWorkspaceId);
        }
        else
        {
            logger.LogWarning("Global Administrator not ensured: {ErrorCode} {ErrorMessage}", result.Error!.Code, result.Error.Message);
        }
    }

    private static CertificateDetails? ReadCertificate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var pem = trimmed.StartsWith("-----BEGIN", StringComparison.Ordinal) ? trimmed : File.ReadAllText(trimmed);
        using var certificate = X509Certificate2.CreateFromPem(pem);
        return new CertificateDetails(
            certificate.GetCertHashString(HashAlgorithmName.SHA256),
            certificate.SerialNumber,
            new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
            new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero));
    }
}
