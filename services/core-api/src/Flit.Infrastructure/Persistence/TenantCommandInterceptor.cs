using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Flit.Infrastructure.Persistence;

/// <summary>
/// Reaplica app.tenant_id antes de cada comando (pooling Npgsql no dispara ConnectionOpened).
/// </summary>
public sealed class TenantCommandInterceptor(IHttpContextAccessor httpContextAccessor)
    : DbCommandInterceptor
{
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(command, eventData, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(command, eventData, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(command, eventData, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private async Task EnsureSessionAsync(
        DbCommand command,
        CommandEventData eventData,
        CancellationToken cancellationToken)
    {
        if (TenantSessionContext.IsSessionSetupCommand(command))
            return;

        if (command.Connection is null)
            return;

        if (command.Connection.State != ConnectionState.Open)
            await command.Connection.OpenAsync(cancellationToken);

        await TenantSessionContext.ApplyAsync(command.Connection, httpContextAccessor, cancellationToken);
    }
}
