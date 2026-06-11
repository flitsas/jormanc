namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Sección de formulario dentro de un paso de trámite.
/// schema: procedures_config / tabla: form_sections
/// </summary>
public sealed class FormSection
{
    public Guid Id { get; set; }
    public Guid StepId { get; set; }
    public Guid TenantId { get; set; }
    public int OrderIndex { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCollapsible { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ProcedureStep Step { get; set; } = null!;
    public ICollection<FormField> FormFields { get; set; } = [];
}
