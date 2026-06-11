namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Obtiene todas las entradas de la matriz de firmas de una compañía.
/// AC1 HU-9776 — GET /api/v1/admin/companies/{id}/config/signature-matrix
/// </summary>
public sealed record GetSignatureMatrixQuery(Guid CompanyId);
