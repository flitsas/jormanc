namespace Flit.Infrastructure.Persistence.Entities.Integrations;

/// <summary>
/// Resultado de validación biométrica de identidad (liveness Verifik).
/// Contiene referencias a archivos biométricos en MinIO. @pii:high
/// schema: integrations / tabla: identity_validations
/// </summary>
public sealed class IdentityValidation
{
    public Guid Id { get; set; }
    /// <summary>FK a procedures.procedures.</summary>
    public Guid ProcedureId { get; set; }
    /// <summary>FK a procedures.procedure_actors.</summary>
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>verifik | mock</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>approved | rejected | pending</summary>
    public string Verdict { get; set; } = "pending";
    /// <summary>Referencia MinIO del video de liveness. @pii:high</summary>
    public string? LivenessRef { get; set; }
    /// <summary>Referencia MinIO de la foto de documento. @pii:high</summary>
    public string? DocumentPhotoRef { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
