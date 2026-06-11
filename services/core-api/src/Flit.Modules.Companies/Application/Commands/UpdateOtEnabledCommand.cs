namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Reemplaza la lista de OTs habilitadas por tenant para una compañía.
/// AC3 HU-9776 — PUT /api/v1/admin/companies/{id}/config/ot-enabled
/// SuperAdmin only.
/// </summary>
public sealed record UpdateOtEnabledCommand(
    Guid CompanyId,
    Guid RequestedByUserId,
    IReadOnlyList<OtEnabledEntry> Entries);

/// <summary>
/// Una entrada OT habilitada: slug del OT, familia de trámite y si está activa.
/// </summary>
public sealed record OtEnabledEntry(
    /// <summary>Slug del Organismo de Tránsito.</summary>
    string OtSlug,
    /// <summary>matricula_inicial | traspasos | otros</summary>
    string ProcedureFamily,
    bool IsEnabled = true);
