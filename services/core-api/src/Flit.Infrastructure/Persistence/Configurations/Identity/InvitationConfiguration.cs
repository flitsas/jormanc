using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations", "identity");
        builder.HasKey(i => i.Id).HasName("pk_invitations");
        builder.Property(i => i.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(i => i.Email).HasColumnName("email").IsRequired();
        builder.Property(i => i.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(i => i.RolesJson).HasColumnName("roles_json")
            .HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(i => i.Status).HasColumnName("status").HasDefaultValue("pending").IsRequired();
        builder.Property(i => i.InvitedBy).HasColumnName("invited_by");
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(i => i.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(i => i.Tenant).WithMany(t => t.Invitations)
            .HasForeignKey(i => i.TenantId)
            .HasConstraintName("fk_invitations_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash).IsUnique().HasDatabaseName("uq_invitations_token_hash");
        builder.HasIndex(i => new { i.TenantId, i.Status })
            .HasDatabaseName("ix_invitations_tenant_id_status");
    }
}
