namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Conector API declarativo asociado a un tipo de trámite.
/// param_bindings es read-only una vez creado para proteger trámites activos.
/// schema: procedures_config / tabla: api_connectors
/// </summary>
public sealed class ApiConnector
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>GET | POST | PUT | PATCH</summary>
    public string HttpVerb { get; set; } = "GET";
    public int StepOrder { get; set; }
    /// <summary>JSONB — bindings de parámetros: { "param": "step_2.field_placa" }</summary>
    public string ParamBindings { get; set; } = "{}";
    /// <summary>JSONB — diccionario semántico de respuesta (read-only tras crear).</summary>
    public string ResponseMappings { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
}
