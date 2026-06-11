namespace Flit.Modules.Documents.Domain.Errors;

public sealed record DocumentError(string Code, string Message)
{
    public static readonly DocumentError DocumentTypeNotFound =
        new("DOCUMENT_TYPE_NOT_FOUND", "Tipo de documento no encontrado.");

    public static readonly DocumentError ProcedureTypeNotFound =
        new("PROCEDURE_TYPE_NOT_FOUND", "Tipo de trámite no encontrado.");

    public static readonly DocumentError DocumentAlreadyAssociated =
        new("DOCUMENT_ALREADY_ASSOCIATED", "El documento ya está asociado a este tipo de trámite.");

    public static readonly DocumentError InvalidLoadType =
        new("DOCUMENT_INVALID_LOAD_TYPE", "load_type inválido. Valores: carga, generacion.");

    public static readonly DocumentError ActorDefinitionNotFound =
        new("ACTOR_DEFINITION_NOT_FOUND", "Actor no encontrado en el tipo de trámite.");

    public static readonly DocumentError TemplateNotFound =
        new("DOCUMENT_TEMPLATE_NOT_FOUND", "Plantilla no encontrada.");

    public static readonly DocumentError TemplateRequiresGeneracionLoadType =
        new("DOCUMENT_TEMPLATE_REQUIRES_GENERACION", "Solo tipos de documento con load_type=generacion admiten plantillas.");

    public static readonly DocumentError TemplateHtmlRequired =
        new("DOCUMENT_TEMPLATE_HTML_REQUIRED", "El archivo HTML de la plantilla es requerido.");

    public static readonly DocumentError TemplateHtmlEmpty =
        new("DOCUMENT_TEMPLATE_HTML_EMPTY", "El contenido HTML de la plantilla está vacío.");

    public static readonly DocumentError ProcedureNotFound =
        new("PROCEDURE_NOT_FOUND", "Trámite no encontrado.");

    public static readonly DocumentError ConsolidationNotReady =
        new("CONSOLIDATION_NOT_READY", "Los documentos obligatorios no están completos para consolidar.");

    public static readonly DocumentError ConsolidatedPackageNotFound =
        new("CONSOLIDATED_PACKAGE_NOT_FOUND", "No existe paquete consolidado para este trámite.");
}
