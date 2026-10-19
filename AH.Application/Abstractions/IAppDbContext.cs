using AH.AppDomain.Users;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
