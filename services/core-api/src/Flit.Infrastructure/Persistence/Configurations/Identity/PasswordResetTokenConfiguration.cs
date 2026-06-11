using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens", "identity");
        builder.HasKey(t => t.Id).HasName("pk_password_reset_tokens");
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(t => t.UsedAt).HasColumnName("used_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("fk_password_reset_tokens_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("uq_password_reset_tokens_hash");
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
            .HasFilter("used_at IS NULL")
            .HasDatabaseName("ix_password_reset_tokens_user_id_active");
    }
}
