using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de UpdateSignatureMatrixCommand.
/// Valida actor_role y signature_type, luego reemplaza todas las entradas de la matriz
/// (upsert por companyId — operación replace-all atómica).
/// AC1 HU-9776.
/// </summary>
public sealed class UpdateSignatureMatrixCommandHandler(ICompanyRepository companyRepository)
{
    private static readonly HashSet<string> ValidActorRoles =
        ["vendedor", "comprador", "representante_legal"];

    private static readonly HashSet<string> ValidSignatureTypes =
        ["identidad_digital", "firma_pantalla", "preasignada"];

    public async Task<Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>> HandleAsync(
        UpdateSignatureMatrixCommand command, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(command.CompanyId, ct);
        if (company is null)
            return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Failure(CompanyError.NotFound);

        foreach (var entry in command.Entries)
        {
            if (!ValidActorRoles.Contains(entry.ActorRole))
                return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Failure(
                    CompanyError.InvalidActorRole);

            if (!ValidSignatureTypes.Contains(entry.SignatureType))
                return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Failure(
                    CompanyError.InvalidSignatureType);
        }

        var entities = command.Entries
            .Select(e => new CompanySignatureMatrix
            {
                Id = Guid.NewGuid(),
                CompanyId = command.CompanyId,
                ActorRole = e.ActorRole,
                SignatureType = e.SignatureType,
                IsActive = e.IsActive
            })
            .ToList();

        await companyRepository.ReplaceSignatureMatrixAsync(command.CompanyId, entities, ct);

        var dtos = entities.Select(e => new SignatureMatrixEntryDto(
            Id: e.Id,
            CompanyId: e.CompanyId,
            ActorRole: e.ActorRole,
            SignatureType: e.SignatureType,
            IsActive: e.IsActive))
            .ToList();

        return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Success(dtos);
    }
}
