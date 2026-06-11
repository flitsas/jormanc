using Flit.Modules.Integrations.Application.DTOs;
using Flit.Modules.Integrations.Domain.Interfaces;

namespace Flit.Modules.Integrations.Application.Queries;

/// <summary>
/// Handler de GetIntegrationLogsQuery.
/// AC3 HU-9775: retorna los logs de payload por tenant con paginación.
/// </summary>
public sealed class GetIntegrationLogsQueryHandler(IIntegrationLogRepository logRepository)
{
    public async Task<IntegrationLogsPageDto> HandleAsync(
        GetIntegrationLogsQuery query, CancellationToken ct = default)
    {
        var effectivePage = query.Page < 1 ? 1 : query.Page;
        var effectivePageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var filter = new IntegrationLogFilter(
            TenantId: query.TenantId,
            Page: effectivePage,
            PageSize: effectivePageSize,
            ConnectorType: query.ConnectorType,
            Provider: query.Provider,
            From: query.From,
            To: query.To);

        var (items, total) = await logRepository.ListAsync(filter, ct);

        return new IntegrationLogsPageDto(
            Data: items.Select(l => new IntegrationLogDto(
                l.Id, l.TenantId, l.ConnectorType, l.Operation, l.Provider,
                l.RequestPayload, l.ResponsePayload, l.HttpStatus,
                l.DurationMs, l.ErrorMessage, l.LoggedAt)).ToList(),
            Total: total,
            Page: effectivePage,
            PageSize: effectivePageSize);
    }
}

/// <summary>Resultado paginado de integration_logs.</summary>
public sealed record IntegrationLogsPageDto(
    IReadOnlyList<IntegrationLogDto> Data,
    int Total,
    int Page,
    int PageSize);
