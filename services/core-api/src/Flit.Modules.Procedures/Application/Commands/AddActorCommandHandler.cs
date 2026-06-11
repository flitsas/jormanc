using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.Procedures.Domain.Services;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Commands;

public sealed class AddActorCommandHandler(
    IProcedureRepository procedureRepository,
    IProcedureTypeRepository procedureTypeRepository,
    IPersonQueryService personQueryService,
    ILegalEntityQueryService legalEntityQueryService,
    IClock clock)
{
    private static readonly HashSet<string> ValidNatures =
        ["natural", "juridica"];

    public async Task<Result<AddActorResponseDto, ProcedureError>> HandleAsync(
        AddActorCommand command, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.NotFound);

        if (procedure.Status != "draft")
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.InvalidStatus);

        var actorDef = await procedureTypeRepository.FindActorAsync(
            procedure.ProcedureTypeId, command.ActorDefinitionId, command.TenantId, ct);
        if (actorDef is null)
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.ActorDefinitionNotFound);

        if (!ValidNatures.Contains(command.Nature))
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.InvalidNature);

        if (!IsNatureAllowed(actorDef.AllowedNature, command.Nature))
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.InvalidNature);

        if (command.CuotaPct.HasValue)
        {
            var existing = await procedureRepository.GetActorsAsync(
                command.ProcedureId, command.TenantId, command.ActorDefinitionId, ct);
            var validation = CuotaValidator.Validate(
                command.CuotaPct.Value,
                existing.Select(a => a.CuotaPct));

            if (!validation.IsValid)
            {
                return Result<AddActorResponseDto, ProcedureError>.Failure(
                    ProcedureError.CuotaSumExceeds100(validation.CurrentSum, command.CuotaPct.Value));
            }
        }

        var now = clock.UtcNow;

        return command.Nature switch
        {
            "natural" => await AddNaturalActorAsync(command, procedure, actorDef, now, ct),
            "juridica" => await AddJuridicaActorAsync(command, procedure, actorDef, now, ct),
            _ => Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.InvalidNature)
        };
    }

    private async Task<Result<AddActorResponseDto, ProcedureError>> AddNaturalActorAsync(
        AddActorCommand command,
        Procedure procedure,
        ActorDefinition actorDef,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.DocumentNumber))
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.PersonQueryFailed);

        var docNumber = command.DocumentNumber.Trim();
        var person = await personQueryService.QueryByDocumentAsync(command.TenantId, docNumber, ct);
        if (!person.Found)
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.PersonQueryFailed);

        var actor = new ProcedureActor
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedure.Id,
            TenantId = command.TenantId,
            ActorDefinitionId = command.ActorDefinitionId,
            Nature = "natural",
            DocumentType = command.DocumentType ?? "CC",
            DocumentNumber = docNumber,
            FullName = person.FullName,
            CuotaPct = command.CuotaPct,
            QueryResults = person.QueryResultsJson,
            CreatedAt = now,
            CreatedBy = command.UserId,
            UpdatedAt = now,
            UpdatedBy = command.UserId
        };

        await procedureRepository.AddActorsAsync([actor], ct);

        return Result<AddActorResponseDto, ProcedureError>.Success(
            new AddActorResponseDto(
                ActorId: actor.Id,
                Nature: actor.Nature,
                QueryResults: JsonSerializer.Deserialize<object>(person.QueryResultsJson),
                Warnings: person.Warnings));
    }

    private async Task<Result<AddActorResponseDto, ProcedureError>> AddJuridicaActorAsync(
        AddActorCommand command,
        Procedure procedure,
        ActorDefinition actorDef,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nit))
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.LegalEntityQueryFailed);

        var nit = command.Nit.Trim();
        var entity = await legalEntityQueryService.QueryByNitAsync(command.TenantId, nit, ct);
        if (!entity.Found)
            return Result<AddActorResponseDto, ProcedureError>.Failure(ProcedureError.LegalEntityQueryFailed);

        var juridicaId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var repActorDefId = actorDef.LegalRepActorId ?? command.ActorDefinitionId;

        var juridicaActor = new ProcedureActor
        {
            Id = juridicaId,
            ProcedureId = procedure.Id,
            TenantId = command.TenantId,
            ActorDefinitionId = command.ActorDefinitionId,
            Nature = "juridica",
            Nit = nit,
            FullName = entity.CompanyName,
            CuotaPct = command.CuotaPct,
            QueryResults = entity.RuesPayloadJson,
            CreatedAt = now,
            CreatedBy = command.UserId,
            UpdatedAt = now,
            UpdatedBy = command.UserId
        };

        var repActor = new ProcedureActor
        {
            Id = repId,
            ProcedureId = procedure.Id,
            TenantId = command.TenantId,
            ActorDefinitionId = repActorDefId,
            Nature = "representante_legal",
            ParentActorId = juridicaId,
            DocumentNumber = entity.RepresentativeDocument,
            FullName = entity.RepresentativeName,
            QueryResults = "{}",
            CreatedAt = now,
            CreatedBy = command.UserId,
            UpdatedAt = now,
            UpdatedBy = command.UserId
        };

        await procedureRepository.AddActorsAsync([juridicaActor, repActor], ct);

        return Result<AddActorResponseDto, ProcedureError>.Success(
            new AddActorResponseDto(
                ActorId: juridicaId,
                Nature: "juridica",
                QueryResults: JsonSerializer.Deserialize<object>(entity.RuesPayloadJson),
                Warnings: entity.Warnings,
                LegalRepresentativeActorId: repId));
    }

    private static bool IsNatureAllowed(string allowedNature, string requestedNature) =>
        allowedNature switch
        {
            "ambas" => requestedNature is "natural" or "juridica",
            "natural" => requestedNature == "natural",
            "juridica" => requestedNature == "juridica",
            _ => false
        };
}
