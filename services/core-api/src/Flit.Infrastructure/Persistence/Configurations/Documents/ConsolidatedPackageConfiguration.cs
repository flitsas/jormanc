using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class ConsolidatedPackageConfiguration : IEntityTypeConfiguration<ConsolidatedPackage>
{
    public void Configure(EntityTypeBuilder<ConsolidatedPackage> builder)
    {
        builder.ToTable("consolidated_packages", "documents");
        builder.HasKey(p => p.Id).HasName("pk_consolidated_packages");
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(p => p.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(p => p.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(p => p.Version).HasColumnName("version").IsRequired();
        builder.Property(p => p.MergedFileRef).HasColumnName("merged_file_ref").IsRequired();
        builder.Property(p => p.DownloadFilename).HasColumnName("download_filename").IsRequired();
        builder.Property(p => p.DocCount).HasColumnName("doc_count").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();

        builder.HasIndex(p => new { p.ProcedureId, p.Version }).IsUnique()
            .HasDatabaseName("uq_consolidated_package_proc_version");
        builder.HasIndex(p => new { p.TenantId, p.ProcedureId })
            .HasDatabaseName("ix_consolidated_packages_tenant_procedure");
    }
}
