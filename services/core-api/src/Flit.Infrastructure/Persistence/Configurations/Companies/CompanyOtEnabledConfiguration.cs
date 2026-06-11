using Flit.Infrastructure.Persistence.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyOtEnabledConfiguration : IEntityTypeConfiguration<CompanyOtEnabled>
{
    public void Configure(EntityTypeBuilder<CompanyOtEnabled> builder)
    {
        builder.ToTable("company_ot_enabled", "companies");
        builder.HasKey(e => e.Id).HasName("pk_company_ot_enabled");
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.OtSlug).HasColumnName("ot_slug").IsRequired();
        builder.Property(e => e.ProcedureFamily).HasColumnName("procedure_family").IsRequired();
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);

        builder.HasOne(e => e.Company).WithMany(c => c.OtEnabled)
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("fk_company_ot_enabled_companies")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CompanyId, e.OtSlug, e.ProcedureFamily }).IsUnique()
            .HasDatabaseName("uq_company_ot_family");
    }
}
