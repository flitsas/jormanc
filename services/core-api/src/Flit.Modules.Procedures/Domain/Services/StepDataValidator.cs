using System.Text.Json;

namespace Flit.Modules.Procedures.Domain.Services;

public sealed record FieldValidationError(string FieldSlug, string Step, string Error);

public static class StepDataValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyList<FieldValidationError> ValidateRequiredFields(
        string snapshotJson, string stepDataJson)
    {
        var errors = new List<FieldValidationError>();
        var stepData = ParseStepData(stepDataJson);

        try
        {
            using var doc = JsonDocument.Parse(snapshotJson);
            if (!doc.RootElement.TryGetProperty("steps", out var steps) ||
                steps.ValueKind != JsonValueKind.Array)
                return errors;

            foreach (var step in steps.EnumerateArray())
            {
                var stepName = step.TryGetProperty("name", out var nameEl)
                    ? nameEl.GetString() ?? string.Empty
                    : string.Empty;

                if (!step.TryGetProperty("sections", out var sections) ||
                    sections.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var section in sections.EnumerateArray())
                {
                    if (!section.TryGetProperty("fields", out var fields) ||
                        fields.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var field in fields.EnumerateArray())
                    {
                        if (!field.TryGetProperty("isRequired", out var requiredEl) ||
                            !requiredEl.GetBoolean())
                            continue;

                        if (!field.TryGetProperty("slug", out var slugEl))
                            continue;

                        var slug = slugEl.GetString();
                        if (string.IsNullOrWhiteSpace(slug))
                            continue;

                        if (!HasValue(stepData, slug))
                        {
                            errors.Add(new FieldValidationError(
                                slug, stepName, "REQUIRED_FIELD_MISSING"));
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // snapshot inválido — sin campos requeridos detectables
        }

        return errors;
    }

    private static Dictionary<string, JsonElement> ParseStepData(string stepDataJson)
    {
        if (string.IsNullOrWhiteSpace(stepDataJson) || stepDataJson == "{}")
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stepDataJson, JsonOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool HasValue(IReadOnlyDictionary<string, JsonElement> data, string slug)
    {
        if (!data.TryGetValue(slug, out var element))
            return false;

        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
            _ => true
        };
    }
}
