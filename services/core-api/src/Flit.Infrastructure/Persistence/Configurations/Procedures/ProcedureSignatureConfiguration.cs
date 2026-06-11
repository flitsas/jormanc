using Flit.Infrastructure.Persistence.Entities.Procedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Procedures;

public sealed class ProcedureSignatureConfiguration : IEntityTypeConfiguration<ProcedureSignature>
{
    public void Configure(EntityTypeBuilder<ProcedureSignature> builder)
    {
        builder.ToTable("procedure_signatures", "procedures");
        builder.HasKey(s => s.Id).HasName("pk_procedure_signatures");
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(s => s.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.SignatureType).HasColumnName("signature_type").IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasDefaultValue("pending").IsRequired();
        builder.Property(s => s.FileRef).HasColumnName("file_ref");
        builder.Property(s => s.SignedAt).HasColumnName("signed_at");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(s => s.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(s => s.Procedure).WithMany(p => p.Signatures)
            .HasForeignKey(s => s.ProcedureId)
            .HasConstraintName("fk_procedure_signatures_procedures")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Actor).WithMany()
            .HasForeignKey(s => s.ActorId)
            .HasConstraintName("fk_procedure_signatures_actors")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TenantId, s.ProcedureId, s.Status })
            .HasDatabaseName("ix_procedure_signatures_tenant_procedure_status");
    }
}
