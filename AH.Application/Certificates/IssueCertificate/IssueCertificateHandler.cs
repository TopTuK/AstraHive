using System.Security.Cryptography;
using AH.Application.Abstractions;
using AH.Application.Common;
using AH.AppDomain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AH.Application.Certificates.IssueCertificate;

// Issues a client certificate for a main-workspace member and keeps the password-protected PFX in object
// storage, so the Global Administrator can download it again to hand it over. The password is returned once.
public sealed class IssueCertificateHandler(
    IAppDbContext db,
    ICertificateAuthority certificateAuthority,
    IObjectStorage objectStorage,
    TimeProvider timeProvider,
    ILogger<IssueCertificateHandler> logger)
{
    public const string PfxContentType = "application/x-pkcs12";

    // No look-alikes (0/O, 1/l/I): the password is read off the screen and typed into an import dialog.
    private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int PasswordLength = 20;

    public async Task<Result<IssuedCertificateResponse>> HandleAsync(IssueCertificateCommand command, CancellationToken cancellationToken)
    {
        var loaded = await CertificateAccess.LoadMemberForGlobalAdministratorAsync(
            db, command.WorkspaceId, command.RequesterId, command.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        if (!certificateAuthority.IsAvailable)
        {
            return Error.Conflict("Certificates.AuthorityUnavailable", "No root certificate with a private key is configured.");
        }

        var user = loaded.Value.User;
        var password = RandomNumberGenerator.GetString(PasswordAlphabet, PasswordLength);
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        var issued = certificateAuthority.Issue(user.Email, displayName.Length > 0 ? displayName : user.Email, password);
        var now = timeProvider.GetUtcNow();

        var objectKey = $"certificates/{user.Id}/{issued.Details.Thumbprint}.pfx";
        using (var content = new MemoryStream(issued.Pfx, writable: false))
        {
            await objectStorage.PutAsync(objectKey, content, PfxContentType, cancellationToken);
        }

        var certificate = UserCertificate.Register(
            user.Id,
            issued.Details.Thumbprint,
            issued.Details.SerialNumber,
            issued.Details.NotBefore,
            issued.Details.NotAfter,
            objectKey,
            command.RequesterId,
            now);
        db.UserCertificates.Add(certificate);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Never leave an object that no row points at; the failure itself is unexpected (a fresh key pair).
            await objectStorage.DeleteAsync(objectKey, CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "User {RequesterId} issued certificate {CertificateId} ({Thumbprint}) for user {UserId}, valid until {NotAfter}",
            command.RequesterId,
            certificate.Id,
            certificate.Thumbprint,
            user.Id,
            certificate.NotAfter);

        return new IssuedCertificateResponse(CertificateResponse.From(certificate), password);
    }
}
