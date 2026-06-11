using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(u => u.Id).HasName("pk_users");
        builder.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(u => u.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired();
        builder.Property(u => u.FullName).HasColumnName("full_name").IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.Status).HasColumnName("status").HasDefaultValue("active").IsRequired();
        builder.Property(u => u.MustResetPwd).HasColumnName("must_reset_pwd").HasDefaultValue(false);
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(u => u.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedBy).HasColumnName("deleted_by");
        builder.Property(u => u.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(u => u.Tenant).WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .HasConstraintName("fk_users_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.Email, u.TenantId }).IsUnique()
            .HasDatabaseName("uq_users_email_tenant");
        builder.HasIndex(u => u.TenantId).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_users_tenant_id");

        // El filtro global de tenant se aplica en FlitDbContext según el ITenantContext.
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
