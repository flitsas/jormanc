using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Helpers;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Events;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.Procedures.Domain.Services;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Commands;

public sealed class SubmitProcedureCommandHandler(
    IProcedureRepository procedureRepository,
    IProcedureTypeRepository procedureTypeRepository,
    IProcedureEventPublisher eventPublisher,
    IClock clock)
{
    public async Task<Result<SubmitProcedureResponseDto, ProcedureError>> HandleAsync(
        SubmitProcedureCommand command, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(ProcedureError.NotFound);

        if (procedure.Status != "draft")
            return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(ProcedureError.InvalidStatus);

        var snapshot = await procedureTypeRepository.FindSnapshotByIdAsync(
            procedure.ProcedureTypeSnapshotId, ct);
        if (snapshot is null)
            return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(ProcedureError.ProcedureTypeNotFound);

        var fieldErrors = StepDataValidator.ValidateRequiredFields(snapshot.SnapshotJson, procedure.StepData);
        if (fieldErrors.Count > 0)
            return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(
                ProcedureError.RequiredFieldsMissing(fieldErrors));

        var actorDefs = await procedureTypeRepository.GetActorDefinitionsAsync(
            procedure.ProcedureTypeId, command.TenantId, ct);
        var existingActors = await procedureRepository.GetActorsAsync(
            procedure.Id, command.TenantId, actorDefinitionId: null, ct);

        foreach (var def in actorDefs.Where(d => d.IsRequired && d.Role != "representante_legal"))
        {
            if (!existingActors.Any(a => a.ActorDefinitionId == def.Id))
            {
                return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(
                    ProcedureError.RequiredActorsMissing);
            }
        }

        var procedureType = await procedureTypeRepository.FindByIdWithDetailsAsync(
            procedure.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<SubmitProcedureResponseDto, ProcedureError>.Failure(ProcedureError.ProcedureTypeNotFound);

        var now = clock.UtcNow;
        var submitSnapshot = SnapshotHelper.BuildSnapshot(procedureType, now);
        await procedureTypeRepository.AddSnapshotAsync(submitSnapshot, ct);

        procedure.ProcedureTypeSnapshotId = submitSnapshot.Id;
        procedure.Status = "submitted";
        procedure.SubmittedAt = now;
        procedure.UpdatedAt = now;
        procedure.UpdatedBy = command.UserId;

        await procedureRepository.UpdateAsync(procedure, ct);

        await eventPublisher.PublishProcedureSubmittedAsync(
            new ProcedureSubmitted(procedure.Id, command.TenantId, submitSnapshot.Id, now), ct);

        return Result<SubmitProcedureResponseDto, ProcedureError>.Success(
            new SubmitProcedureResponseDto(procedure.Id, procedure.Status, procedure.SubmittedAt));
    }
}
