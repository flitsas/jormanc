using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Queries;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints del parametrizador — HU-9779.
/// CRUD tipos de trámite, pasos, secciones y campos.
/// </summary>
public static class ProcedureTypesEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";

    public static IEndpointRouteBuilder MapProcedureTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/procedure-types").WithTags("ProceduresConfig");

        group.MapPost("/", HandleCreateProcedureTypeAsync)
            .WithName("CreateProcedureType")
            .RequireAuthorization()
            .Produces<ProcedureTypeResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", HandleGetProcedureTypeAsync)
            .WithName("GetProcedureType")
            .RequireAuthorization()
            .Produces<ProcedureTypeResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", HandleDeleteProcedureTypeAsync)
            .WithName("DeleteProcedureType")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/steps", HandleCreateStepAsync)
            .WithName("CreateProcedureStep")
            .RequireAuthorization()
            .Produces<ProcedureStepResponse>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/steps/{stepId:guid}/sections", HandleCreateSectionAsync)
            .WithName("CreateFormSection")
            .RequireAuthorization()
            .Produces<FormSectionResponse>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/steps/{stepId:guid}/sections/{sectionId:guid}/fields", HandleCreateFieldAsync)
            .WithName("CreateFormField")
            .RequireAuthorization()
            .Produces<FormFieldResponse>(StatusCodes.Status201Created);

        // HU-9780 — Rule sets + simulador de coherencia
        group.MapPost("/{id:guid}/rules", HandleCreateRuleSetAsync)
            .WithName("CreateRuleSet")
            .RequireAuthorization()
            .Produces<RuleSetResponse>(StatusCodes.Status201Created)
            .Produces<RuleConflictResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/rules/simulate", HandleSimulateCoherenceAsync)
            .WithName("SimulateCoherence")
            .RequireAuthorization()
            .Produces<CoherenceSimulationResponse>(StatusCodes.Status200OK);

        // HU-9781 — Actores, query-rules y vehicle-query
        group.MapPost("/{id:guid}/actors", HandleCreateActorAsync)
            .WithName("CreateActorDefinition")
            .RequireAuthorization()
            .Produces<ActorDefinitionResponse>(StatusCodes.Status201Created);

        group.MapPost("/{id:guid}/actors/{actorId:guid}/query-rules", HandleCreateQueryRuleAsync)
            .WithName("CreateQueryRule")
            .RequireAuthorization()
            .Produces<QueryRuleResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}/vehicle-query", HandleSetVehicleQueryKeyAsync)
            .WithName("SetVehicleQueryKey")
            .RequireAuthorization()
            .Produces<ProcedureTypeResponse>(StatusCodes.Status200OK);

        // HU-9782 — PUT steps, fields, api-connectors
        group.MapPost("/{id:guid}/api-connectors", HandleCreateApiConnectorAsync)
            .WithName("CreateApiConnector")
            .RequireAuthorization()
            .Produces<ApiConnectorResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}/steps/{stepId:guid}", HandleUpdateStepAsync)
            .WithName("UpdateProcedureStep")
            .RequireAuthorization()
            .Produces<ProcedureStepResponse>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/steps/{stepId:guid}/sections/{sectionId:guid}/fields/{fieldId:guid}",
                HandleUpdateFieldAsync)
            .WithName("UpdateFormField")
            .RequireAuthorization()
            .Produces<FormFieldResponse>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/api-connectors/{connId:guid}", HandleUpdateApiConnectorAsync)
            .WithName("UpdateApiConnector")
            .RequireAuthorization()
            .Produces<ApiConnectorResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> HandleCreateProcedureTypeAsync(
        [FromBody] CreateProcedureTypeRequest request,
        HttpContext ctx,
        CreateProcedureTypeCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (!TryValidateCreateProcedureType(request, out var errors))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", string.Join("; ", errors)));

        var command = new CreateProcedureTypeCommand(
            TenantId: tenantId,
            RequestedByUserId: userId,
            Name: request.Name.Trim(),
            Family: request.Family.Trim().ToLowerInvariant(),
            Scope: request.Scope.Trim().ToLowerInvariant(),
            ScopeRefId: request.ScopeRefId,
            VehicleQueryKey: (request.VehicleQueryKey ?? "placa").Trim().ToLowerInvariant());

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/procedure-types/{dto.Id}", MapProcedureType(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleGetProcedureTypeAsync(
        Guid id,
        HttpContext ctx,
        GetProcedureTypeQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetProcedureTypeQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapProcedureType(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleDeleteProcedureTypeAsync(
        Guid id,
        HttpContext ctx,
        DeleteProcedureTypeCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new DeleteProcedureTypeCommand(id, tenantId, userId), ct);
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleCreateStepAsync(
        Guid id,
        [FromBody] CreateProcedureStepRequest request,
        HttpContext ctx,
        CreateProcedureStepCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "name es requerido."));

        var command = new CreateProcedureStepCommand(
            ProcedureTypeId: id,
            TenantId: tenantId,
            RequestedByUserId: userId,
            Name: request.Name,
            StepType: request.StepType ?? "form",
            OrderIndex: request.OrderIndex < 1 ? 1 : request.OrderIndex,
            IsRequired: request.IsRequired);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/steps/{dto.Id}",
                MapStep(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleCreateSectionAsync(
        Guid id,
        Guid stepId,
        [FromBody] CreateFormSectionRequest request,
        HttpContext ctx,
        CreateFormSectionCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "slug y name son requeridos."));

        var command = new CreateFormSectionCommand(
            ProcedureTypeId: id,
            StepId: stepId,
            TenantId: tenantId,
            RequestedByUserId: userId,
            Slug: request.Slug,
            Name: request.Name,
            OrderIndex: request.OrderIndex < 1 ? 1 : request.OrderIndex,
            IsCollapsible: request.IsCollapsible);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/steps/{stepId}/sections/{dto.Id}",
                MapSection(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleCreateFieldAsync(
        Guid id,
        Guid stepId,
        Guid sectionId,
        [FromBody] CreateFormFieldRequest request,
        HttpContext ctx,
        CreateFormFieldCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.FieldType))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "slug y field_type son requeridos."));

        var configJson = request.Config is null
            ? "{}"
            : JsonSerializer.Serialize(request.Config);

        var command = new CreateFormFieldCommand(
            ProcedureTypeId: id,
            StepId: stepId,
            SectionId: sectionId,
            TenantId: tenantId,
            RequestedByUserId: userId,
            Slug: request.Slug,
            Name: request.Name ?? request.Slug,
            FieldType: request.FieldType,
            IsRequired: request.IsRequired,
            ConfigJson: configJson,
            OrderIndex: request.OrderIndex < 1 ? 1 : request.OrderIndex);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/steps/{stepId}/sections/{sectionId}/fields/{dto.Id}",
                MapField(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static (Guid TenantId, Guid UserId, IResult? Error) ExtractSuperAdminClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty,
                Results.Json(new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                    statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        if (!roles.Contains(SuperAdminRoleClaim))
        {
            return (Guid.Empty, Guid.Empty,
                Results.Json(new ErrorResponse("FORBIDDEN", "Se requiere el rol superadmin."),
                    statusCode: StatusCodes.Status403Forbidden));
        }

        return (tenantId, userId, null);
    }

    private static async Task<IResult> HandleCreateRuleSetAsync(
        Guid id,
        [FromBody] CreateRuleSetRequest request,
        HttpContext ctx,
        CreateRuleSetCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "name es requerido."));

        var conditionsJson = request.Conditions is null ? "{}" : JsonSerializer.Serialize(request.Conditions);
        var actionsJson = request.Actions is null ? "[]" : JsonSerializer.Serialize(request.Actions);

        var command = new CreateRuleSetCommand(id, tenantId, userId, request.Name, conditionsJson, actionsJson);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/procedure-types/{id}/rules/{dto.Id}", MapRuleSet(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleSimulateCoherenceAsync(
        Guid id,
        [FromBody] SimulateRuleRequest request,
        HttpContext ctx,
        SimulateCoherenceQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var conditionsJson = request.Conditions is null ? "{}" : JsonSerializer.Serialize(request.Conditions);
        var actionsJson = request.Actions is null ? "[]" : JsonSerializer.Serialize(request.Actions);

        var query = new SimulateCoherenceQuery(
            id, tenantId, request.Name ?? "candidata", conditionsJson, actionsJson);

        var result = await handler.HandleAsync(query, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(new CoherenceSimulationResponse(
                dto.IsCoherent,
                dto.Conflicts.Select(c => new RuleConflictItemResponse(c.Rule1, c.Rule2, c.Description)).ToArray())),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleCreateActorAsync(
        Guid id,
        [FromBody] CreateActorDefinitionRequest request,
        HttpContext ctx,
        CreateActorDefinitionCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Role))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "role es requerido."));

        var command = new CreateActorDefinitionCommand(
            ProcedureTypeId: id,
            TenantId: tenantId,
            RequestedByUserId: userId,
            Role: request.Role,
            AllowedNature: request.AllowedNature ?? "ambas",
            MinCount: request.MinCount < 1 ? 1 : request.MinCount,
            MaxCount: request.MaxCount < 1 ? 1 : request.MaxCount,
            IsRequired: request.IsRequired,
            OrderIndex: request.OrderIndex,
            LegalRepActorId: request.LegalRepActorId);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/actors/{dto.Id}",
                MapActor(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleCreateQueryRuleAsync(
        Guid id,
        Guid actorId,
        [FromBody] CreateQueryRuleRequest request,
        HttpContext ctx,
        CreateQueryRuleCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.SubjectType) || string.IsNullOrWhiteSpace(request.EntryKey))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "subject_type y entry_key son requeridos."));

        var verificationsJson = request.Verifications is null
            ? "[]"
            : JsonSerializer.Serialize(request.Verifications);

        var command = new CreateQueryRuleCommand(
            ProcedureTypeId: id,
            ActorId: actorId,
            TenantId: tenantId,
            RequestedByUserId: userId,
            SubjectType: request.SubjectType,
            EntryKey: request.EntryKey,
            IsBlocking: request.IsBlocking,
            VerificationsJson: verificationsJson);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/actors/{actorId}/query-rules/{dto.Id}",
                MapQueryRule(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleSetVehicleQueryKeyAsync(
        Guid id,
        [FromBody] SetVehicleQueryKeyRequest request,
        HttpContext ctx,
        SetVehicleQueryKeyCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.QueryKey))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "query_key es requerido."));

        var command = new SetVehicleQueryKeyCommand(id, tenantId, userId, request.QueryKey);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapProcedureType(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static IResult MapProcedureTypeError(ProcedureTypeError err)
    {
        if (err.Code == "RULE_SET_CONFLICT" && err.Conflicts is { Count: > 0 })
        {
            return Results.Conflict(new RuleConflictResponse(
                err.Code,
                err.Message,
                err.Conflicts.Select(c => new RuleConflictItemResponse(c.Rule1, c.Rule2, c.Description)).ToArray()));
        }

        return err.Code switch
        {
            "PROCEDURE_TYPE_NOT_FOUND" or "PROCEDURE_STEP_NOT_FOUND" or "FORM_SECTION_NOT_FOUND"
                or "FORM_FIELD_NOT_FOUND" or "API_CONNECTOR_NOT_FOUND"
                or "ACTOR_NOT_FOUND" or "LEGAL_REP_ACTOR_NOT_FOUND" =>
                Results.NotFound(new ErrorResponse(err.Code, err.Message)),
            "PROCEDURE_TYPE_SLUG_ALREADY_EXISTS" or "PROCEDURE_TYPE_HAS_ACTIVE_PROCEDURES" =>
                Results.Conflict(new ErrorResponse(err.Code, err.Message)),
            _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
        };
    }

    private static async Task<IResult> HandleCreateApiConnectorAsync(
        Guid id,
        [FromBody] CreateApiConnectorRequest request,
        HttpContext ctx,
        CreateApiConnectorCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Endpoint))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "name y endpoint son requeridos."));

        var bindingsJson = request.ParamBindings is null
            ? "{}"
            : JsonSerializer.Serialize(request.ParamBindings);

        var command = new CreateApiConnectorCommand(
            id, tenantId, userId,
            request.Name, request.Endpoint,
            request.HttpVerb ?? "GET",
            request.StepOrder,
            bindingsJson);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/api-connectors/{dto.Id}",
                MapApiConnector(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleUpdateStepAsync(
        Guid id,
        Guid stepId,
        [FromBody] UpdateProcedureStepRequest request,
        HttpContext ctx,
        UpdateProcedureStepCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (request.OrderIndex < 1)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "order_index debe ser >= 1."));

        var command = new UpdateProcedureStepCommand(
            id, stepId, tenantId, userId, request.OrderIndex, request.Name);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapStep(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleUpdateFieldAsync(
        Guid id,
        Guid stepId,
        Guid sectionId,
        Guid fieldId,
        [FromBody] UpdateFormFieldRequest request,
        HttpContext ctx,
        UpdateFormFieldCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var configJson = request.Config is null ? null : JsonSerializer.Serialize(request.Config);

        var command = new UpdateFormFieldCommand(
            id, stepId, sectionId, fieldId, tenantId, userId,
            request.Name, request.FieldType, request.IsRequired, configJson);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapField(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static async Task<IResult> HandleUpdateApiConnectorAsync(
        Guid id,
        Guid connId,
        [FromBody] UpdateApiConnectorRequest request,
        HttpContext ctx,
        UpdateApiConnectorCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractSuperAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var bindingsJson = request.ParamBindings is null
            ? "{}"
            : JsonSerializer.Serialize(request.ParamBindings);

        var command = new UpdateApiConnectorCommand(id, connId, tenantId, userId, bindingsJson);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapApiConnector(dto)),
            onFailure: MapProcedureTypeError);
    }

    private static ActorDefinitionResponse MapActor(ActorDefinitionDto dto) =>
        new(dto.Id, dto.ProcedureTypeId, dto.TenantId, dto.Role, dto.AllowedNature,
            dto.MinCount, dto.MaxCount, dto.IsRequired, dto.OrderIndex, dto.LegalRepActorId, dto.CreatedAt);

    private static QueryRuleResponse MapQueryRule(QueryRuleDto dto)
    {
        JsonElement verifications;
        try { verifications = JsonSerializer.Deserialize<JsonElement>(dto.Verifications); }
        catch { verifications = JsonSerializer.Deserialize<JsonElement>("[]"); }

        return new(dto.Id, dto.ActorDefinitionId, dto.TenantId, dto.SubjectType, dto.EntryKey,
            dto.IsBlocking, verifications, dto.CreatedAt);
    }

    private static RuleSetResponse MapRuleSet(RuleSetDto dto)
    {
        JsonElement conditions;
        JsonElement actions;
        try { conditions = JsonSerializer.Deserialize<JsonElement>(dto.Conditions); }
        catch { conditions = JsonSerializer.Deserialize<JsonElement>("{}"); }
        try { actions = JsonSerializer.Deserialize<JsonElement>(dto.Actions); }
        catch { actions = JsonSerializer.Deserialize<JsonElement>("[]"); }

        return new RuleSetResponse(
            dto.Id, dto.ProcedureTypeId, dto.TenantId, dto.Name,
            conditions, actions, dto.IsActive, dto.CreatedAt);
    }

    private static bool TryValidateCreateProcedureType(CreateProcedureTypeRequest req, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name es requerido.");
        if (string.IsNullOrWhiteSpace(req.Family)) errors.Add("family es requerido.");
        if (string.IsNullOrWhiteSpace(req.Scope)) errors.Add("scope es requerido.");
        return errors.Count == 0;
    }

    private static ApiConnectorResponse MapApiConnector(ApiConnectorDto dto)
    {
        JsonElement paramBindings;
        JsonElement responseMappings;
        try { paramBindings = JsonSerializer.Deserialize<JsonElement>(dto.ParamBindings); }
        catch { paramBindings = JsonSerializer.Deserialize<JsonElement>("{}"); }
        try { responseMappings = JsonSerializer.Deserialize<JsonElement>(dto.ResponseMappings); }
        catch { responseMappings = JsonSerializer.Deserialize<JsonElement>("{}"); }

        return new(dto.Id, dto.ProcedureTypeId, dto.TenantId, dto.Name, dto.Endpoint,
            dto.HttpVerb, dto.StepOrder, paramBindings, responseMappings, dto.IsActive, dto.CreatedAt);
    }

    private static ProcedureTypeResponse MapProcedureType(ProcedureTypeDto dto) =>
        new(dto.Id, dto.TenantId, dto.Slug, dto.Name, dto.Family, dto.Scope, dto.ScopeRefId,
            dto.VehicleQueryKey, dto.Version, dto.IsActive, dto.CreatedAt,
            dto.Steps.Select(MapStep).ToArray(),
            dto.ApiConnectors.Select(MapApiConnector).ToArray());

    private static ProcedureStepResponse MapStep(ProcedureStepDto dto) =>
        new(dto.Id, dto.ProcedureTypeId, dto.OrderIndex, dto.Name, dto.StepType, dto.IsRequired,
            dto.Sections.Select(MapSection).ToArray());

    private static FormSectionResponse MapSection(FormSectionDto dto) =>
        new(dto.Id, dto.StepId, dto.OrderIndex, dto.Slug, dto.Name, dto.IsCollapsible,
            dto.Fields.Select(MapField).ToArray());

    private static FormFieldResponse MapField(FormFieldDto dto)
    {
        JsonElement config;
        try
        {
            config = JsonSerializer.Deserialize<JsonElement>(dto.Config);
        }
        catch
        {
            config = JsonSerializer.Deserialize<JsonElement>("{}");
        }

        return new(dto.Id, dto.SectionId, dto.OrderIndex, dto.Slug, dto.Name, dto.FieldType,
            dto.IsRequired, config);
    }
}

