using AH.Application.Abstractions;
using AH.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Certificates.DownloadCertificate;

// Returns the PFX exactly as issued; it opens with the password shown at issuance.
public sealed class DownloadCertificateHandler(IAppDbContext db, IObjectStorage objectStorage, ILogger<DownloadCertificateHandler> logger)
{
    public async Task<Result<CertificateFileResponse>> HandleAsync(DownloadCertificateQuery query, CancellationToken cancellationToken)
    {
        var loaded = await CertificateAccess.LoadMemberForGlobalAdministratorAsync(
            db, query.WorkspaceId, query.RequesterId, query.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var certificate = await db.UserCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CertificateId && c.UserId == query.UserId, cancellationToken);
        if (certificate?.ObjectKey is null)
        {
            // Unknown, revoked, or the bootstrap certificate (never stored by the product).
            return CertificateAccess.CertificateNotFound(query.CertificateId);
        }

        var content = await objectStorage.GetAsync(certificate.ObjectKey, cancellationToken);
        if (content is null)
        {
            logger.LogError("Certificate {CertificateId} points at missing object {ObjectKey}", certificate.Id, certificate.ObjectKey);
            return CertificateAccess.CertificateNotFound(query.CertificateId);
        }

        logger.LogInformation("User {RequesterId} downloaded certificate {CertificateId} of user {UserId}", query.RequesterId, certificate.Id, query.UserId);

        return new CertificateFileResponse($"{loaded.Value.User.Email}.pfx", content);
    }
}
