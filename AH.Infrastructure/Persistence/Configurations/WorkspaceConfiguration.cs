using AH.AppDomain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AH.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(Workspace.NameMaxLength).IsRequired();

        // At most one main workspace.
        builder.HasIndex(w => w.IsMain).IsUnique().HasFilter("\"IsMain\"");

        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(w => w.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        builder.HasKey(m => new { m.WorkspaceId, m.UserId });

        // "Which workspaces does this user belong to" runs on every sign-in.
        builder.HasIndex(m => m.UserId);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
    }
}
