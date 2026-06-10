using System.Text.Json;

namespace Flit.Modules.ProceduresConfig.Ports;

/// <summary>Invoca endpoints del catálogo cuando una regla dispara call_endpoint (AC2 #9439).</summary>
public interface IRuleEndpointInvoker
{
    Task InvokeFromRuleAsync(
        Guid tenantId,
        string endpointCode,
        Guid? procedureInstanceId,
        Guid ruleId,
        string ruleName,
        CancellationToken ct = default);
}

public sealed class NoOpRuleEndpointInvoker : IRuleEndpointInvoker
{
    public Task InvokeFromRuleAsync(
        Guid tenantId,
        string endpointCode,
        Guid? procedureInstanceId,
        Guid ruleId,
        string ruleName,
        CancellationToken ct = default) => Task.CompletedTask;
}
