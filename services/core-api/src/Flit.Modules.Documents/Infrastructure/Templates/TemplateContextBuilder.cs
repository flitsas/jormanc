using System.Collections.Frozen;
using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Documents.Domain.Models;

namespace Flit.Modules.Documents.Infrastructure.Templates;

public static class TemplateContextBuilder
{
    public static TemplateContext Build(
        Procedure procedure,
        IReadOnlyList<ProcedureActor> actors,
        IReadOnlyList<ActorDefinition> actorDefinitions,
        VehicleQuery? vehicleQuery,
        OtOrganism? ot)
    {
        var roleByDefinitionId = actorDefinitions.ToDictionary(a => a.Id, a => a.Role);

        var actorContext = new Dictionary<string, ActorContextData>(StringComparer.Ordinal);
        foreach (var actor in actors)
        {
            if (!roleByDefinitionId.TryGetValue(actor.ActorDefinitionId, out var role))
                continue;

            actorContext[role] = new ActorContextData
            {
                FullName = actor.FullName,
                DocumentNumber = actor.DocumentNumber,
                Nit = actor.Nit,
                CuotaPct = actor.CuotaPct
            };
        }

        VehicleContextData? vehicle = null;
        if (vehicleQuery is not null)
        {
            vehicle = new VehicleContextData
            {
                Plate = vehicleQuery.QueryKey == "placa" ? vehicleQuery.QueryValue : null,
                Runt = ParseFlatPayload(vehicleQuery.RuntPayload),
                Simit = ParseFlatPayload(vehicleQuery.SimitPayload)
            };
        }

        return new TemplateContext
        {
            Procedure = new ProcedureContextData
            {
                CompositeId = procedure.CompositeId,
                SubmittedAt = procedure.SubmittedAt
            },
            Actors = actorContext.Count == 0
                ? FrozenDictionary<string, ActorContextData>.Empty
                : actorContext.ToFrozenDictionary(StringComparer.Ordinal),
            Vehicle = vehicle,
            Ot = ot is null ? null : new OtContextData
            {
                Name = ot.Name,
                Code = ot.Slug
            }
        };
    }

    private static FrozenDictionary<string, string?> ParseFlatPayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return FrozenDictionary<string, string?>.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return FrozenDictionary<string, string?>.Empty;

            var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.ToString();

            return dict.ToFrozenDictionary(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return FrozenDictionary<string, string?>.Empty;
        }
    }
}