public sealed record CreateProcedureTypeRequest(
    [property: Required] string Name,
    [property: Required] string Family,
    [property: Required] string Scope,
    Guid? ScopeRefId,
    string? VehicleQueryKey);

public sealed record CreateProcedureStepRequest(
    [property: Required] string Name,
    string? StepType,
    int OrderIndex,
    bool IsRequired = true);

public sealed record CreateFormSectionRequest(
    [property: Required] string Slug,
    [property: Required] string Name,
    int OrderIndex,
    bool IsCollapsible = false);

public sealed record CreateFormFieldRequest(
    [property: Required] string Slug,
    string? Name,
    [property: Required] string FieldType,
    bool IsRequired,
    JsonElement? Config,
    int OrderIndex);

public sealed record UpdateProcedureStepRequest(int OrderIndex, string? Name);

public sealed record UpdateFormFieldRequest(
    string? Name,
    string? FieldType,
    bool? IsRequired,
    JsonElement? Config);

public sealed record CreateApiConnectorRequest(
    string Name,
    string Endpoint,
    string? HttpVerb,
    int StepOrder,
    JsonElement? ParamBindings);

public sealed record UpdateApiConnectorRequest(JsonElement? ParamBindings);

public sealed record ProcedureTypeResponse(
    Guid Id, Guid TenantId, string Slug, string Name, string Family, string Scope,
    Guid? ScopeRefId, string VehicleQueryKey, int Version, bool IsActive,
    DateTimeOffset CreatedAt, ProcedureStepResponse[] Steps, ApiConnectorResponse[] ApiConnectors);

