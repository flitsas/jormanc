using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de UpdateOtEnabledCommand.
/// Valida procedure_family y ot_slug (no vacío), luego reemplaza todas las entradas
/// de OTs habilitadas para la compañía.
/// AC3 HU-9776.
/// </summary>
public sealed class UpdateOtEnabledCommandHandler(ICompanyRepository companyRepository)
{
    private static readonly HashSet<string> ValidProcedureFamilies =
        ["matricula_inicial", "traspasos", "otros"];

    public async Task<Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>> HandleAsync(
        UpdateOtEnabledCommand command, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(command.CompanyId, ct);
        if (company is null)
            return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Failure(CompanyError.NotFound);

        foreach (var entry in command.Entries)
        {
            if (!ValidProcedureFamilies.Contains(entry.ProcedureFamily))
                return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Failure(
                    CompanyError.InvalidProcedureFamily);

            if (string.IsNullOrWhiteSpace(entry.OtSlug))
                return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Failure(
                    CompanyError.InvalidProcedureFamily);
        }

        var entities = command.Entries
            .Select(e => new CompanyOtEnabled
            {
                Id = Guid.NewGuid(),
                CompanyId = command.CompanyId,
                OtSlug = e.OtSlug.Trim().ToLowerInvariant(),
                ProcedureFamily = e.ProcedureFamily,
                IsEnabled = e.IsEnabled
            })
            .ToList();

        await companyRepository.ReplaceOtEnabledAsync(command.CompanyId, entities, ct);

        var dtos = entities.Select(e => new OtEnabledEntryDto(
            Id: e.Id,
            CompanyId: e.CompanyId,
            OtSlug: e.OtSlug,
            ProcedureFamily: e.ProcedureFamily,
            IsEnabled: e.IsEnabled))
            .ToList();

        return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Success(dtos);
    }
}
