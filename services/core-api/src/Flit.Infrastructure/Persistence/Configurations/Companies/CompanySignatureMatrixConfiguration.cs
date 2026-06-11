using Flit.Infrastructure.Persistence.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanySignatureMatrixConfiguration : IEntityTypeConfiguration<CompanySignatureMatrix>
{
    public void Configure(EntityTypeBuilder<CompanySignatureMatrix> builder)
    {
        builder.ToTable("company_signature_matrix", "companies");
        builder.HasKey(m => m.Id).HasName("pk_company_signature_matrix");
        builder.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(m => m.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(m => m.ActorRole).HasColumnName("actor_role").IsRequired();
        builder.Property(m => m.SignatureType).HasColumnName("signature_type").IsRequired();
        builder.Property(m => m.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasOne(m => m.Company).WithMany(c => c.SignatureMatrix)
            .HasForeignKey(m => m.CompanyId)
            .HasConstraintName("fk_company_signature_matrix_companies")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.CompanyId, m.ActorRole }).IsUnique()
            .HasDatabaseName("uq_signature_matrix_role");
    }
}
