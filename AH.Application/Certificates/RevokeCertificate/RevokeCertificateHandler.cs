using AH.Application.Abstractions;
using AH.Application.Certificates.GetUserCertificates;
using AH.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Certificates.RevokeCertificate;

// A revoked certificate no longer signs in (GetOrCreateUser checks the registry) and its PFX is deleted.
// The Global Administrator's last active certificate cannot be revoked: that would lock them out.
public sealed class RevokeCertificateHandler(
    IAppDbContext db,
    IObjectStorage objectStorage,
    TimeProvider timeProvider,
    ILogger<RevokeCertificateHandler> logger)
{
    public async Task<Result<IReadOnlyList<CertificateResponse>>> HandleAsync(RevokeCertificateCommand command, CancellationToken cancellationToken)
    {
        var loaded = await CertificateAccess.LoadMemberForGlobalAdministratorAsync(
            db, command.WorkspaceId, command.RequesterId, command.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var certificate = await db.UserCertificates
            .FirstOrDefaultAsync(c => c.Id == command.CertificateId && c.UserId == command.UserId, cancellationToken);
        if (certificate is null || certificate.IsRevoked)
        {
            return CertificateAccess.CertificateNotFound(command.CertificateId);
        }

        var now = timeProvider.GetUtcNow();
        if (loaded.Value.User.IsGlobalAdministrator)
        {
            var othersActive = await db.UserCertificates.AnyAsync(
                c => c.UserId == command.UserId && c.Id != certificate.Id && c.RevokedAt == null && c.NotBefore <= now && c.NotAfter > now,
                cancellationToken);
            if (!othersActive)
            {
                return Error.Conflict(
                    "Certificates.LastGlobalAdministratorCertificate",
                    "This is the last active certificate of the Global Administrator; issue another one first.");
            }
        }

        var objectKey = certificate.ObjectKey;
        certificate.Revoke(now);
        await db.SaveChangesAsync(cancellationToken);

        // After the save: an orphaned object is harmless, a revoked but still downloadable certificate is not.
        if (objectKey is not null)
        {
            await objectStorage.DeleteAsync(objectKey, cancellationToken);
        }

        logger.LogInformation("User {RequesterId} revoked certificate {CertificateId} of user {UserId}", command.RequesterId, certificate.Id, command.UserId);

        return Result<IReadOnlyList<CertificateResponse>>.Success(await GetUserCertificatesHandler.ListAsync(db, command.UserId, cancellationToken));
    }
}
