namespace Flit.Modules.Integrations.Domain.Errors;

/// <summary>
/// Excepción lanzada por los conectores cuando el proveedor externo devuelve HTTP 5xx.
/// ConnectorRouter la captura para ejecutar el failover automático (ADR-0011).
/// </summary>
public sealed class ConnectorServerException(string provider, int httpStatus, string operation)
    : Exception($"El proveedor '{provider}' retornó HTTP {httpStatus} para la operación '{operation}'.")
{
    public string Provider { get; } = provider;
    public int HttpStatus { get; } = httpStatus;
    public string Operation { get; } = operation;
}
