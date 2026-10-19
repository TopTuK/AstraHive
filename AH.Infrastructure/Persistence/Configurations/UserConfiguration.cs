using AH.AppDomain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AH.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FirstName).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(User.AvatarUrlMaxLength);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(10);
    }
}
