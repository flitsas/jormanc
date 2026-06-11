using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;

namespace Flit.Modules.OT.Application;

public static class OtDocumentMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyList<Guid> DeserializeOrderedIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
    }

    public static string SerializeOrderedIds(IReadOnlyList<Guid> ids) =>
        JsonSerializer.Serialize(ids, JsonOptions);

    public static IReadOnlyList<OrderedDocumentDto> MapOrderedDocuments(
        IReadOnlyList<Guid> orderedIds,
        IReadOnlyDictionary<Guid, DocumentType> documentTypesById)
    {
        var result = new List<OrderedDocumentDto>(orderedIds.Count);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            if (!documentTypesById.TryGetValue(id, out var docType))
                continue;

            result.Add(new OrderedDocumentDto(
                i + 1,
                new DocumentTypeSummaryDto(docType.Id, docType.Name)));
        }

        return result;
    }

    public static OtDocumentLabelDto ToLabelDto(OtDocumentLabel label) =>
        new(label.Id, label.OtId, label.Slug, label.DisplayName, label.IsActive, label.CreatedAt);
}
