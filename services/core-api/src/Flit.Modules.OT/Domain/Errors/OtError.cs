namespace Flit.Modules.OT.Domain.Errors;

/// <summary>
/// Errores de dominio del módulo OT (Organismos de Tránsito).
/// </summary>
public sealed record OtError(string Code, string Message, int? ImpactCount = null)
{
    public static readonly OtError SlugAlreadyExists =
        new("OT_SLUG_ALREADY_EXISTS", "Ya existe un organismo de tránsito con ese slug en el tenant.");

    public static readonly OtError NotFound =
        new("OT_NOT_FOUND", "Organismo de tránsito no encontrado.");

    public static readonly OtError InvalidMode =
        new("OT_INVALID_MODE", "Modo inválido. Valores permitidos: dashboard, qx.");

    public static readonly OtError ProcedureTypeNotFound =
        new("OT_PROCEDURE_TYPE_NOT_FOUND", "Tipo de trámite no encontrado.");

    public static readonly OtError LabelNotFound =
        new("OT_LABEL_NOT_FOUND", "Etiqueta de documento no encontrada.");

    public static readonly OtError LabelSlugAlreadyExists =
        new("OT_LABEL_SLUG_ALREADY_EXISTS", "Ya existe una etiqueta con ese slug en el organismo de tránsito.");

    public static OtError LabelInUse(int impactCount) =>
        new("LABEL_IN_USE",
            "La etiqueta tiene adjuntos asociados. Envíe confirm=true para eliminar.",
            impactCount);

    public static readonly OtError SlugNotFound =
        new("OT_SLUG_NOT_FOUND", "Organismo de tránsito no encontrado.");

    public static readonly OtError InvalidWebhookSignature =
        new("OT_INVALID_WEBHOOK_SIGNATURE", "Firma de webhook inválida.");

    public static readonly OtError QuipuxNotEnabled =
        new("OT_QUIPUX_NOT_ENABLED", "Quipux no está habilitado para este organismo.");

    public static readonly OtError ProcedureNotFound =
        new("OT_PROCEDURE_NOT_FOUND", "Trámite no encontrado para el procedure_ref indicado.");
}
