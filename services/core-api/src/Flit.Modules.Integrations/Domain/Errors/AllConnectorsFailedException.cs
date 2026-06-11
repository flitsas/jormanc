namespace Flit.Modules.Integrations.Domain.Errors;

/// <summary>
/// Excepción de infraestructura lanzada por ConnectorRouter cuando todos los proveedores
/// de una operación fallaron (timeout o HTTP 5xx).
/// El handler de uso convierte esto en IntegrationsError.AllConnectorsFailed.
/// </summary>
public sealed class AllConnectorsFailedException(string operation, int attemptCount = 0)
    : Exception($"Todos los conectores fallaron para la operación '{operation}' tras {attemptCount} intento(s).")
{
    public string Operation { get; } = operation;
    public int AttemptCount { get; } = attemptCount;
}
