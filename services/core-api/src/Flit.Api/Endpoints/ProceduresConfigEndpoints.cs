using System.Text.Json;
using Flit.Modules.ProceduresConfig.Adapters;
using Flit.Modules.ProceduresConfig.Application;
using Flit.Modules.ProceduresConfig.Ports;

namespace Flit.Api.Endpoints;

/// <summary>API RGL-02 (#9438) y RGL-03 (#9439) — reglas y catálogo de endpoints.</summary>
public static class ProceduresConfigEndpoints
{
    public static readonly Guid DefaultActorUserId = Guid.Parse("00000000-0000-7000-8000-000000000001");

    public sealed record EvaluateRulesRequest(
        Guid TenantId,
        Guid ProcedureTypeId,
        Dictionary<string, string?> CapturedFields,
        JsonDocument? ConfigSnapshot = null,
        Guid? ProcedureInstanceId = null);

    public sealed record CreateEndpointRequest(
        Guid TenantId,
        string Code,
        string Name,
        string Url,
        string Method,
        string AuthType,
        JsonElement AuthConfig,
        int TimeoutMs = 5000,
        bool IsActive = true,
        Guid? ActorUserId = null);

    public sealed record UpdateEndpointRequest(
        Guid TenantId,
        string Name,
        string Url,
        string Method,
        string AuthType,
        JsonElement AuthConfig,
        int TimeoutMs,
        bool IsActive,
        int RowVersion,
        Guid? ActorUserId = null);

    public sealed record InvokeEndpointRequest(
        Guid TenantId,
        JsonElement? Payload = null,
        Guid? ProcedureInstanceId = null);

    public static void MapProceduresConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var rules = app.MapGroup("/api/v1/procedures-config/rules")
            .WithTags("Procedures Config - Rules");

        rules.MapPost("/evaluate", async (
            EvaluateRulesRequest req,
            IProcedureRulesRepository rulesRepo,
            IRuleEndpointInvoker endpointInvoker,
            CancellationToken ct) =>
        {
            var response = await EvaluateProcedureRules.HandleAsync(
                new EvaluateProcedureRules.Query(
                    req.TenantId,
                    req.ProcedureTypeId,
                    req.CapturedFields,
                    req.ConfigSnapshot,
                    req.ProcedureInstanceId),
                rulesRepo,
                endpointInvoker,
                ct);

            return Results.Ok(new
            {
                matchedRules = response.MatchedRules.Select(m => new
                {
                    ruleId = m.RuleId,
                    ruleName = m.RuleName,
                    priority = m.Priority,
                    actions = m.Actions.Select(a => new { type = a.Type, @params = a.Params }),
                }),
                actions = response.Actions.Select(a => new { type = a.Type, @params = a.Params }),
            });
        })
        .WithName("EvaluateProcedureRules")
        .WithSummary("Evalúa reglas activas (o snapshot) y devuelve acciones");

        var endpoints = app.MapGroup("/api/v1/procedures-config/endpoints")
            .WithTags("Procedures Config - Endpoint Catalog");

        endpoints.MapGet("/", async (Guid tenantId, IEndpointCatalogRepository repo, CancellationToken ct) =>
        {
            var items = await ListEndpointCatalog.HandleAsync(
                new ListEndpointCatalog.Query(tenantId), repo, ct);
            return Results.Ok(items);
        })
        .WithName("ListEndpointCatalog");

        endpoints.MapGet("/{id:guid}", async (
            Guid id, Guid tenantId, IEndpointCatalogRepository repo, CancellationToken ct) =>
        {
            var item = await GetEndpointCatalogEntry.HandleAsync(
                new GetEndpointCatalogEntry.Query(tenantId, id), repo, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetEndpointCatalogEntry");

        endpoints.MapPost("/", async (
            CreateEndpointRequest req, IEndpointCatalogRepository repo, CancellationToken ct) =>
        {
            var result = await CreateEndpointCatalogEntry.HandleAsync(
                new CreateEndpointCatalogEntry.Command(
                    req.TenantId,
                    req.Code,
                    req.Name,
                    req.Url,
                    req.Method,
                    req.AuthType,
                    req.AuthConfig,
                    req.TimeoutMs,
                    req.IsActive,
                    req.ActorUserId ?? DefaultActorUserId),
                repo,
                ct);

            return result.Match(
                ok => Results.Created($"/api/v1/procedures-config/endpoints/{ok.Id}", ok),
                MapCatalogError);
        })
        .WithName("CreateEndpointCatalogEntry");

        endpoints.MapPatch("/{id:guid}", async (
            Guid id, UpdateEndpointRequest req, IEndpointCatalogRepository repo, CancellationToken ct) =>
        {
            var result = await UpdateEndpointCatalogEntry.HandleAsync(
                new UpdateEndpointCatalogEntry.Command(
                    req.TenantId,
                    id,
                    req.Name,
                    req.Url,
                    req.Method,
                    req.AuthType,
                    req.AuthConfig,
                    req.TimeoutMs,
                    req.IsActive,
                    req.RowVersion,
                    req.ActorUserId ?? DefaultActorUserId),
                repo,
                ct);

            return result.Match(Results.Ok, MapCatalogError);
        })
        .WithName("UpdateEndpointCatalogEntry");

        endpoints.MapDelete("/{id:guid}", async (
            Guid id, Guid tenantId, Guid? actorUserId,
            IEndpointCatalogRepository repo, CancellationToken ct) =>
        {
            var result = await DeleteEndpointCatalogEntry.HandleAsync(
                new DeleteEndpointCatalogEntry.Command(
                    tenantId, id, actorUserId ?? DefaultActorUserId),
                repo,
                ct);

            return result.Match(_ => Results.NoContent(), MapCatalogError);
        })
        .WithName("DeleteEndpointCatalogEntry");

        endpoints.MapPost("/by-code/{code}/invoke", async (
            string code,
            InvokeEndpointRequest req,
            IEndpointCatalogRepository catalogRepo,
            IEndpointCallLogRepository callLogRepo,
            EndpointInvocationRateLimiter rateLimiter,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            var result = await InvokeCatalogEndpoint.HandleAsync(
                new InvokeCatalogEndpoint.Command(
                    req.TenantId, code, req.Payload, req.ProcedureInstanceId),
                catalogRepo,
                callLogRepo,
                rateLimiter,
                httpClientFactory,
                ct);

            return result.Match(Results.Ok, MapCatalogError);
        })
        .WithName("InvokeCatalogEndpoint");
    }

    private static IResult MapCatalogError(EndpointCatalogError err) => err.Kind switch
    {
        EndpointCatalogErrorKind.NotFound => Results.NotFound(new { error = err.Message }),
        EndpointCatalogErrorKind.Conflict => Results.Conflict(new { error = err.Message }),
        EndpointCatalogErrorKind.Validation => Results.BadRequest(new { error = err.Message }),
        EndpointCatalogErrorKind.RateLimited => Results.Json(
            new { error = err.Message },
            statusCode: StatusCodes.Status429TooManyRequests),
        _ => Results.Problem(err.Message),
    };
}
