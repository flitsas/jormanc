using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Helpers;
using Flit.Modules.Procedures.Application.Mapping;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.Procedures.Domain.Services;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Commands;

public sealed class CreateProcedureCommandHandler(
    IProcedureRepository procedureRepository,
    IProcedureTypeRepository procedureTypeRepository,
    ICompanyRepository companyRepository,
    IClock clock)
{
    public async Task<Result<ProcedureDto, ProcedureError>> HandleAsync(
        CreateProcedureCommand command, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(command.CompanyId, ct);
        if (company is null || company.TenantId != command.TenantId)
            return Result<ProcedureDto, ProcedureError>.Failure(ProcedureError.CompanyNotFound);

        var procedureType = await procedureTypeRepository.FindByIdWithDetailsAsync(
            command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null || !procedureType.IsActive)
            return Result<ProcedureDto, ProcedureError>.Failure(ProcedureError.ProcedureTypeNotFound);

        var now = clock.UtcNow;

        var snapshot = await procedureTypeRepository.FindSnapshotAsync(
            procedureType.Id, procedureType.Version, ct);
        if (snapshot is null)
        {
            snapshot = SnapshotHelper.BuildSnapshot(procedureType, now);
            await procedureTypeRepository.AddSnapshotAsync(snapshot, ct);
        }

        var steps = SnapshotHelper.ExtractSteps(snapshot.SnapshotJson);
        var sequence = await procedureRepository.CountByTenantAsync(command.TenantId, ct) + 1;
        var companySlug = DeriveCompanySlug(company.Name);
        var compositeId = CompositeIdGenerator.Generate(procedureType.Family, companySlug, sequence);

        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            OtId = command.OtId,
            ProcedureTypeId = procedureType.Id,
            ProcedureTypeSnapshotId = snapshot.Id,
            CompositeId = compositeId,
            Status = "draft",
            CurrentStepOrder = 1,
            StepData = "{}",
            CreatedAt = now,
            CreatedBy = command.UserId,
            UpdatedAt = now,
            UpdatedBy = command.UserId
        };

        await procedureRepository.CreateAsync(procedure, ct);

        return Result<ProcedureDto, ProcedureError>.Success(
            ProcedureMapper.ToDto(procedure, steps));
    }

    private static string DeriveCompanySlug(string companyName)
    {
        var trimmed = companyName.Trim();
        if (trimmed.Length == 0)
            return "GEN";

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][..1]}{parts[1][..1]}".ToUpperInvariant();

        return trimmed.Length >= 3
            ? trimmed[..3].ToUpperInvariant()
            : trimmed.ToUpperInvariant();
    }
}