public sealed record ApiConnectorResponse(
    Guid Id, Guid ProcedureTypeId, Guid TenantId, string Name, string Endpoint,
    string HttpVerb, int StepOrder, JsonElement ParamBindings, JsonElement ResponseMappings,
    bool IsActive, DateTimeOffset CreatedAt);

public sealed record ProcedureStepResponse(
    Guid Id, Guid ProcedureTypeId, int OrderIndex, string Name, string StepType, bool IsRequired,
    FormSectionResponse[] Sections);

public sealed record FormSectionResponse(
    Guid Id, Guid StepId, int OrderIndex, string Slug, string Name, bool IsCollapsible,
    FormFieldResponse[] Fields);

public sealed record FormFieldResponse(
    Guid Id, Guid SectionId, int OrderIndex, string Slug, string Name, string FieldType,
    bool IsRequired, JsonElement Config);

public sealed record CreateRuleSetRequest(
    [property: Required] string Name,
    JsonElement? Conditions,
    JsonElement? Actions);

public sealed record SimulateRuleRequest(
    string? Name,
    JsonElement? Conditions,
    JsonElement? Actions);

public sealed record RuleSetResponse(
    Guid Id, Guid ProcedureTypeId, Guid TenantId, string Name,
    JsonElement Conditions, JsonElement Actions, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CoherenceSimulationResponse(
    bool IsCoherent,
    RuleConflictItemResponse[] Conflicts);

public sealed record RuleConflictResponse(
    string Error,
    string Message,
    RuleConflictItemResponse[] Conflicts);

public sealed record RuleConflictItemResponse(string Rule1, string Rule2, string Description);

public sealed record CreateActorDefinitionRequest(
    [property: Required] string Role,
    string? AllowedNature,
    int MinCount = 1,
    int MaxCount = 1,
    bool IsRequired = true,
    int OrderIndex = 0,
    Guid? LegalRepActorId = null);

public sealed record CreateQueryRuleRequest(
    [property: Required] string SubjectType,
    [property: Required] string EntryKey,
    bool IsBlocking = true,
    JsonElement? Verifications = null);

public sealed record SetVehicleQueryKeyRequest(
    [property: Required] string QueryKey);

public sealed record ActorDefinitionResponse(
    Guid Id, Guid ProcedureTypeId, Guid TenantId, string Role, string AllowedNature,
    int MinCount, int MaxCount, bool IsRequired, int OrderIndex, Guid? LegalRepActorId,
    DateTimeOffset CreatedAt);

public sealed record QueryRuleResponse(
    Guid Id, Guid ActorDefinitionId, Guid TenantId, string SubjectType, string EntryKey,
    bool IsBlocking, JsonElement Verifications, DateTimeOffset CreatedAt);
