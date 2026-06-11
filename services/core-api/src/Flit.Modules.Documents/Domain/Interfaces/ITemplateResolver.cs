using Flit.Modules.Documents.Domain.Models;

namespace Flit.Modules.Documents.Domain.Interfaces;

public interface ITemplateResolver
{
    string Resolve(string html, TemplateContext context);

    IReadOnlyList<string> DetectMarkers(string html);
}
