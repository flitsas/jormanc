using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class ProcedureTypeConfiguration : IEntityTypeConfiguration<ProcedureType>
{
    public void Configure(EntityTypeBuilder<ProcedureType> builder)
    {
        builder.ToTable("procedure_types", "procedures_config");
        builder.HasKey(t => t.Id).HasName("pk_procedure_types");
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Family).HasColumnName("family").IsRequired();
        builder.Property(t => t.Scope).HasColumnName("scope").HasDefaultValue("global").IsRequired();
        builder.Property(t => t.ScopeRefId).HasColumnName("scope_ref_id");
        builder.Property(t => t.VehicleQueryKey).HasColumnName("vehicle_query_key").HasDefaultValue("placa");
        builder.Property(t => t.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(t => new { t.Slug, t.TenantId }).IsUnique()
            .HasDatabaseName("uq_procedure_type_slug_tenant");
        builder.HasIndex(t => new { t.TenantId, t.IsActive }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_procedure_types_tenant_id_active");

        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
