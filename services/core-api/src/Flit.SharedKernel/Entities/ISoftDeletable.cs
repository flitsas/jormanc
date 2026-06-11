namespace Flit.SharedKernel.Entities;

/// <summary>
/// Entidad con soporte de borrado lógico.
/// Nunca usar DbContext.Remove() — usar SoftDelete() en el repositorio.
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
