namespace Flit.Infrastructure.Persistence.Entities.Companies;

/// <summary>
/// Tipo de firma configurado por rol de actor en una compañía.
/// schema: companies / tabla: company_signature_matrix
/// </summary>
public sealed class CompanySignatureMatrix
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>vendedor | comprador | representante_legal</summary>
    public string ActorRole { get; set; } = string.Empty;
    /// <summary>identidad_digital | firma_pantalla | preasignada</summary>
    public string SignatureType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
}
