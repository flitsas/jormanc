namespace Flit.Infrastructure.Persistence.Entities.Companies;

/// <summary>
/// Organismos de Tránsito habilitados por compañía y familia de trámite.
/// schema: companies / tabla: company_ot_enabled
/// </summary>
public sealed class CompanyOtEnabled
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>Slug del Organismo de Tránsito.</summary>
    public string OtSlug { get; set; } = string.Empty;
    /// <summary>matricula_inicial | traspasos | otros</summary>
    public string ProcedureFamily { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public Company Company { get; set; } = null!;
}
