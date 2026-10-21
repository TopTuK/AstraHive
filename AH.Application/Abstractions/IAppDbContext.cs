using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<UserCertificate> UserCertificates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
