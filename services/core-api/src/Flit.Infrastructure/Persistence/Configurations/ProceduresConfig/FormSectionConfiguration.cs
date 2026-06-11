using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class FormSectionConfiguration : IEntityTypeConfiguration<FormSection>
{
    public void Configure(EntityTypeBuilder<FormSection> builder)
    {
        builder.ToTable("form_sections", "procedures_config");
        builder.HasKey(s => s.Id).HasName("pk_form_sections");
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.StepId).HasColumnName("step_id").IsRequired();
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(s => s.Slug).HasColumnName("slug").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.IsCollapsible).HasColumnName("is_collapsible").HasDefaultValue(false);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(s => s.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(s => s.Step).WithMany(st => st.FormSections)
            .HasForeignKey(s => s.StepId)
            .HasConstraintName("fk_form_sections_procedure_steps")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TenantId, s.StepId, s.OrderIndex })
            .HasDatabaseName("ix_form_sections_tenant_step_order");
    }
}
