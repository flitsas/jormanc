using Flit.Infrastructure.Persistence.Entities.Documents;

namespace Flit.Modules.Documents.Domain.Services;

public static class DocumentCompletionChecker
{
    public static bool CanConsolidate(
        IReadOnlyList<ProcedureTypeDocument> configs,
        IReadOnlyList<ProcedureDocument> documents)
    {
        if (configs.Count == 0)
            return false;

        var allowPartial = configs.Any(c => c.AllowPartialConsolidation);
        var readyCount = 0;

        foreach (var config in configs)
        {
            var doc = documents.FirstOrDefault(d => d.DocumentTypeId == config.DocumentTypeId);
            var isReady = doc is not null &&
                          doc.Status == "ready" &&
                          !string.IsNullOrWhiteSpace(doc.FileRef);

            if (isReady)
            {
                readyCount++;
                continue;
            }

            if (config.IsRequired && !allowPartial)
                return false;
        }

        return readyCount > 0;
    }

    public static IReadOnlyList<string> GetMissingRequired(
        IReadOnlyList<ProcedureTypeDocument> configs,
        IReadOnlyList<ProcedureDocument> documents)
    {
        var missing = new List<string>();

        foreach (var config in configs.Where(c => c.IsRequired))
        {
            var doc = documents.FirstOrDefault(d => d.DocumentTypeId == config.DocumentTypeId);
            if (doc is null || doc.Status != "ready" || string.IsNullOrWhiteSpace(doc.FileRef))
                missing.Add(config.DocumentType.Name);
        }

        return missing;
    }
}
