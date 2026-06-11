using Flit.Infrastructure.Persistence.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Integrations;

public sealed class IdentityValidationConfiguration : IEntityTypeConfiguration<IdentityValidation>
{
    public void Configure(EntityTypeBuilder<IdentityValidation> builder)
    {
        builder.ToTable("identity_validations", "integrations");
        builder.HasKey(v => v.Id).HasName("pk_identity_validations");
        builder.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(v => v.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(v => v.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(v => v.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(v => v.Provider).HasColumnName("provider").IsRequired();
        // PII: datos biométricos
        builder.Property(v => v.Verdict).HasColumnName("verdict").HasDefaultValue("pending").IsRequired();
        builder.Property(v => v.LivenessRef).HasColumnName("liveness_ref");
        builder.Property(v => v.DocumentPhotoRef).HasColumnName("document_photo_ref");
        builder.Property(v => v.ValidatedAt).HasColumnName("validated_at");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(v => new { v.TenantId, v.ProcedureId, v.ActorId })
            .HasDatabaseName("ix_identity_validations_tenant_procedure_actor");
        // A11: tenant_id como primera columna
        builder.HasIndex(v => new { v.TenantId, v.CreatedAt }).HasDatabaseName("ix_identity_validations_tenant_created_at");
    }
}
