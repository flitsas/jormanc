using Flit.Infrastructure.Persistence.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", "companies");
        builder.HasKey(c => c.Id).HasName("pk_companies");
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Nit).HasColumnName("nit").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasDefaultValue("active").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        // tenant_id 1:1 con company (una empresa por tenant)
        builder.HasIndex(c => c.TenantId).IsUnique().HasDatabaseName("uq_companies_tenant_id");
        // A11: tenant_id como primera columna en índices de tabla con tenant_id
        builder.HasIndex(c => new { c.TenantId, c.Nit }).HasDatabaseName("ix_companies_tenant_nit");
        builder.HasIndex(c => new { c.TenantId, c.Name }).HasDatabaseName("ix_companies_tenant_name");
        builder.HasIndex(c => new { c.TenantId, c.Status }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_companies_tenant_status");

        builder.HasQueryFilter(c => c.DeletedAt == null);

        // Navegación 1:1 con identity.tenants (sin cascade — tenant vive independiente)
        builder.HasOne(c => c.Tenant).WithMany()
            .HasForeignKey(c => c.TenantId)
            .HasConstraintName("fk_companies_tenants")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
