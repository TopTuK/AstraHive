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

        // At most one Global Administrator.
        builder.HasIndex(u => u.IsGlobalAdministrator).IsUnique().HasFilter("\"IsGlobalAdministrator\"");
    }
}

internal sealed class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate>
{
    public void Configure(EntityTypeBuilder<UserCertificate> builder)
    {
        builder.ToTable("user_certificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Thumbprint).HasMaxLength(UserCertificate.ThumbprintLength).IsFixedLength().IsRequired();
        // Certificate sign-in looks the certificate up by thumbprint.
        builder.HasIndex(c => c.Thumbprint).IsUnique();
        builder.HasIndex(c => c.UserId);

        builder.Property(c => c.SerialNumber).HasMaxLength(UserCertificate.SerialNumberMaxLength).IsRequired();
        builder.Property(c => c.ObjectKey).HasMaxLength(UserCertificate.ObjectKeyMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.IssuedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
