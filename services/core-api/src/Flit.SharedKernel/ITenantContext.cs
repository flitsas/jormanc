namespace Flit.SharedKernel;

/// <summary>Contexto de tenant/usuario de la petición HTTP actual (JWT).</summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
}
