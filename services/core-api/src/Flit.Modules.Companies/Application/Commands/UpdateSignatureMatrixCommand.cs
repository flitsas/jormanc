namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Reemplaza la matriz de firmas de una compañía (actor × tipo de firma).
/// AC1 HU-9776 — PUT /api/v1/admin/companies/{id}/config/signature-matrix
/// SuperAdmin only.
/// </summary>
public sealed record UpdateSignatureMatrixCommand(
    Guid CompanyId,
    Guid RequestedByUserId,
    IReadOnlyList<SignatureMatrixEntry> Entries);

/// <summary>
/// Una entrada de la matriz: rol del actor y tipo de firma asociado.
/// </summary>
public sealed record SignatureMatrixEntry(
    /// <summary>vendedor | comprador | representante_legal</summary>
    string ActorRole,
    /// <summary>identidad_digital | firma_pantalla | preasignada</summary>
    string SignatureType,
    bool IsActive = true);
