namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Campo de formulario en una sección. El JSONB config varía por field_type (ADR-0009).
/// schema: procedures_config / tabla: form_fields
/// </summary>
public sealed class FormField
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Guid TenantId { get; set; }
    public int OrderIndex { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>text | dropdown | checkbox | numeric | attachment | list</summary>
    public string FieldType { get; set; } = "text";
    public bool IsRequired { get; set; }
    /// <summary>JSONB — opciones, validaciones, etc. según field_type.</summary>
    public string Config { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public FormSection Section { get; set; } = null!;
}
