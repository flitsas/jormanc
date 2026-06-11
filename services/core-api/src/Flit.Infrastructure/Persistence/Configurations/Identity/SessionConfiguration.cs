using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions", "identity");
        builder.HasKey(s => s.Id).HasName("pk_sessions");
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.Jti).HasColumnName("jti").IsRequired();
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(s => s.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at");
        builder.Property(s => s.RevokedBy).HasColumnName("revoked_by");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(s => s.User).WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("fk_sessions_users")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Tenant).WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TenantId)
            .HasConstraintName("fk_sessions_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Jti).IsUnique().HasDatabaseName("uq_sessions_jti");
        builder.HasIndex(s => s.UserId).HasFilter("is_revoked = false AND expires_at > now()")
            .HasDatabaseName("ix_sessions_active_revoked");
        builder.HasIndex(s => new { s.UserId, s.IsRevoked })
            .HasDatabaseName("ix_sessions_user_id_is_revoked");
    }
}
