using AH.Application.Abstractions;
using AH.AppDomain.Users;
using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AH.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<UserCertificate> UserCertificates => Set<UserCertificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
