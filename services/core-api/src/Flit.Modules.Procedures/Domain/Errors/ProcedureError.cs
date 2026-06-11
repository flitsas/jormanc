using Flit.Modules.Procedures.Domain.Services;

namespace Flit.Modules.Procedures.Domain.Errors;

public sealed record ProcedureError(
    string Code,
    string Message,
    decimal? CuotaCurrentSum = null,
    decimal? CuotaProposed = null,
    IReadOnlyList<FieldValidationError>? ValidationErrors = null)
{
    public static readonly ProcedureError NotFound =
        new("PROCEDURE_NOT_FOUND", "Trámite no encontrado.");

    public static readonly ProcedureError ProcedureTypeNotFound =
        new("PROCEDURE_TYPE_NOT_FOUND", "Tipo de trámite no encontrado.");

    public static readonly ProcedureError CompanyNotFound =
        new("COMPANY_NOT_FOUND", "Compañía no encontrada.");

    public static readonly ProcedureError InvalidStatus =
        new("PROCEDURE_INVALID_STATUS", "El trámite no está en estado draft.");

    public static readonly ProcedureError VehicleQueryFailed =
        new("VEHICLE_QUERY_FAILED", "No fue posible consultar el vehículo en RUNT.");

    public static readonly ProcedureError Forbidden =
        new("FORBIDDEN", "Permiso insuficiente para esta operación.");

    public static readonly ProcedureError ActorDefinitionNotFound =
        new("ACTOR_DEFINITION_NOT_FOUND", "Definición de actor no encontrada.");

    public static readonly ProcedureError InvalidNature =
        new("ACTOR_INVALID_NATURE", "Naturaleza de actor no permitida para esta definición.");

    public static readonly ProcedureError PersonQueryFailed =
        new("PERSON_QUERY_FAILED", "No fue posible consultar la persona en RUNT.");

    public static readonly ProcedureError LegalEntityQueryFailed =
        new("LEGAL_ENTITY_QUERY_FAILED", "No fue posible consultar la entidad en RUES.");

    public static ProcedureError CuotaSumExceeds100(decimal currentSum, decimal proposed) =>
        new(
            "CUOTA_SUM_EXCEEDS_100",
            $"La suma de cuotas sería {currentSum + proposed}% (máximo 100%). Actual: {currentSum}%, propuesta: {proposed}%.",
            currentSum,
            proposed);

    public static readonly ProcedureError RequiredActorsMissing =
        new("REQUIRED_ACTORS_MISSING", "Faltan actores obligatorios para el tipo de trámite.");

    public static readonly ProcedureError InvalidAttachmentStatus =
        new("INVALID_ATTACHMENT_STATUS", "Solo se permiten adjuntos en trámites draft o submitted.");

    public static readonly ProcedureError AttachmentUploadFailed =
        new("ATTACHMENT_UPLOAD_FAILED", "No fue posible subir el adjunto.");

    public static ProcedureError RequiredFieldsMissing(IReadOnlyList<FieldValidationError> errors) =>
        new("REQUIRED_FIELDS_MISSING", "Faltan campos obligatorios en step_data.", ValidationErrors: errors);
}
