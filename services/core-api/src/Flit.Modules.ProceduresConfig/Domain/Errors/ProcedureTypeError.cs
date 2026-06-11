namespace Flit.Modules.ProceduresConfig.Domain.Errors;

public sealed record RuleConflictDetail(string Rule1, string Rule2, string Description);

public sealed record ProcedureTypeError(
    string Code,
    string Message,
    IReadOnlyList<RuleConflictDetail>? Conflicts = null)
{
    public static readonly ProcedureTypeError NotFound =
        new("PROCEDURE_TYPE_NOT_FOUND", "Tipo de trámite no encontrado.");

    public static readonly ProcedureTypeError SlugAlreadyExists =
        new("PROCEDURE_TYPE_SLUG_ALREADY_EXISTS", "Ya existe un tipo de trámite con ese slug en el tenant.");

    public static readonly ProcedureTypeError StepNotFound =
        new("PROCEDURE_STEP_NOT_FOUND", "Paso no encontrado en el tipo de trámite.");

    public static readonly ProcedureTypeError SectionNotFound =
        new("FORM_SECTION_NOT_FOUND", "Sección no encontrada en el paso.");

    public static readonly ProcedureTypeError HasActiveProcedures =
        new("PROCEDURE_TYPE_HAS_ACTIVE_PROCEDURES",
            "No se puede eliminar: existen trámites activos en estado draft o submitted.");

    public static readonly ProcedureTypeError InvalidFamily =
        new("PROCEDURE_TYPE_INVALID_FAMILY",
            "family inválida. Valores: matricula_inicial, traspasos, otros.");

    public static readonly ProcedureTypeError InvalidScope =
        new("PROCEDURE_TYPE_INVALID_SCOPE",
            "scope inválido. Valores: global, company, ot, company_ot.");

    public static readonly ProcedureTypeError InvalidVehicleQueryKey =
        new("PROCEDURE_TYPE_INVALID_VEHICLE_QUERY_KEY",
            "vehicle_query_key inválida. Valores: placa, vin, placa_vin.");

    public static readonly ProcedureTypeError InvalidFieldType =
        new("FORM_FIELD_INVALID_TYPE",
            "field_type inválido. Valores: text, dropdown, checkbox, numeric, attachment, list.");

    public static readonly ProcedureTypeError InvalidConfigJson =
        new("FORM_FIELD_INVALID_CONFIG", "config debe ser JSON válido.");

    public static readonly ProcedureTypeError InvalidRuleJson =
        new("RULE_SET_INVALID_JSON", "conditions o actions deben ser JSON válido.");

    public static readonly ProcedureTypeError ActorNotFound =
        new("ACTOR_NOT_FOUND", "Actor no encontrado en el tipo de trámite.");

    public static readonly ProcedureTypeError LegalRepActorNotFound =
        new("LEGAL_REP_ACTOR_NOT_FOUND", "El actor representante legal referenciado no existe.");

    public static readonly ProcedureTypeError InvalidVerificationsJson =
        new("QUERY_RULE_INVALID_VERIFICATIONS", "verifications debe ser un array JSON válido.");

    public static readonly ProcedureTypeError FieldNotFound =
        new("FORM_FIELD_NOT_FOUND", "Campo no encontrado en la sección.");

    public static readonly ProcedureTypeError ApiConnectorNotFound =
        new("API_CONNECTOR_NOT_FOUND", "Conector API no encontrado.");

    public static readonly ProcedureTypeError InvalidParamBindingsJson =
        new("API_CONNECTOR_INVALID_PARAM_BINDINGS", "param_bindings debe ser un objeto JSON válido.");

    public static ProcedureTypeError RuleConflict(IReadOnlyList<RuleConflictDetail> conflicts) =>
        new("RULE_SET_CONFLICT", "Conflicto de coherencia entre reglas.", conflicts);
}
