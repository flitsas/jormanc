using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class ApiConnectorConfiguration : IEntityTypeConfiguration<ApiConnector>
{
    public void Configure(EntityTypeBuilder<ApiConnector> builder)
    {
        builder.ToTable("api_connectors", "procedures_config");
        builder.HasKey(c => c.Id).HasName("pk_api_connectors");
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(c => c.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Endpoint).HasColumnName("endpoint").IsRequired();
        builder.Property(c => c.HttpVerb).HasColumnName("http_verb").HasDefaultValue("GET").IsRequired();
        builder.Property(c => c.StepOrder).HasColumnName("step_order").IsRequired();
        builder.Property(c => c.ParamBindings).HasColumnName("param_bindings")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.ResponseMappings).HasColumnName("response_mappings")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(c => c.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(c => c.ProcedureType).WithMany(t => t.ApiConnectors)
            .HasForeignKey(c => c.ProcedureTypeId)
            .HasConstraintName("fk_api_connectors_procedure_types")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.ProcedureTypeId })
            .HasDatabaseName("ix_api_connectors_tenant_type");
    }
}
