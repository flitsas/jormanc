namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Campo/marcador de una plantilla documental.
/// schema: documents / tabla: template_fields
/// </summary>
public sealed class TemplateField
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    /// <summary>Ej: "actor[vendedor].full_name"</summary>
    public string Marker { get; set; } = string.Empty;
    /// <summary>actor | vehicle | procedure | identity | ot</summary>
    public string DataSource { get; set; } = string.Empty;
    /// <summary>JSONPath en el contexto de resolución.</summary>
    public string DataPath { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DocumentTemplate Template { get; set; } = null!;
}
