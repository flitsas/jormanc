using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC4 HU-9791 — POST /procedures/{id}/documents/consolidate (re-consolidación forzada).</summary>
public sealed class ForceReconsolidationCommandHandler(
    GenerateProcedureDocumentsCommandHandler generateHandler,
    ConsolidateDocumentsCommandHandler consolidateHandler)
{
    public async Task<Result<ConsolidatedPackageDto, DocumentError>> HandleAsync(
        ForceReconsolidationCommand command, CancellationToken ct = default)
    {
        await generateHandler.HandleAsync(
            new GenerateProcedureDocumentsCommand(command.ProcedureId, command.TenantId, command.UserId),
            ct);

        return await consolidateHandler.HandleAsync(
            new ConsolidateDocumentsCommand(command.ProcedureId, command.TenantId, command.UserId, Force: true),
            ct);
    }
}
