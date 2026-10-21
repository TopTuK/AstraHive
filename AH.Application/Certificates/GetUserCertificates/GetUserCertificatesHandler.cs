using AH.Application.Abstractions;
using AH.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Certificates.GetUserCertificates;

public sealed class GetUserCertificatesHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<CertificateResponse>>> HandleAsync(GetUserCertificatesQuery query, CancellationToken cancellationToken)
    {
        var loaded = await CertificateAccess.LoadMemberForGlobalAdministratorAsync(
            db, query.WorkspaceId, query.RequesterId, query.UserId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        return Result<IReadOnlyList<CertificateResponse>>.Success(await ListAsync(db, query.UserId, cancellationToken));
    }

    internal static async Task<IReadOnlyList<CertificateResponse>> ListAsync(IAppDbContext db, int userId, CancellationToken cancellationToken)
    {
        var certificates = await db.UserCertificates
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        return [.. certificates.Select(CertificateResponse.From)];
    }
}
