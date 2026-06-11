using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "identity");
        builder.HasKey(p => p.Id).HasName("pk_permissions");
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(p => p.Slug).HasColumnName("slug").IsRequired();
        builder.Property(p => p.Module).HasColumnName("module").IsRequired();
        builder.Property(p => p.Action).HasColumnName("action").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");

        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("uq_permissions_slug");
    }
}
