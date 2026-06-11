namespace Flit.SharedKernel.Entities;

/// <summary>
/// Entidad con trazabilidad de creación y actualización.
/// Los campos se rellenan por el AuditingInterceptor, nunca manualmente en handlers.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    Guid CreatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    Guid UpdatedBy { get; set; }
}
